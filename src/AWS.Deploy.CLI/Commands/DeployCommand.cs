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
using AWS.Deploy.Orchestrator;
using AWS.Deploy.Recipes;
using AWS.Deploy.Orchestrator.Data;

namespace AWS.Deploy.CLI.Commands
{
    public class DeployCommand
    {
        private readonly IToolInteractiveService _toolInteractiveService;
        private readonly IOrchestratorInteractiveService _orchestratorInteractiveService;
        private readonly ICdkProjectHandler _cdkProjectHandler;
        private readonly IDeploymentBundleHandler _deploymentBundleHandler;
        private readonly IAWSResourceQueryer _awsResourceQueryer;

        private readonly ConsoleUtilities _consoleUtilities;
        private readonly OrchestratorSession _session;
        private readonly TypeHintCommandFactory _typeHintCommandFactory;

        public DeployCommand(
            IToolInteractiveService toolInteractiveService,
            IOrchestratorInteractiveService orchestratorInteractiveService,
            ICdkProjectHandler cdkProjectHandler,
            IDeploymentBundleHandler deploymentBundleHandler,
            IAWSResourceQueryer awsResourceQueryer,
            OrchestratorSession session)
        {
            _toolInteractiveService = toolInteractiveService;
            _orchestratorInteractiveService = orchestratorInteractiveService;
            _cdkProjectHandler = cdkProjectHandler;
            _deploymentBundleHandler = deploymentBundleHandler;
            _awsResourceQueryer = awsResourceQueryer;
            _consoleUtilities = new ConsoleUtilities(toolInteractiveService);
            _session = session;
            _typeHintCommandFactory = new TypeHintCommandFactory(_toolInteractiveService, _awsResourceQueryer, _session, _consoleUtilities);
        }

        public async Task ExecuteAsync(bool saveCdkProject)
        {
            // Ensure a .NET project can be found.
            if (!ProjectDefinition.TryParse(_session.ProjectPath, out var project))
            {
                _toolInteractiveService.WriteErrorLine($"A project was not found at the path {_session.ProjectPath}");
                Environment.Exit(-1);
            }

            var orchestrator =
                new Orchestrator.Orchestrator(
                    _session,
                    _orchestratorInteractiveService,
                    _cdkProjectHandler,
                    _awsResourceQueryer,
                    _deploymentBundleHandler,
                    new[] { RecipeLocator.FindRecipeDefinitionsPath() });

            // Determine what recommendations are possible for the project.
            var recommendations = await orchestrator.GenerateDeploymentRecommendations();
            if (recommendations.Count == 0)
            {
                _toolInteractiveService.WriteLine(string.Empty);
                _toolInteractiveService.WriteErrorLine($"The project you are trying to deploy is currently not supported.");
                throw new FailedToGenerateAnyRecommendations();
            }

            // Look to see if there are any existing deployed applications using any of the compatible recommendations.
            var existingApplications = await orchestrator.GetExistingDeployedApplications(recommendations);

            _toolInteractiveService.WriteLine(string.Empty);

            string cloudApplicationName;
            if (existingApplications.Count == 0)
            {
                var title = "Name the AWS stack to deploy your application to" + Environment.NewLine +
                              "(a stack is a collection of AWS resources that you can manage as a single unit.)" + Environment.NewLine +
                              "--------------------------------------------------------------------------------";
                cloudApplicationName =
                    _consoleUtilities.AskUserForValue(
                        title,
                        GetDefaultApplicationName(new ProjectDefinition(_session.ProjectPath).ProjectPath),
                        allowEmpty: false);
            }
            else
            {
                var title = "Select the AWS stack to deploy your application to" + Environment.NewLine +
                              "(a stack is a collection of AWS resources that you can manage as a single unit.)";

                var userResponse = _consoleUtilities.AskUserToChooseOrCreateNew(existingApplications.Select(x => x.Name).ToList(), title, askNewName: true, defaultNewName: GetDefaultApplicationName(new ProjectDefinition(_session.ProjectPath).ProjectPath));
                cloudApplicationName = userResponse.SelectedOption ?? userResponse.NewName;
            }

            var existingCloudApplication = existingApplications.FirstOrDefault(x => string.Equals(x.Name, cloudApplicationName));

            Recommendation selectedRecommendation = null;

            _toolInteractiveService.WriteLine(string.Empty);
            // If using a previous deployment preset settings for deployment based on last deployment.
            if (existingCloudApplication != null)
            {
                var existingCloudApplicationMetadata = await orchestrator.LoadCloudApplicationMetadata(existingCloudApplication.Name);

                selectedRecommendation = recommendations.FirstOrDefault(x => string.Equals(x.Recipe.Id, existingCloudApplication.RecipeId, StringComparison.InvariantCultureIgnoreCase));
                selectedRecommendation.ApplyPreviousSettings(existingCloudApplicationMetadata.Settings);

                var header = $"Loading {existingCloudApplication.Name} settings:";
                
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
                selectedRecommendation = _consoleUtilities.AskToChooseRecommendation(recommendations);
            }

            // Apply the user enter project name to the recommendation so that any default settings based on project name are applied.
            selectedRecommendation.OverrideProjectName(cloudApplicationName);

            if (selectedRecommendation.Recipe.DeploymentType == DeploymentTypes.CdkProject &&
                !(await _session.SystemCapabilities).NodeJsMinVersionInstalled)
            {
                _toolInteractiveService.WriteErrorLine("The selected deployment option NodeJS 10.3 or later.  Please install NodeJS https://nodejs.org/en/download/");
                throw new MissingNodeJsException();
            }

            if (selectedRecommendation.Recipe.DeploymentBundle == DeploymentBundleTypes.Container &&
                !(await _session.SystemCapabilities).DockerInstalled)
            {
                _toolInteractiveService.WriteErrorLine("The selected deployment option requires Docker which was not detected.  Please install and start the appropriate version of Docker for you OS: https://docs.docker.com/engine/install/");
                throw new MissingDockerException();
            }

            var deploymentBundleDefinition = orchestrator.GetDeploymentBundleDefinition(selectedRecommendation);

            var configurableOptionSettings = selectedRecommendation.Recipe.OptionSettings.Union(deploymentBundleDefinition.Parameters);

            await ConfigureDeployment(selectedRecommendation, configurableOptionSettings, false);

            var cloudApplication = new CloudApplication
            {
                Name = cloudApplicationName
            };

            await CreateDeploymentBundle(orchestrator, selectedRecommendation, cloudApplication);

            await orchestrator.DeployRecommendation(cloudApplication, selectedRecommendation);
        }

        private async Task CreateDeploymentBundle(Orchestrator.Orchestrator orchestrator, Recommendation selectedRecommendation, CloudApplication cloudApplication)
        {
            if (selectedRecommendation.Recipe.DeploymentBundle == DeploymentBundleTypes.Container)
            {
                while (!await orchestrator.CreateContainerDeploymentBundle(cloudApplication, selectedRecommendation))
                {
                    _toolInteractiveService.WriteLine(string.Empty);
                    var answer = _consoleUtilities.AskYesNoQuestion("Do you want to go back and modify the current configuration?", "true");
                    if (answer == ConsoleUtilities.YesNo.Yes)
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
                        _toolInteractiveService.WriteLine(string.Empty);
                        throw new FailedToCreateDeploymentBundleException();
                    }
                }
            }
            else if (selectedRecommendation.Recipe.DeploymentBundle == DeploymentBundleTypes.DotnetPublishZipFile)
            {
                var dotnetPublishDeploymentBundleResult = await orchestrator.CreateDotnetPublishDeploymentBundle(selectedRecommendation);
                if (!dotnetPublishDeploymentBundleResult)
                    throw new FailedToCreateDeploymentBundleException();
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
                    if(configurableOptionSettings.Any(x => x.AdvancedSetting))
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
                    return;
                }

                // configure option setting
                if (int.TryParse(input, out var selectedNumber) &&
                    selectedNumber >= 1 &&
                    selectedNumber <= optionSettings.Length)
                {
                    await ConfigureDeployment(recommendation, optionSettings[selectedNumber - 1]);

                    _toolInteractiveService.WriteLine(string.Empty);
                }

                _toolInteractiveService.WriteLine(string.Empty);
            }
        }

        enum DisplayOptionSettingsMode {Editable, Readonly}
        private void DisplayOptionSetting(Recommendation recommendation, OptionSettingItem optionSetting, int optionSettingNumber, int optionSettingsCount, DisplayOptionSettingsMode mode)
        {
            var value = recommendation.GetOptionSettingValue(optionSetting);

            Type typeHintResponseType = null;
            if(optionSetting.Type == OptionSettingValueType.Object)
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
            _toolInteractiveService.WriteLine(string.Empty);

            var currentValue = recommendation.GetOptionSettingValue(setting);
            object settingValue = null;
            if (setting.AllowedValues?.Count > 0)
            {
                var userInputConfig = new UserInputConfiguration<string>
                {
                    DisplaySelector = x => setting.ValueMapping[x],
                    DefaultSelector = x => x.Equals(currentValue),
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
                            settingValue = _consoleUtilities.AskUserForValue(string.Empty, currentValue?.ToString(), allowEmpty: true, resetValue: recommendation.GetOptionSettingDefaultValue<string>(setting));
                            break;
                        case OptionSettingValueType.Bool:
                            var answer = _consoleUtilities.AskYesNoQuestion(string.Empty, recommendation.GetOptionSettingValue(setting).ToString());
                            settingValue = answer == ConsoleUtilities.YesNo.Yes ? "true" : "false";
                            break;
                        case OptionSettingValueType.Object:
                            foreach (var childSetting in setting.ChildOptionSettings)
                            {
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
                setting.SetValueOverride(settingValue);
            }
        }

        /// <summary>
        /// Uses reflection to call <see cref="Recommendation.GetOptionSettingValue{T}" /> with the Object type option setting value
        /// This allows to use a generic implementation to display Object type option setting values without casting the response to
        /// the specific TypeHintResponse type.
        /// </summary>
        private void DisplayValue(Recommendation recommendation, OptionSettingItem optionSetting, int optionSettingNumber, int optionSettingsCount, Type typeHintResponseType, DisplayOptionSettingsMode mode)
        {
            object displayValue = null;
            Dictionary<string, object> objectValues = null;
            if (typeHintResponseType != null)
            {
                var methodInfo = typeof(Recommendation)
                    .GetMethod(nameof(Recommendation.GetOptionSettingValue), 1, new[] { typeof(OptionSettingItem), typeof(bool) });
                var genericMethodInfo = methodInfo?.MakeGenericMethod(typeHintResponseType);
                var response = genericMethodInfo?.Invoke(recommendation, new object[] { optionSetting, false });

                displayValue = ((IDisplayable)response)?.ToDisplayString();
            }
            else
            {
                var value = recommendation.GetOptionSettingValue(optionSetting);
                objectValues = value as Dictionary<string, object>;
                displayValue = objectValues == null ? value : string.Empty;
            }

            if(mode == DisplayOptionSettingsMode.Editable)
            {
                _toolInteractiveService.WriteLine($"{optionSettingNumber.ToString().PadRight(optionSettingsCount.ToString().Length)}. {optionSetting.Name}: {displayValue}");
            }
            else if(mode == DisplayOptionSettingsMode.Readonly)
            {
                _toolInteractiveService.WriteLine($"{optionSetting.Name}: {displayValue}");
            }

            if (objectValues != null)
            {
                _consoleUtilities.DisplayValues(objectValues, "\t");
            }
        }

        public static string GetDefaultApplicationName(string projectPath)
        {
            if (File.Exists(projectPath))
                return Path.GetFileNameWithoutExtension(projectPath);

            return new DirectoryInfo(projectPath).Name;
        }


    }
}
