// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AWS.Deploy.CLI.Commands.TypeHints;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Extensions;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Recipes.Validation;
using AWS.Deploy.DockerEngine;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.CDK;
using AWS.Deploy.Recipes;
using AWS.Deploy.Orchestration.Data;
using AWS.Deploy.Orchestration.Utilities;
using AWS.Deploy.Orchestration.DisplayedResources;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Orchestration.LocalUserSettings;
using Newtonsoft.Json;
using AWS.Deploy.Orchestration.ServiceHandlers;
using Microsoft.Extensions.DependencyInjection;
using AWS.Deploy.Common.Data;

namespace AWS.Deploy.CLI.Commands
{
    public class DeployCommand
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IToolInteractiveService _toolInteractiveService;
        private readonly IOrchestratorInteractiveService _orchestratorInteractiveService;
        private readonly ICdkProjectHandler _cdkProjectHandler;
        private readonly ICDKManager _cdkManager;
        private readonly IDeploymentBundleHandler _deploymentBundleHandler;
        private readonly IDockerEngine _dockerEngine;
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly ICloudFormationTemplateReader _cloudFormationTemplateReader;
        private readonly IDeployedApplicationQueryer _deployedApplicationQueryer;
        private readonly ITypeHintCommandFactory _typeHintCommandFactory;
        private readonly IDisplayedResourcesHandler _displayedResourcesHandler;
        private readonly ICloudApplicationNameGenerator _cloudApplicationNameGenerator;
        private readonly ILocalUserSettingsEngine _localUserSettingsEngine;
        private readonly IConsoleUtilities _consoleUtilities;
        private readonly ISystemCapabilityEvaluator _systemCapabilityEvaluator;
        private readonly OrchestratorSession _session;
        private readonly IDirectoryManager _directoryManager;
        private readonly IFileManager _fileManager;
        private readonly ICDKVersionDetector _cdkVersionDetector;
        private readonly IAWSServiceHandler _awsServiceHandler;
        private readonly IOptionSettingHandler _optionSettingHandler;
        private readonly IValidatorFactory _validatorFactory;
        private readonly IRecipeHandler _recipeHandler;
        private readonly IDeployToolWorkspaceMetadata _deployToolWorkspaceMetadata;
        private readonly IDeploymentSettingsHandler _deploymentSettingsHandler;

        public DeployCommand(
            IServiceProvider serviceProvider,
            IToolInteractiveService toolInteractiveService,
            IOrchestratorInteractiveService orchestratorInteractiveService,
            ICdkProjectHandler cdkProjectHandler,
            ICDKManager cdkManager,
            ICDKVersionDetector cdkVersionDetector,
            IDeploymentBundleHandler deploymentBundleHandler,
            IDockerEngine dockerEngine,
            IAWSResourceQueryer awsResourceQueryer,
            ICloudFormationTemplateReader cloudFormationTemplateReader,
            IDeployedApplicationQueryer deployedApplicationQueryer,
            ITypeHintCommandFactory typeHintCommandFactory,
            IDisplayedResourcesHandler displayedResourcesHandler,
            ICloudApplicationNameGenerator cloudApplicationNameGenerator,
            ILocalUserSettingsEngine localUserSettingsEngine,
            IConsoleUtilities consoleUtilities,
            ISystemCapabilityEvaluator systemCapabilityEvaluator,
            OrchestratorSession session,
            IDirectoryManager directoryManager,
            IFileManager fileManager,
            IAWSServiceHandler awsServiceHandler,
            IOptionSettingHandler optionSettingHandler,
            IValidatorFactory validatorFactory,
            IRecipeHandler recipeHandler,
            IDeployToolWorkspaceMetadata deployToolWorkspaceMetadata,
            IDeploymentSettingsHandler deploymentSettingsHandler)
        {
            _serviceProvider = serviceProvider;
            _toolInteractiveService = toolInteractiveService;
            _orchestratorInteractiveService = orchestratorInteractiveService;
            _cdkProjectHandler = cdkProjectHandler;
            _deploymentBundleHandler = deploymentBundleHandler;
            _dockerEngine = dockerEngine;
            _awsResourceQueryer = awsResourceQueryer;
            _cloudFormationTemplateReader = cloudFormationTemplateReader;
            _deployedApplicationQueryer = deployedApplicationQueryer;
            _typeHintCommandFactory = typeHintCommandFactory;
            _displayedResourcesHandler = displayedResourcesHandler;
            _cloudApplicationNameGenerator = cloudApplicationNameGenerator;
            _localUserSettingsEngine = localUserSettingsEngine;
            _consoleUtilities = consoleUtilities;
            _session = session;
            _directoryManager = directoryManager;
            _fileManager = fileManager;
            _cdkVersionDetector = cdkVersionDetector;
            _cdkManager = cdkManager;
            _systemCapabilityEvaluator = systemCapabilityEvaluator;
            _awsServiceHandler = awsServiceHandler;
            _optionSettingHandler = optionSettingHandler;
            _validatorFactory = validatorFactory;
            _recipeHandler = recipeHandler;
            _deployToolWorkspaceMetadata = deployToolWorkspaceMetadata;
            _deploymentSettingsHandler = deploymentSettingsHandler;
        }

        public async Task ExecuteAsync(string applicationName, string deploymentProjectPath, SaveSettingsConfiguration saveSettingsConfig, DeploymentSettings? deploymentSettings = null)
        {
            var (orchestrator, selectedRecommendation, cloudApplication) = await InitializeDeployment(applicationName, deploymentSettings, deploymentProjectPath);

            // Verify Docker installation and minimum NodeJS version.
            await EvaluateSystemCapabilities(selectedRecommendation);

            // Configure option settings.
            await ConfigureDeployment(cloudApplication, orchestrator, selectedRecommendation, deploymentSettings);

            if (!ConfirmDeployment(selectedRecommendation))
            {
                return;
            }

            // Because we're starting a deployment, clear the cached system capabilities checks
            // in case the deployment fails and the user reruns it after modifying Docker or Node 
            _systemCapabilityEvaluator.ClearCachedCapabilityChecks();

            await CreateDeploymentBundle(orchestrator, selectedRecommendation, cloudApplication);

            if (saveSettingsConfig.SettingsType != SaveSettingsType.None)
            {
                await _deploymentSettingsHandler.SaveSettings(saveSettingsConfig, selectedRecommendation, cloudApplication, _session);
                _toolInteractiveService.WriteLine($"{Environment.NewLine}Successfully saved the deployment settings at {saveSettingsConfig.FilePath}");
            }

            await orchestrator.DeployRecommendation(cloudApplication, selectedRecommendation);

            var displayedResources = await _displayedResourcesHandler.GetDeploymentOutputs(cloudApplication, selectedRecommendation);
            DisplayOutputResources(displayedResources);
        }

        private void DisplayOutputResources(List<DisplayedResourceItem> displayedResourceItems)
        {
            _orchestratorInteractiveService.LogSectionStart("AWS Resource Details", null);
            foreach (var resource in displayedResourceItems)
            {
                _toolInteractiveService.WriteLine($"{resource.Description}:");
                _toolInteractiveService.WriteLine($"\t{nameof(resource.Id)}: {resource.Id}");
                _toolInteractiveService.WriteLine($"\t{nameof(resource.Type)}: {resource.Type}");
                foreach (var resourceKey in resource.Data.Keys)
                {
                    _toolInteractiveService.WriteLine($"\t{resourceKey}: {resource.Data[resourceKey]}");
                }
            }
        }

        /// <summary>
        /// Initiates a deployment or a re-deployment.
        /// If a new Cloudformation stack name is selected, then a fresh deployment is initiated with the user-selected deployment recipe.
        /// If an existing deployment target is selected, then a re-deployment is initiated with the same deployment recipe.
        /// </summary>
        /// <param name="cloudApplicationName">The cloud application name provided via the --application-name CLI argument</param>
        /// <param name="deploymentSettings">The deserialized object from the user provided config file.<see cref="DeploymentSettings"/></param>
        /// <param name="deploymentProjectPath">The absolute or relative path of the CDK project that will be used for deployment</param>
        /// <returns>A tuple consisting of the Orchestrator object, Selected Recommendation, Cloud Application metadata.</returns>
        public async Task<(Orchestrator, Recommendation, CloudApplication)> InitializeDeployment(string cloudApplicationName, DeploymentSettings? deploymentSettings, string deploymentProjectPath)
        {
            var orchestrator = new Orchestrator(
                    _session,
                    _orchestratorInteractiveService,
                    _cdkProjectHandler,
                    _cdkManager,
                    _cdkVersionDetector,
                    _awsResourceQueryer,
                    _deploymentBundleHandler,
                    _localUserSettingsEngine,
                    _dockerEngine,
                    _recipeHandler,
                    _fileManager,
                    _directoryManager,
                    _awsServiceHandler,
                    _optionSettingHandler,
                    _deployToolWorkspaceMetadata);

            // Determine what recommendations are possible for the project.
            var recommendations = await GenerateDeploymentRecommendations(orchestrator, deploymentProjectPath);

            // Get all existing applications that were previously deployed using our deploy tool.
            var allDeployedApplications = await _deployedApplicationQueryer.GetExistingDeployedApplications(recommendations.Select(x => x.Recipe.DeploymentType).ToList());

            // Filter compatible applications that can be re-deployed  using the current set of recommendations.
            var compatibleApplications = await _deployedApplicationQueryer.GetCompatibleApplications(recommendations, allDeployedApplications, _session);

            if (string.IsNullOrEmpty(cloudApplicationName))
                // Try finding the CloudApplication name via the user provided config settings.
                cloudApplicationName = deploymentSettings?.ApplicationName ?? string.Empty;

            // Prompt the user with a choice to re-deploy to existing targets or deploy to a new cloud application.
            // This prompt is NOT needed if the user is just pushing the docker image to ECR.
            if (string.IsNullOrEmpty(cloudApplicationName) && !string.Equals(deploymentSettings?.RecipeId, Constants.RecipeIdentifier.PUSH_TO_ECR_RECIPE_ID))
                cloudApplicationName = AskForCloudApplicationNameFromDeployedApplications(compatibleApplications);

            // Find existing application with the same CloudApplication name.
            CloudApplication? deployedApplication = null;
            if (!string.IsNullOrEmpty(deploymentSettings?.RecipeId))
            {
                // if the recommendation is specified via a config file then find the deployed application by matching the deployment type along with the cloudApplicationName
                var recommendation = recommendations.FirstOrDefault(x => string.Equals(x.Recipe.Id, deploymentSettings.RecipeId));
                if (recommendation == null)
                {
                    var errorMsg = "The recipe ID specified in the deployment settings file does not match any compatible deployment recipes.";
                    throw new InvalidDeploymentSettingsException(DeployToolErrorCode.DeploymentConfigurationNeedsAdjusting, errorMsg);
                }
                deployedApplication = allDeployedApplications.FirstOrDefault(x => string.Equals(x.Name, cloudApplicationName) && x.DeploymentType == recommendation.Recipe.DeploymentType);
            }
            else
            {
                deployedApplication = allDeployedApplications.FirstOrDefault(x => string.Equals(x.Name, cloudApplicationName));
            }

            Recommendation? selectedRecommendation = null;
            if (deployedApplication != null)
            {
                // Verify that the target application can be deployed using the current set of recommendations
                if (!compatibleApplications.Any(app => app.Name.Equals(deployedApplication.Name, StringComparison.Ordinal)))
                {
                    var errorMessage = $"{deployedApplication.Name} already exists as a {deployedApplication.ResourceType} but a compatible recommendation to perform a redeployment was not found";
                    throw new FailedToFindCompatibleRecipeException(DeployToolErrorCode.CompatibleRecommendationForRedeploymentNotFound, errorMessage);
                }

                // preset settings for deployment based on last deployment.
                selectedRecommendation = await GetSelectedRecommendationFromPreviousDeployment(orchestrator, recommendations, deployedApplication, deploymentSettings, deploymentProjectPath);
            }
            else
            {
                if (!string.IsNullOrEmpty(deploymentProjectPath))
                {
                    selectedRecommendation = recommendations.First();
                }
                else
                {
                    // Filter the recommendation list for a NEW deployment with recipes which have the DisableNewDeployments property set to false.
                    selectedRecommendation = GetSelectedRecommendation(deploymentSettings, recommendations.Where(x => !x.Recipe.DisableNewDeployments).ToList());
                }

                // Ask the user for a new Cloud Application name based on the deployment type of the recipe.
                if (string.IsNullOrEmpty(cloudApplicationName))
                {
                    // Don't prompt for a new name if a user just wants to push images to ECR
                    // The ECR repository name is already configurable as part of the recipe option settings.
                    if (selectedRecommendation.Recipe.DeploymentType == DeploymentTypes.ElasticContainerRegistryImage)
                    {
                        cloudApplicationName = _cloudApplicationNameGenerator.GenerateValidName(_session.ProjectDefinition, compatibleApplications, selectedRecommendation.Recipe.DeploymentType);
                    }
                    else
                    {
                        cloudApplicationName = AskForNewCloudApplicationName(selectedRecommendation.Recipe.DeploymentType, compatibleApplications);
                    }
                }
                // cloudApplication name was already provided via CLI args or the deployment config file
                else
                {
                    var validationResult = _cloudApplicationNameGenerator.IsValidName(cloudApplicationName, allDeployedApplications, selectedRecommendation.Recipe.DeploymentType);
                    if (!validationResult.IsValid)
                        throw new InvalidCloudApplicationNameException(DeployToolErrorCode.InvalidCloudApplicationName, validationResult.ErrorMessage);
                }
            }

            await orchestrator.ApplyAllReplacementTokens(selectedRecommendation, cloudApplicationName);

            var cloudApplication = new CloudApplication(cloudApplicationName, deployedApplication?.UniqueIdentifier ?? string.Empty, orchestrator.GetCloudApplicationResourceType(selectedRecommendation.Recipe.DeploymentType), selectedRecommendation.Recipe.Id);

            return (orchestrator, selectedRecommendation, cloudApplication);
        }

        /// <summary>
        /// Checks if the system meets all the necessary requirements for deployment.
        /// </summary>
        /// <param name="selectedRecommendation">The selected recommendation settings used for deployment.<see cref="Recommendation"/></param>
        public async Task EvaluateSystemCapabilities(Recommendation selectedRecommendation)
        {
            var missingSystemCapabilities = await _systemCapabilityEvaluator.EvaluateSystemCapabilities(selectedRecommendation);
            var missingCapabilitiesMessage = "";
            foreach (var capability in missingSystemCapabilities)
            {
                missingCapabilitiesMessage = $"{missingCapabilitiesMessage}{Environment.NewLine}{capability.GetMessage()}{Environment.NewLine}";
            }

            if (missingSystemCapabilities.Any())
                throw new MissingSystemCapabilityException(DeployToolErrorCode.MissingSystemCapabilities, missingCapabilitiesMessage);
        }

        /// <summary>
        /// Configure option setings using the CLI or a user provided configuration file.
        /// </summary>
        /// <param name="cloudApplication"><see cref="CloudApplication"/></param>
        /// <param name="orchestrator"><see cref="Orchestrator"/></param>
        /// <param name="selectedRecommendation"><see cref="Recommendation"/></param>
        /// <param name="deploymentSettings"><see cref="DeploymentSettings"/></param>
        public async Task ConfigureDeployment(CloudApplication cloudApplication, Orchestrator orchestrator, Recommendation selectedRecommendation, DeploymentSettings? deploymentSettings)
        {
            var configurableOptionSettings = selectedRecommendation.GetConfigurableOptionSettingItems();

            if (deploymentSettings != null)
            {
                await _deploymentSettingsHandler.ApplySettings(deploymentSettings, selectedRecommendation, _session);
            }

            if (!_toolInteractiveService.DisableInteractive)
            {
                await ConfigureDeploymentFromCli(selectedRecommendation, configurableOptionSettings, false);
            }
        }

        private async Task<List<Recommendation>> GenerateDeploymentRecommendations(Orchestrator orchestrator, string deploymentProjectPath)
        {
            List<Recommendation> recommendations;
            if (!string.IsNullOrEmpty(deploymentProjectPath))
            {
                recommendations = await orchestrator.GenerateRecommendationsFromSavedDeploymentProject(deploymentProjectPath);
                if (!recommendations.Any())
                {
                    var errorMessage = $"Could not find any deployment recipe located inside '{deploymentProjectPath}' that can be used for deployment of the target application";
                    throw new FailedToGenerateAnyRecommendations(DeployToolErrorCode.NoDeploymentRecipesFound, errorMessage);
                }
            }
            else
            {
                recommendations = await orchestrator.GenerateDeploymentRecommendations();
                if (!recommendations.Any())
                {
                    var errorMessage = "There are no compatible deployment recommendations for this application.";
                    throw new FailedToGenerateAnyRecommendations(DeployToolErrorCode.NoCompatibleDeploymentRecipesFound, errorMessage);
                }
            }
            return recommendations;
        }

        private async Task<Recommendation> GetSelectedRecommendationFromPreviousDeployment(Orchestrator orchestrator, List<Recommendation> recommendations, CloudApplication deployedApplication, DeploymentSettings? deploymentSettings, string deploymentProjectPath)
        {
            var deploymentSettingRecipeId = deploymentSettings?.RecipeId;
            var selectedRecommendation = await GetRecommendationForRedeployment(recommendations, deployedApplication, deploymentProjectPath);
            if (selectedRecommendation == null)
            {
                var errorMessage = $"{deployedApplication.Name} already exists as a {deployedApplication.ResourceType} but a compatible recommendation used to perform a re-deployment was not found.";
                throw new FailedToFindCompatibleRecipeException(DeployToolErrorCode.CompatibleRecommendationForRedeploymentNotFound, errorMessage);
            }
            if (!string.IsNullOrEmpty(deploymentSettingRecipeId) && !string.Equals(deploymentSettingRecipeId, selectedRecommendation.Recipe.Id, StringComparison.InvariantCultureIgnoreCase))
            {
                var errorMessage = $"The existing {deployedApplication.ResourceType} {deployedApplication.Name} was created from a different deployment recommendation. " +
                    "Deploying to an existing target must be performed with the original deployment recommendation to avoid unintended destructive changes to the resources.";
                if (_toolInteractiveService.Diagnostics)
                {
                    errorMessage += Environment.NewLine + $"The original deployment recipe ID was {deployedApplication.RecipeId} and the current deployment recipe ID is {deploymentSettingRecipeId}";
                }
                throw new InvalidDeploymentSettingsException(DeployToolErrorCode.StackCreatedFromDifferentDeploymentRecommendation, errorMessage.Trim());
            }

            IDictionary<string, object> previousSettings;
            if (deployedApplication.ResourceType == CloudApplicationResourceType.CloudFormationStack)
            {
                var metadata = await _cloudFormationTemplateReader.LoadCloudApplicationMetadata(deployedApplication.Name);
                previousSettings = metadata.Settings.Union(metadata.DeploymentBundleSettings).ToDictionary(x => x.Key, x => x.Value);
            }
            else
            {
                previousSettings = await _deployedApplicationQueryer.GetPreviousSettings(deployedApplication, selectedRecommendation);
            }

            await orchestrator.ApplyAllReplacementTokens(selectedRecommendation, deployedApplication.Name);

            selectedRecommendation = await orchestrator.ApplyRecommendationPreviousSettings(selectedRecommendation, previousSettings);

            var header = $"Loading {deployedApplication.DisplayName} settings:";

            _toolInteractiveService.WriteLine(header);
            _toolInteractiveService.WriteLine(new string('-', header.Length));
            var optionSettings =
                selectedRecommendation
                    .Recipe
                    .OptionSettings
                    .Where(x => _optionSettingHandler.IsSummaryDisplayable(selectedRecommendation, x))
                    .ToArray();

            foreach (var setting in optionSettings)
            {
                DisplayOptionSetting(selectedRecommendation, setting, -1, optionSettings.Length, DisplayOptionSettingsMode.Readonly);
            }

            return selectedRecommendation;
        }

        private async Task<Recommendation?> GetRecommendationForRedeployment(List<Recommendation> recommendations, CloudApplication deployedApplication, string deploymentProjectPath)
        {
            var targetRecipeId = !string.IsNullOrEmpty(deploymentProjectPath) ?
                await GetDeploymentProjectRecipeId(deploymentProjectPath) : deployedApplication.RecipeId;

            foreach (var recommendation in recommendations)
            {
                if (string.Equals(recommendation.Recipe.Id, targetRecipeId) && _deployedApplicationQueryer.IsCompatible(deployedApplication, recommendation))
                {
                    return recommendation;
                }
            }
            return null;
        }

        private async Task<string> GetDeploymentProjectRecipeId(string deploymentProjectPath)
        {
            if (!_directoryManager.Exists(deploymentProjectPath))
            {
                throw new InvalidOperationException($"Invalid deployment project path. {deploymentProjectPath} does not exist on the file system.");
            }

            try
            {
                var recipeFiles = _directoryManager.GetFiles(deploymentProjectPath, "*.recipe");
                if (recipeFiles.Length == 0)
                {
                    throw new InvalidOperationException($"Failed to find a recipe file at {deploymentProjectPath}");
                }
                if (recipeFiles.Length > 1)
                {
                    throw new InvalidOperationException($"Found more than one recipe files at {deploymentProjectPath}. Only one recipe file per deployment project is supported.");
                }

                var recipeFilePath = recipeFiles.First();
                var recipeBody = await _fileManager.ReadAllTextAsync(recipeFilePath);
                var recipe = JsonConvert.DeserializeObject<RecipeDefinition>(recipeBody);
                if (recipe == null)
                    throw new FailedToDeserializeException(DeployToolErrorCode.FailedToDeserializeDeploymentProjectRecipe, $"Failed to deserialize Deployment Project Recipe '{recipeFilePath}'");
                return recipe.Id;
            }
            catch (Exception ex)
            {
                throw new FailedToFindDeploymentProjectRecipeIdException(DeployToolErrorCode.FailedToFindDeploymentProjectRecipeId, $"Failed to find a recipe ID for the deployment project located at {deploymentProjectPath}", ex);
            }
        }

        // This method prompts the user to select a CloudApplication name for existing deployments or create a new one.
        // If a user chooses to create a new CloudApplication, then this method returns string.Empty
        private string AskForCloudApplicationNameFromDeployedApplications(List<CloudApplication> deployedApplications)
        {
            if (!deployedApplications.Any())
                return string.Empty;

            var title = "Select an existing AWS deployment target to deploy your application to.";

            var userInputConfiguration = new UserInputConfiguration<CloudApplication>(
                idSelector: app => app.DisplayName,
                displaySelector: app => app.DisplayName,
                defaultSelector: app => app.DisplayName.Equals(deployedApplications.First().DisplayName))
            {
                AskNewName = false,
                CanBeEmpty = false
            };

            var userResponse = _consoleUtilities.AskUserToChooseOrCreateNew(
                options: deployedApplications,
                title: title,
                userInputConfiguration: userInputConfiguration,
                defaultChoosePrompt: Constants.CLI.PROMPT_CHOOSE_DEPLOYMENT_TARGET,
                defaultCreateNewLabel: Constants.CLI.CREATE_NEW_APPLICATION_LABEL);

            var cloudApplicationName = userResponse.SelectedOption != null ? userResponse.SelectedOption.Name : string.Empty;
            return cloudApplicationName;
        }

        // This method prompts the user for a new CloudApplication name and also generate a valid default name by respecting existing applications.
        private string AskForNewCloudApplicationName(DeploymentTypes deploymentType, List<CloudApplication> deployedApplications)
        {
            if (_toolInteractiveService.DisableInteractive)
            {
                var message = "The \"--silent\" CLI argument can only be used if a cloud application name is provided either via the CLI argument \"--application-name\" or through a deployment-settings file. " +
                "Please provide an application name and try again";
                throw new InvalidCliArgumentException(DeployToolErrorCode.SilentArgumentNeedsApplicationNameArgument, message);
            }

            var defaultName = "";

            try
            {
                defaultName = _cloudApplicationNameGenerator.GenerateValidName(_session.ProjectDefinition, deployedApplications, deploymentType);
            }
            catch (Exception exception)
            {
                _toolInteractiveService.WriteDebugLine(exception.PrettyPrint());
            }

            var cloudApplicationName = string.Empty;

            while (true)
            {
                _toolInteractiveService.WriteLine();

                var title = "Name the Cloud Application to deploy your project to" + Environment.NewLine +
                            "--------------------------------------------------------------------------------";

                string inputPrompt;

                switch (deploymentType)
                {
                    case DeploymentTypes.CdkProject:
                        inputPrompt = Constants.CLI.PROMPT_NEW_STACK_NAME;
                        break;
                    case DeploymentTypes.ElasticContainerRegistryImage:
                        inputPrompt = Constants.CLI.PROMPT_ECR_REPOSITORY_NAME;
                        break;
                    default:
                        throw new InvalidOperationException($"The {nameof(DeploymentTypes)} {deploymentType} does not have an input prompt");
                }

                cloudApplicationName =
                    _consoleUtilities.AskUserForValue(
                        title,
                        defaultName,
                        allowEmpty: false,
                        defaultAskValuePrompt: inputPrompt);

                var validationResult = _cloudApplicationNameGenerator.IsValidName(cloudApplicationName, deployedApplications, deploymentType);
                if (validationResult.IsValid)
                {
                    return cloudApplicationName;
                }

                _toolInteractiveService.WriteLine();
                _toolInteractiveService.WriteErrorLine(validationResult.ErrorMessage);
            }
        }

        /// <summary>
        /// This method is responsible for selecting a deployment recommendation.
        /// </summary>
        /// <param name="deploymentSettings">The deserialized object from the user provided config file.<see cref="DeploymentSettings"/></param>
        /// <param name="recommendations">A List of available recommendations to choose from.</param>
        /// <returns><see cref="Recommendation"/></returns>
        private Recommendation GetSelectedRecommendation(DeploymentSettings? deploymentSettings, List<Recommendation> recommendations)
        {
            var deploymentSettingsRecipeId = deploymentSettings?.RecipeId;

            if (string.IsNullOrEmpty(deploymentSettingsRecipeId))
            {
                if (_toolInteractiveService.DisableInteractive)
                {
                    var message = "The \"--silent\" CLI argument can only be used if a deployment recipe is specified as part of the " +
                    "deployement-settings file or if a path to a custom CDK deployment project is provided via the '--deployment-project' CLI argument." +
                    $"{Environment.NewLine}Please provide a deployment recipe and try again";

                    throw new InvalidCliArgumentException(DeployToolErrorCode.SilentArgumentNeedsDeploymentRecipe, message);
                }
                return _consoleUtilities.AskToChooseRecommendation(recommendations);
            }

            var selectedRecommendation = recommendations.FirstOrDefault(x => x.Recipe.Id.Equals(deploymentSettingsRecipeId, StringComparison.Ordinal));
            if (selectedRecommendation == null)
            {
                throw new InvalidDeploymentSettingsException(DeployToolErrorCode.InvalidPropertyValueForUserDeployment, $"The user deployment settings provided contains an invalid value for the property '{nameof(deploymentSettings.RecipeId)}'.");
            }

            _toolInteractiveService.WriteLine();
            _toolInteractiveService.WriteLine($"Configuring Recommendation with: '{selectedRecommendation.Name}'.");
            return selectedRecommendation;
        }

        private bool ConfirmDeployment(Recommendation recommendation)
        {
            var message = recommendation.Recipe.DeploymentConfirmation?.DefaultMessage;
            if (string.IsNullOrEmpty(message))
                return true;

            var result = _consoleUtilities.AskYesNoQuestion(message);

            return result == YesNo.Yes;
        }

        private async Task CreateDeploymentBundle(Orchestrator orchestrator, Recommendation selectedRecommendation, CloudApplication cloudApplication)
        {
            try
            {
                await orchestrator.CreateDeploymentBundle(cloudApplication, selectedRecommendation);
            }
            catch(FailedToCreateDeploymentBundleException ex) when (ex.ErrorCode == DeployToolErrorCode.DockerBuildFailed)
            {
                if (_toolInteractiveService.DisableInteractive)
                {
                    throw;
                }

                _toolInteractiveService.WriteLine("Docker builds usually fail due to executing them from a working directory that is incompatible with the Dockerfile." +
                            " You can try setting the 'Docker Execution Directory' in the option settings.");

                _toolInteractiveService.WriteLine(string.Empty);
                var answer = _consoleUtilities.AskYesNoQuestion("Do you want to go back and modify the current configuration?", "false");
                if (answer == YesNo.Yes)
                {
                    string dockerExecutionDirectory;
                    do
                    {
                        dockerExecutionDirectory = _consoleUtilities.AskUserForValue(
                            "Enter the docker execution directory where the docker build command will be executed from:",
                            selectedRecommendation.DeploymentBundle.DockerExecutionDirectory,
                            allowEmpty: true);

                        if (!_directoryManager.Exists(dockerExecutionDirectory))
                        {
                            _toolInteractiveService.WriteErrorLine($"Error, directory does not exist \"{dockerExecutionDirectory}\"");
                        }
                    } while (!_directoryManager.Exists(dockerExecutionDirectory));

                    selectedRecommendation.DeploymentBundle.DockerExecutionDirectory = dockerExecutionDirectory;
                    await CreateDeploymentBundle(orchestrator, selectedRecommendation, cloudApplication);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task ConfigureDeploymentFromCli(Recommendation recommendation, IEnumerable<OptionSettingItem> configurableOptionSettings, bool showAdvancedSettings)
        {
            _toolInteractiveService.WriteLine(string.Empty);

            while (true)
            {
                var message = "Current settings (select number to change its value)";
                var title = message + Environment.NewLine + new string('-', message.Length);

                _toolInteractiveService.WriteLine(title);

                var optionSettings =
                    configurableOptionSettings
                        .Where(x => (!recommendation.IsExistingCloudApplication || x.Updatable) && (!x.AdvancedSetting || showAdvancedSettings) && _optionSettingHandler.IsOptionSettingDisplayable(recommendation, x))
                        .ToArray();

                for (var i = 1; i <= optionSettings.Length; i++)
                {
                    DisplayOptionSetting(recommendation, optionSettings[i - 1], i, optionSettings.Length, DisplayOptionSettingsMode.Editable);
                }

                _toolInteractiveService.WriteLine();
                if (!showAdvancedSettings)
                {
                    // Don't bother showing 'more' for advanced options if there aren't any advanced options.
                    if (configurableOptionSettings.Any(x => x.AdvancedSetting))
                    {
                        _toolInteractiveService.WriteLine("Enter 'more' to display Advanced settings. ");
                    }
                }
                _toolInteractiveService.WriteLine("Or press 'Enter' to deploy:");

                var input = _toolInteractiveService.ReadLine();

                // advanced - break to main loop to reprint menu
                if (input.Trim().ToLower().Equals("more"))
                {
                    showAdvancedSettings = true;
                    _toolInteractiveService.WriteLine();
                    continue;
                }

                // deploy case, nothing more to configure
                if (string.IsNullOrEmpty(input))
                {
                    var settingValidatorFailedResults = _optionSettingHandler.RunOptionSettingValidators(recommendation);

                    var recipeValidatorFailedResults = _recipeHandler.RunRecipeValidators(recommendation, _session);

                    if (!settingValidatorFailedResults.Any() && !recipeValidatorFailedResults.Any())
                    {
                        // validation successful
                        // deployment configured
                        return;
                    }

                    _toolInteractiveService.WriteLine();
                    _toolInteractiveService.WriteErrorLine("The deployment configuration needs to be adjusted before it can be deployed:");
                    foreach (var result in settingValidatorFailedResults)
                        _toolInteractiveService.WriteErrorLine($" - {result.ValidationFailedMessage}");
                    foreach (var result in recipeValidatorFailedResults)
                        _toolInteractiveService.WriteErrorLine($" - {result.ValidationFailedMessage}");

                    _toolInteractiveService.WriteLine();
                    _toolInteractiveService.WriteErrorLine("Please adjust your settings");
                }

                // configure option setting
                if (int.TryParse(input, out var selectedNumber) &&
                    selectedNumber >= 1 &&
                    selectedNumber <= optionSettings.Length)
                {
                    await ConfigureDeploymentFromCli(recommendation, optionSettings[selectedNumber - 1]);
                }

                _toolInteractiveService.WriteLine();
            }
        }

        enum DisplayOptionSettingsMode { Editable, Readonly }
        private void DisplayOptionSetting(Recommendation recommendation, OptionSettingItem optionSetting, int optionSettingNumber, int optionSettingsCount, DisplayOptionSettingsMode mode)
        {
            var value = _optionSettingHandler.GetOptionSettingValue(recommendation, optionSetting);

            Type? typeHintResponseType = null;
            if (optionSetting.Type == OptionSettingValueType.Object)
            {
                var typeHintResponseTypeFullName = $"AWS.Deploy.CLI.TypeHintResponses.{optionSetting.TypeHint}TypeHintResponse";
                typeHintResponseType = Assembly.GetExecutingAssembly().GetType(typeHintResponseTypeFullName);
            }

            DisplayValue(recommendation, optionSetting, optionSettingNumber, optionSettingsCount, typeHintResponseType, mode);
        }

        private async Task ConfigureDeploymentFromCli(Recommendation recommendation, OptionSettingItem setting)
        {
            _toolInteractiveService.WriteLine(string.Empty);
            _toolInteractiveService.WriteLine($"{setting.Name}:");
            _toolInteractiveService.WriteLine($"{setting.Description}");

            object currentValue = _optionSettingHandler.GetOptionSettingValue(recommendation, setting);
            object? settingValue = null;

            if (setting.TypeHint.HasValue && _typeHintCommandFactory.GetCommand(setting.TypeHint.Value) is var typeHintCommand && typeHintCommand != null)
            {
                settingValue = await typeHintCommand.Execute(recommendation, setting);
            }
            else
            {
                if (setting.AllowedValues?.Count > 0)
                {
                    var userInputConfig = new UserInputConfiguration<string>(
                        idSelector: x => x,
                        displaySelector: x => setting.ValueMapping.ContainsKey(x) ? setting.ValueMapping[x] : x,
                        defaultSelector: x => x.Equals(currentValue))
                    {
                        CreateNew = false
                    };

                    var userResponse = _consoleUtilities.AskUserToChooseOrCreateNew(setting.AllowedValues, string.Empty, userInputConfig);
                    settingValue = userResponse.SelectedOption;

                    // If they didn't change the value then don't store so we can rely on using the default in the recipe.
                    if (Equals(settingValue, currentValue))
                        return;
                }
                else
                {
                    switch (setting.Type)
                    {
                        case OptionSettingValueType.String:
                        case OptionSettingValueType.Int:
                        case OptionSettingValueType.Double:
                            settingValue = _consoleUtilities.AskUserForValue(string.Empty, currentValue.ToString() ?? "", allowEmpty: true, resetValue: _optionSettingHandler.GetOptionSettingDefaultValue<string>(recommendation, setting) ?? "");
                            break;
                        case OptionSettingValueType.Bool:
                            var answer = _consoleUtilities.AskYesNoQuestion(string.Empty, _optionSettingHandler.GetOptionSettingValue(recommendation, setting).ToString());
                            settingValue = answer == YesNo.Yes ? "true" : "false";
                            break;
                        case OptionSettingValueType.KeyValue:
                            settingValue = _consoleUtilities.AskUserForKeyValue(!string.IsNullOrEmpty(currentValue.ToString()) ? (Dictionary<string, string>) currentValue : new Dictionary<string, string>());
                            break;
                        case OptionSettingValueType.List:
                            var valueList = new SortedSet<string>();
                            if (!string.IsNullOrEmpty(currentValue.ToString()))
                                valueList = ((SortedSet<string>) currentValue).DeepCopy();
                            settingValue = _consoleUtilities.AskUserForList(valueList);
                            break;
                        case OptionSettingValueType.Object:
                            foreach (var childSetting in setting.ChildOptionSettings)
                            {
                                if (_optionSettingHandler.IsOptionSettingDisplayable(recommendation, childSetting) && (!recommendation.IsExistingCloudApplication || childSetting.Updatable))
                                    await ConfigureDeploymentFromCli(recommendation, childSetting);
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            if (settingValue != null)
            {
                try
                {
                    await _optionSettingHandler.SetOptionSettingValue(recommendation, setting, settingValue);
                }
                catch (ValidationFailedException ex)
                {
                    _toolInteractiveService.WriteErrorLine(ex.Message);

                    await ConfigureDeploymentFromCli(recommendation, setting);
                }
            }
        }

        /// <summary>
        /// Uses reflection to call <see cref="IOptionSettingHandler.GetOptionSettingValue{T}" /> with the Object type option setting value
        /// This allows to use a generic implementation to display Object type option setting values without casting the response to
        /// the specific TypeHintResponse type.
        /// </summary>
        private void DisplayValue(Recommendation recommendation, OptionSettingItem optionSetting, int optionSettingNumber, int optionSettingsCount, Type? typeHintResponseType, DisplayOptionSettingsMode mode)
        {
            object? displayValue = null;
            Dictionary<string, string>? keyValuePair = null;
            Dictionary<string, object>? objectValues = null;
            SortedSet<string>? listValues = null;
            if (typeHintResponseType != null)
            {
                var methodInfo = typeof(IOptionSettingHandler)
                    .GetMethod(nameof(IOptionSettingHandler.GetOptionSettingValue), 1, new[] { typeof(Recommendation), typeof(OptionSettingItem) });
                var genericMethodInfo = methodInfo?.MakeGenericMethod(typeHintResponseType);
                var response = genericMethodInfo?.Invoke(_optionSettingHandler, new object[] { recommendation, optionSetting });

                displayValue = ((IDisplayable?)response)?.ToDisplayString();
            }

            if (displayValue == null)
            {
                var value = _optionSettingHandler.GetOptionSettingValue(recommendation, optionSetting);
                objectValues = value as Dictionary<string, object>;
                keyValuePair = value as Dictionary<string, string>;
                listValues = value as SortedSet<string>;
                displayValue = objectValues == null && keyValuePair == null && listValues == null ? value : string.Empty;
            }

            if (mode == DisplayOptionSettingsMode.Editable)
            {
                _toolInteractiveService.WriteLine($"{optionSettingNumber.ToString().PadRight(optionSettingsCount.ToString().Length)}. {optionSetting.Name}: {displayValue}");
            }
            else if (mode == DisplayOptionSettingsMode.Readonly)
            {
                _toolInteractiveService.WriteLine($"{optionSetting.Name}: {displayValue}");
            }

            if (keyValuePair != null)
            {
                foreach (var (key, value) in keyValuePair)
                {
                    _toolInteractiveService.WriteLine($"\t{key}: {value}");
                }
            }

            if (listValues != null)
            {
                foreach (var value in listValues)
                {
                    _toolInteractiveService.WriteLine($"\t{value}");
                }
            }

            if (objectValues != null)
            {
                var displayableValues = new Dictionary<string, object>();
                foreach (var child in optionSetting.ChildOptionSettings)
                {
                    if (!objectValues.ContainsKey(child.Id))
                        continue;
                    displayableValues.Add(child.Name, objectValues[child.Id]);
                }
                _consoleUtilities.DisplayValues(displayableValues, "\t");
            }
        }
    }
}
