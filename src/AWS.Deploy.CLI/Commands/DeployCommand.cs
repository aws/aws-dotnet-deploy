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

namespace AWS.Deploy.CLI.Commands
{
    public class DeployCommand
    {
        
        private readonly IToolInteractiveService _toolInteractiveService;
        private readonly IOrchestratorInteractiveService _orchestratorInteractiveService;
        private readonly ICdkProjectHandler _cdkProjectHandler;
        private readonly ICDKManager _cdkManager;
        private readonly IDeploymentBundleHandler _deploymentBundleHandler;
        private readonly IDockerEngine _dockerEngine;
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly ITemplateMetadataReader _templateMetadataReader;
        private readonly IDeployedApplicationQueryer _deployedApplicationQueryer;
        private readonly ITypeHintCommandFactory _typeHintCommandFactory;
        private readonly IDisplayedResourcesHandler _displayedResourcesHandler;
        private readonly ICloudApplicationNameGenerator _cloudApplicationNameGenerator;
        private readonly ILocalUserSettingsEngine _localUserSettingsEngine;
        private readonly IConsoleUtilities _consoleUtilities;
        private readonly ICustomRecipeLocator _customRecipeLocator;
        private readonly ISystemCapabilityEvaluator _systemCapabilityEvaluator;
        private readonly OrchestratorSession _session;
        private readonly IDirectoryManager _directoryManager;
        private readonly IFileManager _fileManager;
        private readonly ICDKVersionDetector _cdkVersionDetector;
        private readonly IAWSServiceHandler _awsServiceHandler;

        public DeployCommand(
            IToolInteractiveService toolInteractiveService,
            IOrchestratorInteractiveService orchestratorInteractiveService,
            ICdkProjectHandler cdkProjectHandler,
            ICDKManager cdkManager,
            ICDKVersionDetector cdkVersionDetector,
            IDeploymentBundleHandler deploymentBundleHandler,
            IDockerEngine dockerEngine,
            IAWSResourceQueryer awsResourceQueryer,
            ITemplateMetadataReader templateMetadataReader,
            IDeployedApplicationQueryer deployedApplicationQueryer,
            ITypeHintCommandFactory typeHintCommandFactory,
            IDisplayedResourcesHandler displayedResourcesHandler,
            ICloudApplicationNameGenerator cloudApplicationNameGenerator,
            ILocalUserSettingsEngine localUserSettingsEngine,
            IConsoleUtilities consoleUtilities,
            ICustomRecipeLocator customRecipeLocator,
            ISystemCapabilityEvaluator systemCapabilityEvaluator,
            OrchestratorSession session,
            IDirectoryManager directoryManager,
            IFileManager fileManager,
            IAWSServiceHandler awsServiceHandler)
        {
            _toolInteractiveService = toolInteractiveService;
            _orchestratorInteractiveService = orchestratorInteractiveService;
            _cdkProjectHandler = cdkProjectHandler;
            _deploymentBundleHandler = deploymentBundleHandler;
            _dockerEngine = dockerEngine;
            _awsResourceQueryer = awsResourceQueryer;
            _templateMetadataReader = templateMetadataReader;
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
            _customRecipeLocator = customRecipeLocator;
            _systemCapabilityEvaluator = systemCapabilityEvaluator;
            _awsServiceHandler = awsServiceHandler;
        }

        public async Task ExecuteAsync(string applicationName, string deploymentProjectPath, UserDeploymentSettings? userDeploymentSettings = null)
        {
            var (orchestrator, selectedRecommendation, cloudApplication) = await InitializeDeployment(applicationName, userDeploymentSettings, deploymentProjectPath);

            // Verify Docker installation and minimum NodeJS version.
            await EvaluateSystemCapabilities(selectedRecommendation);

            // Configure option settings.
            await ConfigureDeployment(cloudApplication, orchestrator, selectedRecommendation, userDeploymentSettings);

            if (!ConfirmDeployment(selectedRecommendation))
            {
                return;
            }

            await CreateDeploymentBundle(orchestrator, selectedRecommendation, cloudApplication);

            await orchestrator.DeployRecommendation(cloudApplication, selectedRecommendation);

            var displayedResources = await _displayedResourcesHandler.GetDeploymentOutputs(cloudApplication, selectedRecommendation);
            DisplayOutputResources(displayedResources);
        }

        private void DisplayOutputResources(List<DisplayedResourceItem> displayedResourceItems)
        {
            _toolInteractiveService.WriteLine("Resources");
            _toolInteractiveService.WriteLine("---------");
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
        /// <param name="applicationName">The cloud application name provided via the --application-name CLI argument</param>
        /// <param name="userDeploymentSettings">The deserialized object from the user provided config file.<see cref="UserDeploymentSettings"/></param>
        /// <param name="deploymentProjectPath">The absolute or relative path of the CDK project that will be used for deployment</param>
        /// <returns>A tuple consisting of the Orchestrator object, Selected Recommendation, Cloud Application metadata.</returns>
        public async Task<(Orchestrator, Recommendation, CloudApplication)> InitializeDeployment(string applicationName, UserDeploymentSettings? userDeploymentSettings, string deploymentProjectPath)
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
                    _customRecipeLocator,
                    new List<string> { RecipeLocator.FindRecipeDefinitionsPath() },
                    _fileManager,
                    _directoryManager,
                    _awsServiceHandler);

            // Determine what recommendations are possible for the project.
            var recommendations = await GenerateDeploymentRecommendations(orchestrator, deploymentProjectPath);

            // Get all existing applications that were previously deployed using our deploy tool.
            var allDeployedApplications = await _deployedApplicationQueryer.GetExistingDeployedApplications(recommendations.Select(x => x.Recipe.DeploymentType).ToList());

            // Filter compatible applications that can be re-deployed  using the current set of recommendations.
            var compatibleApplications = await _deployedApplicationQueryer.GetCompatibleApplications(recommendations, allDeployedApplications, _session);

            // Get CloudApplication name.
            var cloudApplicationName = GetCloudApplicationName(applicationName, userDeploymentSettings, compatibleApplications);

            // Find existing application with the same CloudApplication name.
            var deployedApplication = allDeployedApplications.FirstOrDefault(x => string.Equals(x.Name, cloudApplicationName));

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
                selectedRecommendation = await GetSelectedRecommendationFromPreviousDeployment(recommendations, deployedApplication, userDeploymentSettings, deploymentProjectPath);
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
                    selectedRecommendation = GetSelectedRecommendation(userDeploymentSettings, recommendations.Where(x => !x.Recipe.DisableNewDeployments).ToList());
                }
            }

            // Apply the user entered application name to the recommendation so that any default settings based on CloudFormation stack name are applied.
            // Currently only Cloudformation based recipes require the {StackName} replacement token
            selectedRecommendation.AddReplacementToken(Constants.RecipeIdentifier.REPLACE_TOKEN_STACK_NAME, cloudApplicationName);

            var cloudApplication = new CloudApplication(cloudApplicationName, deployedApplication?.UniqueIdentifier ?? string.Empty, deployedApplication?.ResourceType ?? CloudApplicationResourceType.CloudFormationStack, selectedRecommendation.Recipe.Id);

            return (orchestrator, selectedRecommendation, cloudApplication);
        }

        /// <summary>
        /// Checks if the system meets all the necessary requirements for deployment.
        /// </summary>
        /// <param name="selectedRecommendation">The selected recommendation settings used for deployment.<see cref="Recommendation"/></param>
        public async Task EvaluateSystemCapabilities(Recommendation selectedRecommendation)
        {
            var systemCapabilities = await _systemCapabilityEvaluator.EvaluateSystemCapabilities(selectedRecommendation);
            var missingCapabilitiesMessage = "";
            foreach (var capability in systemCapabilities)
            {
                missingCapabilitiesMessage = $"{missingCapabilitiesMessage}{capability.GetMessage()}{Environment.NewLine}";
            }

            if (systemCapabilities.Any())
                throw new MissingSystemCapabilityException(DeployToolErrorCode.MissingSystemCapabilities, missingCapabilitiesMessage);
        }

        /// <summary>
        /// Configure option setings using the CLI or a user provided configuration file.
        /// </summary>
        /// <param name="cloudApplication"><see cref="CloudApplication"/></param>
        /// <param name="orchestrator"><see cref="Orchestrator"/></param>
        /// <param name="selectedRecommendation"><see cref="Recommendation"/></param>
        /// <param name="userDeploymentSettings"><see cref="UserDeploymentSettings"/></param>
        public async Task ConfigureDeployment(CloudApplication cloudApplication, Orchestrator orchestrator, Recommendation selectedRecommendation, UserDeploymentSettings? userDeploymentSettings)
        {
            var configurableOptionSettings = selectedRecommendation.GetConfigurableOptionSettingItems();

            if (userDeploymentSettings != null)
            {
                ConfigureDeploymentFromConfigFile(selectedRecommendation, userDeploymentSettings);
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

        private async Task<Recommendation> GetSelectedRecommendationFromPreviousDeployment(List<Recommendation> recommendations, CloudApplication deployedApplication, UserDeploymentSettings? userDeploymentSettings, string deploymentProjectPath)
        {
            var deploymentSettingRecipeId = userDeploymentSettings?.RecipeId;
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
                throw new InvalidUserDeploymentSettingsException(DeployToolErrorCode.StackCreatedFromDifferentDeploymentRecommendation, errorMessage.Trim());
            }

            IDictionary<string, object> previousSettings;
            if (deployedApplication.ResourceType == CloudApplicationResourceType.CloudFormationStack)
                previousSettings = (await _templateMetadataReader.LoadCloudApplicationMetadata(deployedApplication.Name)).Settings;
            else
                previousSettings = await _deployedApplicationQueryer.GetPreviousSettings(deployedApplication);

            selectedRecommendation = selectedRecommendation.ApplyPreviousSettings(previousSettings);

            var header = $"Loading {deployedApplication.DisplayName} settings:";

            _toolInteractiveService.WriteLine(header);
            _toolInteractiveService.WriteLine(new string('-', header.Length));
            var optionSettings =
                selectedRecommendation
                    .Recipe
                    .OptionSettings
                    .Where(x => selectedRecommendation.IsSummaryDisplayable(x))
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
                return recipe.Id;
            }
            catch (Exception ex)
            {
                throw new FailedToFindDeploymentProjectRecipeIdException(DeployToolErrorCode.FailedToFindDeploymentProjectRecipeId, $"Failed to find a recipe ID for the deployment project located at {deploymentProjectPath}", ex);
            }
        }

        /// <summary>
        /// This method is used to set the values for Option Setting Items when a deployment is being performed using a user specifed config file.
        /// </summary>
        /// <param name="recommendation">The selected recommendation settings used for deployment <see cref="Recommendation"/></param>
        /// <param name="userDeploymentSettings">The deserialized object from the user provided config file. <see cref="UserDeploymentSettings"/></param>
        private void ConfigureDeploymentFromConfigFile(Recommendation recommendation, UserDeploymentSettings userDeploymentSettings)
        {
            foreach (var entry in userDeploymentSettings.LeafOptionSettingItems)
            {
                var optionSettingJsonPath = entry.Key;
                var optionSettingValue = entry.Value;

                var optionSetting = recommendation.GetOptionSetting(optionSettingJsonPath);

                if (optionSetting == null)
                    throw new OptionSettingItemDoesNotExistException(DeployToolErrorCode.OptionSettingItemDoesNotExistInRecipe, $"The Option Setting Item {optionSettingJsonPath} does not exist.");

                if (!recommendation.IsExistingCloudApplication || optionSetting.Updatable)
                {
                    object settingValue;
                    try
                    {
                        switch (optionSetting.Type)
                        {
                            case OptionSettingValueType.String:
                                settingValue = optionSettingValue;
                                break;
                            case OptionSettingValueType.Int:
                                settingValue = int.Parse(optionSettingValue);
                                break;
                            case OptionSettingValueType.Bool:
                                settingValue = bool.Parse(optionSettingValue);
                                break;
                            case OptionSettingValueType.Double:
                                settingValue = double.Parse(optionSettingValue);
                                break;
                            case OptionSettingValueType.KeyValue:
                                var optionSettingKey = optionSettingJsonPath.Split(".").Last();
                                var existingValue = recommendation.GetOptionSettingValue<Dictionary<string, string>>(optionSetting);
                                existingValue ??= new Dictionary<string, string>();
                                existingValue[optionSettingKey] = optionSettingValue;
                                settingValue = existingValue;
                                break;
                            default:
                                throw new InvalidOverrideValueException(DeployToolErrorCode.InvalidValueForOptionSettingItem, $"Invalid value {optionSettingValue} for option setting item {optionSettingJsonPath}");
                        }
                    }
                    catch (Exception exception)
                    {
                        _toolInteractiveService.WriteDebugLine(exception.PrettyPrint());
                        throw new InvalidOverrideValueException(DeployToolErrorCode.InvalidValueForOptionSettingItem, $"Invalid value {optionSettingValue} for option setting item {optionSettingJsonPath}");
                    }

                    optionSetting.SetValueOverride(settingValue);

                    SetDeploymentBundleOptionSetting(recommendation, optionSetting.Id, settingValue);
                }
            }

            var validatorFailedResults =
                        recommendation.Recipe
                            .BuildValidators()
                            .Select(validator => validator.Validate(recommendation, _session))
                            .Where(x => !x.IsValid)
                            .ToList();

            if (!validatorFailedResults.Any())
            {
                // validation successful
                // deployment configured
                return;
            }

            var errorMessage = "The deployment configuration needs to be adjusted before it can be deployed:" + Environment.NewLine;
            foreach (var result in validatorFailedResults)
            {
                errorMessage += result.ValidationFailedMessage + Environment.NewLine;
            }
            throw new InvalidUserDeploymentSettingsException(DeployToolErrorCode.DeploymentConfigurationNeedsAdjusting, errorMessage.Trim());
        }

        private void SetDeploymentBundleOptionSetting(Recommendation recommendation, string optionSettingId, object settingValue)
        {
            switch (optionSettingId)
            {
                case "DockerExecutionDirectory":
                    new DockerExecutionDirectoryCommand(_consoleUtilities, _directoryManager).OverrideValue(recommendation, settingValue.ToString() ?? "");
                    break;
                case "DockerBuildArgs":
                    new DockerBuildArgsCommand(_consoleUtilities).OverrideValue(recommendation, settingValue.ToString() ?? "");
                    break;
                case "DotnetBuildConfiguration":
                    new DotnetPublishBuildConfigurationCommand(_consoleUtilities).Overridevalue(recommendation, settingValue.ToString() ?? "");
                    break;
                case "DotnetPublishArgs":
                    new DotnetPublishArgsCommand(_consoleUtilities).OverrideValue(recommendation, settingValue.ToString() ?? "");
                    break;
                case "SelfContainedBuild":
                    new DotnetPublishSelfContainedBuildCommand(_consoleUtilities).OverrideValue(recommendation, (bool)settingValue);
                    break;
                default:
                    return;
            }
        }

        private string GetCloudApplicationName(string? applicationName, UserDeploymentSettings? userDeploymentSettings, List<CloudApplication> deployedApplications)
        {
            // validate the applicationName provided by the --application-name cli argument if present.
            if (!string.IsNullOrEmpty(applicationName))
            {
                if (_cloudApplicationNameGenerator.IsValidName(applicationName))
                    return applicationName;

                PrintInvalidApplicationNameMessage();
                throw new InvalidCliArgumentException(DeployToolErrorCode.InvalidCliArguments, "Found invalid CLI arguments");
            }

            if (!string.IsNullOrEmpty(userDeploymentSettings?.ApplicationName))
            {
                if (_cloudApplicationNameGenerator.IsValidName(userDeploymentSettings.ApplicationName))
                    return userDeploymentSettings.ApplicationName;

                PrintInvalidApplicationNameMessage();
                throw new InvalidUserDeploymentSettingsException(DeployToolErrorCode.UserDeploymentInvalidStackName, "Please provide a valid stack name and try again.");
            }

            if (_toolInteractiveService.DisableInteractive)
            {
                var message = "The \"--silent\" CLI argument can only be used if a cloud application name is provided either via the CLI argument \"--application-name\" or through a deployment-settings file. " +
                "Please provide an application name and try again";
                throw new InvalidCliArgumentException(DeployToolErrorCode.SilentArgumentNeedsApplicationNameArgument, message);
            }
            return AskUserForCloudApplicationName(_session.ProjectDefinition, deployedApplications);
        }

        /// <summary>
        /// This method is responsible for selecting a deployment recommendation.
        /// </summary>
        /// <param name="userDeploymentSettings">The deserialized object from the user provided config file.<see cref="UserDeploymentSettings"/></param>
        /// <param name="recommendations">A List of available recommendations to choose from.</param>
        /// <returns><see cref="Recommendation"/></returns>
        private Recommendation GetSelectedRecommendation(UserDeploymentSettings? userDeploymentSettings, List<Recommendation> recommendations)
        {
            var deploymentSettingsRecipeId = userDeploymentSettings?.RecipeId;

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
                throw new InvalidUserDeploymentSettingsException(DeployToolErrorCode.InvalidPropertyValueForUserDeployment, $"The user deployment settings provided contains an invalid value for the property '{nameof(userDeploymentSettings.RecipeId)}'.");
            }

            _toolInteractiveService.WriteLine();
            _toolInteractiveService.WriteLine($"Configuring Recommendation with: '{selectedRecommendation.Name}'.");
            return selectedRecommendation;
        }

        private string AskUserForCloudApplicationName(ProjectDefinition project, List<CloudApplication> existingApplications)
        {
            var defaultName = "";

            try
            {
                defaultName = _cloudApplicationNameGenerator.GenerateValidName(project, existingApplications);
            }
            catch (Exception exception)
            {
                _toolInteractiveService.WriteDebugLine(exception.PrettyPrint());
            }

            var cloudApplicationName = "";

            while (true)
            {
                _toolInteractiveService.WriteLine();

                if (!existingApplications.Any())
                {
                    var title = "Name the AWS CloudFormation stack to deploy your application to" + Environment.NewLine +
                                "(A stack is a collection of AWS resources that you can manage as a single unit.)" + Environment.NewLine +
                                "--------------------------------------------------------------------------------";

                    cloudApplicationName =
                        _consoleUtilities.AskUserForValue(
                            title,
                            defaultName,
                            allowEmpty: false,
                            defaultAskValuePrompt: Constants.CLI.PROMPT_NEW_STACK_NAME);
                }
                else
                {
                    var title = "Select an existing AWS deployment target to deploy your application to." + Environment.NewLine +
                        "The existing target can be a CloudFormation Stack or an Elastic Beanstalk Environment.";

                    var userResponse =
                        _consoleUtilities.AskUserToChooseOrCreateNew(
                            existingApplications.Select(x => x.DisplayName),
                            title,
                            askNewName: true,
                            defaultNewName: defaultName,
                            defaultChoosePrompt: Constants.CLI.PROMPT_CHOOSE_DEPLOYMENT_TARGET,
                            defaultCreateNewPrompt: Constants.CLI.PROMPT_NEW_STACK_NAME,
                            defaultCreateNewLabel: Constants.CLI.CREATE_NEW_STACK_LABEL) ;

                    // Since the selected option will be the display name, we need to extract the actual name out of it.
                    // Ex - DisplayName = "MyAppStack (CloudFormationStack)", ActualName = "MyAppStack"
                    cloudApplicationName = userResponse.SelectedOption?.Split().FirstOrDefault() ?? userResponse.NewName;
                }

                if (!string.IsNullOrEmpty(cloudApplicationName) &&
                    _cloudApplicationNameGenerator.IsValidName(cloudApplicationName))
                    return cloudApplicationName;

                PrintInvalidApplicationNameMessage();
            }
        }

        private void PrintInvalidApplicationNameMessage()
        {
            _toolInteractiveService.WriteLine();
            _toolInteractiveService.WriteErrorLine(
                "Invalid application name. The application name can contain only alphanumeric characters (case-sensitive) and hyphens. " +
                "It must start with an alphabetic character and can't be longer than 128 characters");
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
            if (selectedRecommendation.Recipe.DeploymentBundle == DeploymentBundleTypes.Container)
            {
                while (!await orchestrator.CreateContainerDeploymentBundle(cloudApplication, selectedRecommendation))
                {
                    if (_toolInteractiveService.DisableInteractive)
                    {
                        var errorMessage = "Failed to build Docker Image." + Environment.NewLine;
                        errorMessage += "Docker builds usually fail due to executing them from a working directory that is incompatible with the Dockerfile." + Environment.NewLine;
                        errorMessage += "Specify a valid Docker execution directory as part of the deployment settings file and try again.";
                        throw new DockerBuildFailedException(DeployToolErrorCode.DockerBuildFailed, errorMessage);
                    }

                    _toolInteractiveService.WriteLine(string.Empty);
                    var answer = _consoleUtilities.AskYesNoQuestion("Do you want to go back and modify the current configuration?", "false");
                    if (answer == YesNo.Yes)
                    {
                        var dockerExecutionDirectory =
                        _consoleUtilities.AskUserForValue(
                            "Enter the docker execution directory where the docker build command will be executed from:",
                            selectedRecommendation.DeploymentBundle.DockerExecutionDirectory,
                            allowEmpty: true);

                        if (!_directoryManager.Exists(dockerExecutionDirectory))
                            continue;

                        selectedRecommendation.DeploymentBundle.DockerExecutionDirectory = dockerExecutionDirectory;
                    }
                    else
                    {
                        throw new FailedToCreateDeploymentBundleException(DeployToolErrorCode.FailedToCreateContainerDeploymentBundle, "Failed to create a deployment bundle");
                    }
                }
            }
            else if (selectedRecommendation.Recipe.DeploymentBundle == DeploymentBundleTypes.DotnetPublishZipFile)
            {
                var dotnetPublishDeploymentBundleResult = await orchestrator.CreateDotnetPublishDeploymentBundle(selectedRecommendation);
                if (!dotnetPublishDeploymentBundleResult)
                    throw new FailedToCreateDeploymentBundleException(DeployToolErrorCode.FailedToCreateDotnetPublishDeploymentBundle, "Failed to create a deployment bundle");
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
                        .Where(x => (!recommendation.IsExistingCloudApplication || x.Updatable) && (!x.AdvancedSetting || showAdvancedSettings) && recommendation.IsOptionSettingDisplayable(x))
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
                    var validatorFailedResults =
                        recommendation.Recipe
                            .BuildValidators()
                            .Select(validator => validator.Validate(recommendation, _session))
                            .Where(x => !x.IsValid)
                            .ToList();

                    if (!validatorFailedResults.Any())
                    {
                        // validation successful
                        // deployment configured
                        return;
                    }

                    _toolInteractiveService.WriteLine();
                    _toolInteractiveService.WriteErrorLine("The deployment configuration needs to be adjusted before it can be deployed:");
                    foreach (var result in validatorFailedResults)
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
            var value = recommendation.GetOptionSettingValue(optionSetting);

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

            object currentValue = recommendation.GetOptionSettingValue(setting);
            object? settingValue = null;
            if (setting.AllowedValues?.Count > 0)
            {
                var userInputConfig = new UserInputConfiguration<string>(
                    x => setting.ValueMapping.ContainsKey(x) ? setting.ValueMapping[x] : x,
                    x => x.Equals(currentValue))
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
                if (setting.TypeHint.HasValue && _typeHintCommandFactory.GetCommand(setting.TypeHint.Value) is var typeHintCommand && typeHintCommand != null)
                {
                    settingValue = await typeHintCommand.Execute(recommendation, setting);
                }
                else
                {
                    switch (setting.Type)
                    {
                        case OptionSettingValueType.String:
                        case OptionSettingValueType.Int:
                        case OptionSettingValueType.Double:
                            settingValue = _consoleUtilities.AskUserForValue(string.Empty, currentValue.ToString() ?? "", allowEmpty: true, resetValue: recommendation.GetOptionSettingDefaultValue<string>(setting) ?? "");
                            break;
                        case OptionSettingValueType.Bool:
                            var answer = _consoleUtilities.AskYesNoQuestion(string.Empty, recommendation.GetOptionSettingValue(setting).ToString());
                            settingValue = answer == YesNo.Yes ? "true" : "false";
                            break;
                        case OptionSettingValueType.KeyValue:
                            settingValue = _consoleUtilities.AskUserForKeyValue(!string.IsNullOrEmpty(currentValue.ToString()) ? (Dictionary<string, string>) currentValue : new Dictionary<string, string>());
                            break;
                        case OptionSettingValueType.Object:
                            foreach (var childSetting in setting.ChildOptionSettings)
                            {
                                if (recommendation.IsOptionSettingDisplayable(childSetting))
                                    await ConfigureDeploymentFromCli(recommendation, childSetting);
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            if (!Equals(settingValue, currentValue) && settingValue != null)
            {
                try
                {
                    setting.SetValueOverride(settingValue);
                }
                catch (ValidationFailedException ex)
                {
                    _toolInteractiveService.WriteErrorLine(
                        $"Value [{settingValue}] is not valid: {ex.Message}");

                    await ConfigureDeploymentFromCli(recommendation, setting);
                }
            }
        }

        /// <summary>
        /// Uses reflection to call <see cref="Recommendation.GetOptionSettingValue{T}" /> with the Object type option setting value
        /// This allows to use a generic implementation to display Object type option setting values without casting the response to
        /// the specific TypeHintResponse type.
        /// </summary>
        private void DisplayValue(Recommendation recommendation, OptionSettingItem optionSetting, int optionSettingNumber, int optionSettingsCount, Type? typeHintResponseType, DisplayOptionSettingsMode mode)
        {
            object? displayValue = null;
            Dictionary<string, string>? keyValuePair = null;
            Dictionary<string, object>? objectValues = null;
            if (typeHintResponseType != null)
            {
                var methodInfo = typeof(Recommendation)
                    .GetMethod(nameof(Recommendation.GetOptionSettingValue), 1, new[] { typeof(OptionSettingItem) });
                var genericMethodInfo = methodInfo?.MakeGenericMethod(typeHintResponseType);
                var response = genericMethodInfo?.Invoke(recommendation, new object[] { optionSetting });

                displayValue = ((IDisplayable?)response)?.ToDisplayString();
            }
            else
            {
                var value = recommendation.GetOptionSettingValue(optionSetting);
                objectValues = value as Dictionary<string, object>;
                keyValuePair = value as Dictionary<string, string>;
                displayValue = objectValues == null && keyValuePair == null ? value : string.Empty;
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
