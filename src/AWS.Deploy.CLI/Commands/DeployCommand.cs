// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly IAWSResourceQueryer _awsResourceQueryer;

        private readonly ConsoleUtilities _consoleUtilities;
        private readonly OrchestratorSession _session;

        public DeployCommand(
            IToolInteractiveService toolInteractiveService,
            IOrchestratorInteractiveService orchestratorInteractiveService,
            ICdkProjectHandler cdkProjectHandler,
            IAWSResourceQueryer awsResourceQueryer,
            OrchestratorSession session)
        {
            _toolInteractiveService = toolInteractiveService;
            _orchestratorInteractiveService = orchestratorInteractiveService;
            _cdkProjectHandler = cdkProjectHandler;
            _awsResourceQueryer = awsResourceQueryer;
            _consoleUtilities = new ConsoleUtilities(toolInteractiveService);
            _session = session;
        }

        public async Task ExecuteAsync(bool saveCdkProject)
        {
            var orchestrator =
                new Orchestrator.Orchestrator(
                    _session,
                    _orchestratorInteractiveService,
                    _cdkProjectHandler,
                    new []{ RecipeLocator.FindRecipeDefinitionsPath() });

            var previousSettings = orchestrator.GetPreviousDeploymentSettings();
            var previousDeploymentNames = previousSettings.GetDeploymentNames();

            _toolInteractiveService.WriteLine(string.Empty);

            string cloudApplicationName;
            if (previousSettings.Deployments.Count == 0)
            {
                if (!ProjectDefinition.TryParse(_session.ProjectPath, out var project))
                {
                    _toolInteractiveService.WriteErrorLine($"A project was not found at the path {_session.ProjectPath}");
                    Environment.Exit(-1);
                }

                cloudApplicationName =
                    _consoleUtilities.AskUserForValue(
                        "Enter name for Cloud Application",
                        GetDefaultApplicationName(new ProjectDefinition(_session.ProjectPath).ProjectPath),
                        allowEmpty: false);
            }
            else
            {
                cloudApplicationName = _consoleUtilities.AskUserToChooseOrCreateNew(previousDeploymentNames.ToList(), "Select Cloud Application to deploy to", null);
            }

            var previousDeployment = previousSettings.Deployments.FirstOrDefault(x => string.Equals(x.StackName, cloudApplicationName));

            var recommendations = orchestrator.GenerateDeploymentRecommendations();

            if (recommendations.Count == 0)
            {
                _toolInteractiveService.WriteErrorLine($"Unable to determine a method for deploying application: {_session.ProjectPath}");
                throw new FailedToGenerateAnyRecommendations();
            }

            _toolInteractiveService.WriteLine(string.Empty);

            Recommendation selectedRecommendation = null;

            // If there was a previous deployment be sure to make that recipe be the top recommendation.
            if (previousDeployment != null)
            {
                selectedRecommendation = recommendations.FirstOrDefault(x => string.Equals(x.Recipe.Id, previousDeployment.RecipeId, StringComparison.InvariantCultureIgnoreCase));
                selectedRecommendation.ApplyPreviousSettings(previousDeployment?.RecipeOverrideSettings);
            }
            else
            {
                selectedRecommendation = _consoleUtilities.AskUserToChoose(recommendations, "Available options to deploy project", recommendations[0]);
            }

            if (selectedRecommendation.Recipe.DeploymentType == DeploymentTypes.CdkProject &&
                !(await _session.SystemCapabilities).NodeJsMinVersionInstalled)
            {
                _toolInteractiveService.WriteErrorLine("The selected Recipe requires NodeJS 10.3 or later.  Please install NodeJS https://nodejs.org/en/download/");
                throw new MissingNodeJsException();
            }

            if (selectedRecommendation.Recipe.DeploymentBundle == DeploymentBundleTypes.Container &&
                !(await _session.SystemCapabilities).DockerInstalled)
            {
                _toolInteractiveService.WriteErrorLine("The selected Recipe requires docker but docker was not detected as running.  Please install and start docker: https://docs.docker.com/engine/install/");
                throw new MissingDockerException();
            }

            await ConfigureDeployment(selectedRecommendation, false);

            var cloudApplication = new CloudApplication
            {
                Name = cloudApplicationName
            };

            await orchestrator.DeployRecommendation(cloudApplication, selectedRecommendation);
        }

        private async Task ConfigureDeployment(Recommendation recommendation, bool showAdvancedSettings)
        {
            _toolInteractiveService.WriteLine(string.Empty);

            while (true)
            {
                var title =
                    (showAdvancedSettings) ? "Select the setting you want to configure:" : "Below are the settings we'll use to deploy:";

                _toolInteractiveService.WriteLine(title);

                var optionSettings =
                    recommendation
                        .Recipe
                        .OptionSettings
                        .Where(x => !x.AdvancedSetting || showAdvancedSettings)
                        .ToArray();

                for (var i = 1; i <= optionSettings.Length; i++)
                {
                    _toolInteractiveService.WriteLine($"{i}. {optionSettings[i - 1].Name}: {recommendation.GetOptionSettingValue(optionSettings[i - 1].Id)}");
                }

                _toolInteractiveService.WriteLine();
                _toolInteractiveService.WriteLine("Select a number to change its value.");
                if (!showAdvancedSettings)
                {
                    _toolInteractiveService.WriteLine("Enter 'more' to include Advanced settings. ");
                }
                else
                {
                    _toolInteractiveService.WriteLine("(Advanced settings are displayed)");
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
                    await ConfigureOptionSetting(optionSettings[selectedNumber - 1], recommendation);

                    _toolInteractiveService.WriteLine(string.Empty);

                    var additionalConfig = _consoleUtilities.AskYesNoQuestion("Do you want to do any additional configuration?", "false");
                    
                    if (additionalConfig == ConsoleUtilities.YesNo.No)
                        return;
                    // If yes is selected, we will loop back into the prompt
                }

                _toolInteractiveService.WriteLine(string.Empty);
            }
        }

        private async Task ConfigureOptionSetting(OptionSettingItem setting, Recommendation recommendation)
        {
            var isDisplayed = true;
            PropertyDependency failedDependency = null;

            foreach (var dependency in setting.DependsOn)
            {
                var dependsOnValue = recommendation.GetOptionSettingValue(dependency.Id);
                if (!dependsOnValue.Equals(dependency.Value))
                {
                    isDisplayed = false;
                    failedDependency = dependency;
                    recommendation.SetOverrideOptionSettingValue(setting.Id, setting.DefaultValue);
                    break;
                }
            }

            if (!isDisplayed)
            {
                var dependentOption = recommendation.Recipe.OptionSettings.First(x => x.Id.Equals(failedDependency.Id)).Name;
                _toolInteractiveService.WriteLine(string.Empty);
                _toolInteractiveService.WriteLine(
                    $"{setting.Name} depends on '{dependentOption}' to have the value '{failedDependency.Value}'");
                _toolInteractiveService.WriteLine($"Please configure '{dependentOption}' to have the value '{failedDependency.Value}' first, or select another setting.");
                _toolInteractiveService.WriteLine(string.Empty);
                return;
            }

            _toolInteractiveService.WriteLine(string.Empty);
            _toolInteractiveService.WriteLine($"{setting.Name}:");

            var currentValue = recommendation.GetOptionSettingValue(setting.Id);
            object settingValue = null;
            if (setting.AllowedValues?.Count > 0)
            {
                _toolInteractiveService.WriteLine(setting.Description);
                settingValue = _consoleUtilities.AskUserToChoose(setting.AllowedValues, null, currentValue?.ToString());
            }
            else if (setting.TypeHint == OptionSettingTypeHint.BeanstalkApplication)
            {
                _toolInteractiveService.WriteLine(setting.Description);

                var applications = await _awsResourceQueryer.GetListOfElasticBeanstalkApplications(_session);

                settingValue = _consoleUtilities.AskUserToChooseOrCreateNew(applications,
                    "Select Beanstalk application to deploy to:",
                    currentValue?.ToString());

                if (applications.Contains(settingValue.ToString()))
                    recommendation.SetOverrideOptionSettingValue("UseExistingApplication", "true");
            }
            else if (setting.TypeHint == OptionSettingTypeHint.BeanstalkEnvironment)
            {
                _toolInteractiveService.WriteLine(setting.Description);

                var applicationName = recommendation.GetOptionSettingValue(setting.ParentSettingId) as string;
                var environments = await _awsResourceQueryer.GetListOfElasticBeanstalkEnvironments(_session, applicationName);

                settingValue = _consoleUtilities.AskUserToChooseOrCreateNew(environments,
                    "Select Beanstalk environment to deploy to:",
                    currentValue?.ToString());
            }
            else if (setting.TypeHint == OptionSettingTypeHint.DotnetPublishArgs)
            {
                settingValue =
                    _consoleUtilities
                        .AskUserForValue(
                                setting.Description,
                                recommendation.GetOptionSettingValue(setting.Id).ToString(),
                                 allowEmpty: true,
                                // validators:
                                publishArgs =>
                                        (publishArgs.Contains("-o ") || publishArgs.Contains("--output "))
                                        ? "You must not include -o/--output as an additional argument as it is used internally."
                                        : "",
                                publishArgs =>
                                        (publishArgs.Contains("-c ") || publishArgs.Contains("--configuration ")
                                        ? "You must not include -c/--configuration as an additional argument. You can set the build configuration in the advanced settings."
                                        : ""),
                                publishArgs =>
                                        (publishArgs.Contains("--self-contained") || publishArgs.Contains("--no-self-contained")
                                        ? "You must not include --self-contained/--no-self-contained as an additional argument. You can set the self-contained property in the advanced settings."
                                        : ""))
                        .ToString()
                        .Replace("\"", "\"\"");
            }
            else if (setting.TypeHint == OptionSettingTypeHint.EC2KeyPair)
            {
                _toolInteractiveService.WriteLine(setting.Description);
                var keyPairs = await _awsResourceQueryer.GetListOfEC2KeyPairs(_session);

                while (true)
                {
                    settingValue = _consoleUtilities.AskUserToChooseOrCreateNew(keyPairs,
                        "Select Key Pair to use:",
                        currentValue?.ToString(), true);

                    if (!string.IsNullOrEmpty(settingValue.ToString()) && !keyPairs.Contains(settingValue.ToString()))
                    {
                        _toolInteractiveService.WriteLine(string.Empty);
                        _toolInteractiveService.WriteLine("You have chosen to create a new Key Pair.");
                        _toolInteractiveService.WriteLine("You are required to specify a directory to save the key pair private key.");

                        var answer = _consoleUtilities.AskYesNoQuestion("Do you want to continue?", "false");
                        if (answer == ConsoleUtilities.YesNo.No)
                            continue;

                        _toolInteractiveService.WriteLine(string.Empty);
                        _toolInteractiveService.WriteLine($"A new Key Pair will be created with the name {settingValue}.");

                        var keyPairDirectory = _consoleUtilities.AskForEC2KeyPairSaveDirectory(recommendation.ProjectPath);

                        await _awsResourceQueryer.CreateEC2KeyPair(_session, settingValue.ToString(), keyPairDirectory);
                    }

                    break;
                }
            }
            else if (setting.Type == OptionSettingValueType.Bool)
            {
                var answer = _consoleUtilities.AskYesNoQuestion(setting.Description, recommendation.GetOptionSettingValue(setting.Id).ToString());
                settingValue = answer == ConsoleUtilities.YesNo.Yes ? "true" : "false";
            }
            else
            {
                settingValue = _consoleUtilities.AskUserForValue(setting.Description, currentValue.ToString(), allowEmpty: true);
            }

            if (!Equals(settingValue, currentValue) && settingValue != null)
            {
                recommendation.SetOverrideOptionSettingValue(setting.Id, settingValue);
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
