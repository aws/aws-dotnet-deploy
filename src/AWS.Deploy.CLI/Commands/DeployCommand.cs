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
        private readonly ICloudApplicationNameGenerator _cloudApplicationNameGenerator;

        private readonly IConsoleUtilities _consoleUtilities;
        private readonly OrchestratorSession _session;

        public DeployCommand(
            IToolInteractiveService toolInteractiveService,
            IOrchestratorInteractiveService orchestratorInteractiveService,
            ICdkProjectHandler cdkProjectHandler,
            ICDKManager cdkManager,
            IDeploymentBundleHandler deploymentBundleHandler,
            IDockerEngine dockerEngine,
            IAWSResourceQueryer awsResourceQueryer,
            ITemplateMetadataReader templateMetadataReader,
            IDeployedApplicationQueryer deployedApplicationQueryer,
            ITypeHintCommandFactory typeHintCommandFactory,
            ICloudApplicationNameGenerator cloudApplicationNameGenerator,
            IConsoleUtilities consoleUtilities,
            OrchestratorSession session)
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
            _cloudApplicationNameGenerator = cloudApplicationNameGenerator;
            _consoleUtilities = consoleUtilities;
            _session = session;
            _cdkManager = cdkManager;
        }

        public async Task ExecuteAsync(string stackName, bool saveCdkProject, UserDeploymentSettings? userDeploymentSettings = null)
        {
            var orchestrator =
                new Orchestrator(
                    _session,
                    _orchestratorInteractiveService,
                    _cdkProjectHandler,
                    _cdkManager,
                    _awsResourceQueryer,
                    _deploymentBundleHandler,
                    _dockerEngine,
                    new[] { RecipeLocator.FindRecipeDefinitionsPath() });

            // Determine what recommendations are possible for the project.
            var recommendations = await orchestrator.GenerateDeploymentRecommendations();
            if (recommendations.Count == 0)
            {
                throw new FailedToGenerateAnyRecommendations("The project you are trying to deploy is currently not supported.");
            }

            // Look to see if there are any existing deployed applications using any of the compatible recommendations.
            var deployedApplications = await _deployedApplicationQueryer.GetExistingDeployedApplications(recommendations);

            if (!string.IsNullOrEmpty(stackName) && !_cloudApplicationNameGenerator.IsValidName(stackName))
            {
                PrintInvalidStackNameMessage();
                throw new InvalidCliArgumentException("Found invalid CLI arguments");
            }

            _toolInteractiveService.WriteLine();

            var cloudApplicationName = GetCloudApplicationName(stackName, userDeploymentSettings, deployedApplications);

            var deployedApplication = deployedApplications.FirstOrDefault(x => string.Equals(x.Name, cloudApplicationName));

            Recommendation? selectedRecommendation = null;

            _toolInteractiveService.WriteLine();

            // If using a previous deployment preset settings for deployment based on last deployment.
            if (deployedApplication != null)
            {
                var existingCloudApplicationMetadata = await _templateMetadataReader.LoadCloudApplicationMetadata(deployedApplication.Name);

                selectedRecommendation = recommendations.FirstOrDefault(x => string.Equals(x.Recipe.Id, deployedApplication.RecipeId, StringComparison.InvariantCultureIgnoreCase));

                if (selectedRecommendation == null)
                    throw new FailedToFindCompatibleRecipeException("A compatible recipe was not found for the deployed application.");

                if (userDeploymentSettings != null && !string.IsNullOrEmpty(userDeploymentSettings.RecipeId))
                {
                    if (!string.Equals(userDeploymentSettings.RecipeId, selectedRecommendation.Recipe.Id, StringComparison.InvariantCultureIgnoreCase))
                        throw new InvalidUserDeploymentSettingsException("The recipe ID specified as part of the deployment configuration file" +
                            " does not match the original recipe used to deploy the application stack.");
                }

                selectedRecommendation.ApplyPreviousSettings(existingCloudApplicationMetadata.Settings);

                var header = $"Loading {deployedApplication.Name} settings:";

                _toolInteractiveService.WriteLine(header);
                _toolInteractiveService.WriteLine(new string('-', header.Length));
                var optionSettings =
                    selectedRecommendation
                        .Recipe
                        .OptionSettings
                        .Where(x =>
                            {
                                if (!selectedRecommendation.IsOptionSettingDisplayable(x))
                                    return false;

                                var value = selectedRecommendation.GetOptionSettingValue(x);
                                if (value == null || value.ToString() == string.Empty || object.Equals(value, x.DefaultValue))
                                    return false;

                                return true;
                            })
                        .ToArray();

                foreach (var setting in optionSettings)
                {
                    DisplayOptionSetting(selectedRecommendation, setting, -1, optionSettings.Length, DisplayOptionSettingsMode.Readonly);
                }
            }
            else
            {
                selectedRecommendation = GetSelectedRecommendation(userDeploymentSettings, recommendations);
            }

            // Apply the user enter project name to the recommendation so that any default settings based on project name are applied.
            selectedRecommendation.OverrideProjectName(cloudApplicationName);

            if (_session.SystemCapabilities == null)
                throw new SystemCapabilitiesNotProvidedException("The system capabilities were not provided.");

            var systemCapabilities = await _session.SystemCapabilities;
            if (selectedRecommendation.Recipe.DeploymentType == DeploymentTypes.CdkProject &&
                !systemCapabilities.NodeJsMinVersionInstalled)
            {
                throw new MissingNodeJsException("The selected deployment uses the AWS CDK, which requires version of Node.js higher than your current installation. The latest LTS version of Node.js is recommended and can be installed from https://nodejs.org/en/download/. Specifically, AWS CDK requires 10.3+ to work properly.");
            }

            if (selectedRecommendation.Recipe.DeploymentBundle == DeploymentBundleTypes.Container)
            {
                if (!systemCapabilities.DockerInfo.DockerInstalled)
                {
                    throw new MissingDockerException("The selected deployment option requires Docker, which was not detected. Please install and start the appropriate version of Docker for you OS: https://docs.docker.com/engine/install/");
                }

                if (!systemCapabilities.DockerInfo.DockerContainerType.Equals("linux", StringComparison.OrdinalIgnoreCase))
                {
                    throw new DockerContainerTypeException("The deployment tool requires Docker to be running in linux mode. Please switch Docker to linux mode to continue.");
                }
            }

            var deploymentBundleDefinition = orchestrator.GetDeploymentBundleDefinition(selectedRecommendation);

            var configurableOptionSettings = selectedRecommendation.Recipe.OptionSettings.Union(deploymentBundleDefinition.Parameters);

            if (userDeploymentSettings != null)
            {
                ConfigureDeployment(selectedRecommendation, deploymentBundleDefinition, userDeploymentSettings);
            }
            
            if (!_toolInteractiveService.DisableInteractive)
            {
                await ConfigureDeployment(selectedRecommendation, configurableOptionSettings, false);
            } 

            var cloudApplication = new CloudApplication(cloudApplicationName, string.Empty);

            if (!ConfirmDeployment(selectedRecommendation))
            {
                return;
            }

            await CreateDeploymentBundle(orchestrator, selectedRecommendation, cloudApplication);

            await orchestrator.DeployRecommendation(cloudApplication, selectedRecommendation);
        }

        /// <summary>
        /// This method is used to set the values for Option Setting Items when a deployment is being performed using a user specifed config file.
        /// </summary>
        /// <param name="recommendation">The selected recommendation settings used for deployment <see cref="Recommendation"/></param>
        /// <param name="deploymentBundleDefinition">The container for the deployment bundle used by an application. <see cref="DeploymentBundleDefinition"/></param>
        /// <param name="userDeploymentSettings">The deserialized object from the user provided config file. <see cref="UserDeploymentSettings"></param>
        private void ConfigureDeployment(Recommendation recommendation, DeploymentBundleDefinition deploymentBundleDefinition, UserDeploymentSettings userDeploymentSettings)
        {
            foreach (var entry in userDeploymentSettings.LeafOptionSettingItems)
            {
                var optionSettingJsonPath = entry.Key;
                var optionSettingValue = entry.Value;

                var isPartOfDeploymentBundle = true;
                var optionSetting = deploymentBundleDefinition.Parameters.FirstOrDefault(x => x.Id.Equals(optionSettingJsonPath));
                if (optionSetting == null)
                {
                    optionSetting = recommendation.GetOptionSetting(optionSettingJsonPath);
                    isPartOfDeploymentBundle = false;
                }
                    
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
                            default:
                                throw new InvalidOverrideValueException($"Invalid value {optionSettingValue} for option setting item {optionSettingJsonPath}");
                        }
                    }
                    catch (Exception)
                    {
                        throw new InvalidOverrideValueException($"Invalid value {optionSettingValue} for option setting item {optionSettingJsonPath}");
                    }
                    
                    optionSetting.SetValueOverride(settingValue);

                    if (isPartOfDeploymentBundle)
                        SetDeploymentBundleOptionSetting(recommendation, optionSetting.Id, settingValue);
                }
            }
        }

        private void SetDeploymentBundleOptionSetting(Recommendation recommendation, string optionSettingId, object settingValue)
        {
            switch (optionSettingId)
            {
                case "DockerExecutionDirectory":
                    new DockerExecutionDirectoryCommand(_consoleUtilities).OverrideValue(recommendation, settingValue.ToString() ?? "");
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
                    throw new OptionSettingItemDoesNotExistException($"The Option Setting Item { optionSettingId } does not exist.");
            }
        }

        private string GetCloudApplicationName(string? stackName, UserDeploymentSettings? userDeploymentSettings, List<CloudApplication> deployedApplications)
        {
            if (!string.IsNullOrEmpty(stackName))
                return stackName;

            if (userDeploymentSettings == null || string.IsNullOrEmpty(userDeploymentSettings.StackName))
            {
                if (_toolInteractiveService.DisableInteractive)
                {
                    var message = "The \"--silent\" CLI argument can only be used if a CDK stack name is provided either via the CLI argument \"--stack-name\" or through a deployment-settings file. " +
                    "Please provide a stack name and try again";
                    throw new InvalidCliArgumentException(message);
                }
                return AskUserForCloudApplicationName(_session.ProjectDefinition, deployedApplications);
            }
            
            _toolInteractiveService.WriteLine($"Configuring Stack Name with specified value '{userDeploymentSettings.StackName}'.");
            return userDeploymentSettings.StackName;
        }

        private Recommendation GetSelectedRecommendation(UserDeploymentSettings? userDeploymentSettings, List<Recommendation> recommendations)
        {
            if (userDeploymentSettings == null || string.IsNullOrEmpty(userDeploymentSettings.RecipeId))
            {
                if (_toolInteractiveService.DisableInteractive)
                {
                    var message = "The \"--silent\" CLI argument can only be used if a deployment recipe is specified as part of the deployement-settings file. " +
                    "Please provide a deployment recipe and try again";
                    throw new InvalidCliArgumentException(message);
                }
                return _consoleUtilities.AskToChooseRecommendation(recommendations);
            }

            Recommendation? selectedRecommendation = recommendations.FirstOrDefault(x => x.Recipe.Id.Equals(userDeploymentSettings.RecipeId));

            if (selectedRecommendation == null)
                throw new InvalidUserDeploymentSettingsException($"The user deployment settings provided contains an invalid value for the property '{nameof(userDeploymentSettings.RecipeId)}'.");

            _toolInteractiveService.WriteLine($"Configuring Recommendation with specified value '{selectedRecommendation.Name}'.");
            return selectedRecommendation;
        }

        private string AskUserForCloudApplicationName(ProjectDefinition project, List<CloudApplication> existingApplications)
        {
            var defaultName = "";

            try
            {
                defaultName = _cloudApplicationNameGenerator.GenerateValidName(project, existingApplications);
            }
            catch { }

            var cloudApplicationName = "";

            while (true)
            {
                _toolInteractiveService.WriteLine();

                if (!existingApplications.Any())
                {
                    var title = "Name the AWS stack to deploy your application to" + Environment.NewLine +
                                "(A stack is a collection of AWS resources that you can manage as a single unit.)" + Environment.NewLine +
                                "--------------------------------------------------------------------------------";

                    cloudApplicationName =
                        _consoleUtilities.AskUserForValue(
                            title,
                            defaultName,
                            allowEmpty: false);
                }
                else
                {
                    var title = "Select the AWS stack to deploy your application to" + Environment.NewLine +
                                "(A stack is a collection of AWS resources that you can manage as a single unit.)";

                    var userResponse =
                        _consoleUtilities.AskUserToChooseOrCreateNew(
                            existingApplications.Select(x => x.Name),
                            title,
                            askNewName: true,
                            defaultNewName: defaultName);

                    cloudApplicationName = userResponse.SelectedOption ?? userResponse.NewName;
                }

                if (!string.IsNullOrEmpty(cloudApplicationName) &&
                    _cloudApplicationNameGenerator.IsValidName(cloudApplicationName))
                    return cloudApplicationName;

                PrintInvalidStackNameMessage();
            }
        }

        private void PrintInvalidStackNameMessage()
        {
            _toolInteractiveService.WriteLine();
            _toolInteractiveService.WriteLine(
                "Invalid stack name.  A stack name can contain only alphanumeric characters (case-sensitive) and hyphens. " +
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
                        throw new DockerBuildFailedException(errorMessage);
                    }
                        
                    _toolInteractiveService.WriteLine(string.Empty);
                    var answer = _consoleUtilities.AskYesNoQuestion("Do you want to go back and modify the current configuration?", "true");
                    if (answer == YesNo.Yes)
                    {
                        var dockerExecutionDirectory =
                        _consoleUtilities.AskUserForValue(
                            "Enter the docker execution directory where the docker build command will be executed from:",
                            selectedRecommendation.DeploymentBundle.DockerExecutionDirectory,
                            allowEmpty: true);

                        if (!Directory.Exists(dockerExecutionDirectory))
                            continue;

                        selectedRecommendation.DeploymentBundle.DockerExecutionDirectory = dockerExecutionDirectory;
                    }
                    else
                    {
                        throw new FailedToCreateDeploymentBundleException("Failed to create a deployment bundle");
                    }
                }
            }
            else if (selectedRecommendation.Recipe.DeploymentBundle == DeploymentBundleTypes.DotnetPublishZipFile)
            {
                var dotnetPublishDeploymentBundleResult = await orchestrator.CreateDotnetPublishDeploymentBundle(selectedRecommendation);
                if (!dotnetPublishDeploymentBundleResult)
                    throw new FailedToCreateDeploymentBundleException("Failed to create a deployment bundle");
            }
        }

        private async Task ConfigureDeployment(Recommendation recommendation, IEnumerable<OptionSettingItem> configurableOptionSettings, bool showAdvancedSettings)
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
                            .Select(validator => validator.Validate(recommendation.Recipe, _session))
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
                    await ConfigureDeployment(recommendation, optionSettings[selectedNumber - 1]);
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

        private async Task ConfigureDeployment(Recommendation recommendation, OptionSettingItem setting)
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
                            settingValue = _consoleUtilities.AskUserForValue(string.Empty, currentValue.ToString() ?? "", allowEmpty: true, resetValue: recommendation.GetOptionSettingDefaultValue<string>(setting) ?? "");
                            break;
                        case OptionSettingValueType.Bool:
                            var answer = _consoleUtilities.AskYesNoQuestion(string.Empty, recommendation.GetOptionSettingValue(setting).ToString());
                            settingValue = answer == YesNo.Yes ? "true" : "false";
                            break;
                        case OptionSettingValueType.Object:
                            foreach (var childSetting in setting.ChildOptionSettings)
                            {
                                if (recommendation.IsOptionSettingDisplayable(childSetting))
                                    await ConfigureDeployment(recommendation, childSetting);
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

                    await ConfigureDeployment(recommendation, setting);
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
                displayValue = objectValues == null ? value : string.Empty;
            }

            if (mode == DisplayOptionSettingsMode.Editable)
            {
                _toolInteractiveService.WriteLine($"{optionSettingNumber.ToString().PadRight(optionSettingsCount.ToString().Length)}. {optionSetting.Name}: {displayValue}");
            }
            else if (mode == DisplayOptionSettingsMode.Readonly)
            {
                _toolInteractiveService.WriteLine($"{optionSetting.Name}: {displayValue}");
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
