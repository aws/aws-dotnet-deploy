// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Amazon.EC2.Model;
using Amazon.ElasticBeanstalk.Model;
using Amazon.IdentityManagement.Model;
using AWS.Deploy.CLI.TypeHintResponses;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.TypeHintData;
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
                    new []{ RecipeLocator.FindRecipeDefinitionsPath() });

            // Determine what recommendations are possible for the project.
            var recommendations = await orchestrator.GenerateDeploymentRecommendations();
            if (recommendations.Count == 0)
            {
                _toolInteractiveService.WriteErrorLine($"Unable to determine a method for deploying application: {_session.ProjectPath}");
                throw new FailedToGenerateAnyRecommendations();
            }

            // Look to see if there are any existing deployed applications using any of the compatible recommendations.
            var existingApplications = await orchestrator.GetExistingDeployedApplications(recommendations);

            _toolInteractiveService.WriteLine(string.Empty);

            string cloudApplicationName;
            if (existingApplications.Count == 0)
            {
                cloudApplicationName =
                    _consoleUtilities.AskUserForValue(
                        "Enter name for Cloud Application",
                        GetDefaultApplicationName(new ProjectDefinition(_session.ProjectPath).ProjectPath),
                        allowEmpty: false);
            }
            else
            {
                var userResponse = _consoleUtilities.AskUserToChooseOrCreateNew(existingApplications.Select(x => x.Name).ToList(), "Select Cloud Application to deploy to", true);
                cloudApplicationName = userResponse.SelectedOption ?? userResponse.NewName;
            }

            var existingCloudApplication = existingApplications.FirstOrDefault(x => string.Equals(x.Name, cloudApplicationName));

            Recommendation selectedRecommendation = null;

            // If using a previous deployment preset settings for deployment based on last deployment.
            if (existingCloudApplication != null)
            {
                var existingCloudApplicationMetadata = await orchestrator.LoadCloudApplicationMetadataAsync(existingCloudApplication.Name);

                selectedRecommendation = recommendations.FirstOrDefault(x => string.Equals(x.Recipe.Id, existingCloudApplication.RecipeId, StringComparison.InvariantCultureIgnoreCase));
                selectedRecommendation.ApplyPreviousSettings(existingCloudApplicationMetadata.Settings);
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
                    DisplayOptionSetting(recommendation, optionSettings[i-1], i);
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
                    await ConfigureDeployment(recommendation, optionSettings[selectedNumber - 1]);

                    _toolInteractiveService.WriteLine(string.Empty);

                    var additionalConfig = _consoleUtilities.AskYesNoQuestion("Do you want to do any additional configuration?", "false");

                    if (additionalConfig == ConsoleUtilities.YesNo.No)
                        return;
                    // If yes is selected, we will loop back into the prompt
                }

                _toolInteractiveService.WriteLine(string.Empty);
            }
        }

        private void DisplayOptionSetting(Recommendation recommendation, OptionSettingItem optionSetting, int optionSettingNumber)
        {
            var value = recommendation.GetOptionSettingValue(optionSetting);

            switch (optionSetting.Type)
            {
                case OptionSettingValueType.Bool:
                case OptionSettingValueType.Int:
                case OptionSettingValueType.String:
                    _toolInteractiveService.WriteLine($"{optionSettingNumber}. {optionSetting.Name}: {value}");
                    break;
                case OptionSettingValueType.Object:
                    var typeHintResponseTypeFullName = $"AWS.Deploy.CLI.TypeHintResponses.{optionSetting.TypeHint}TypeHintResponse";
                    var typeHintResponseType = Assembly.GetExecutingAssembly().GetType(typeHintResponseTypeFullName);
                    if (typeHintResponseType != null)
                    {
                        DisplayValue(recommendation, optionSetting, optionSettingNumber, typeHintResponseType);
                    }
                    else
                    {
                        if (value is Dictionary<string, object> objectValues)
                        {
                            _toolInteractiveService.WriteLine($"{optionSettingNumber}. {optionSetting.Name}:");
                            DisplayValues(objectValues, "\t");
                        }
                        else
                        {
                            throw new ArgumentOutOfRangeException(optionSetting.Id);
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(optionSetting.Id);
            }
        }

        private async Task ConfigureDeployment(Recommendation recommendation, OptionSettingItem setting)
        {
            var isDisplayed = true;
            PropertyDependency failedDependency = null;

            foreach (var dependency in setting.DependsOn)
            {
                var dependsOnOptionSetting = recommendation.GetOptionSetting(setting.Id);
                if (dependsOnOptionSetting != null && !recommendation.GetOptionSettingValue(dependsOnOptionSetting).Equals(dependency.Value))
                {
                    isDisplayed = false;
                    failedDependency = dependency;
                    setting.SetValueOverride(setting.DefaultValue);
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

            var currentValue = recommendation.GetOptionSettingValue(setting);
            object settingValue = null;
            if (setting.AllowedValues?.Count > 0)
            {
                _toolInteractiveService.WriteLine(setting.Description);
                settingValue = _consoleUtilities.AskUserToChoose(setting.AllowedValues, null, currentValue?.ToString());

                // If they didn't change the value then don't store so we can rely on using the default in the recipe.
                if (Equals(settingValue, currentValue))
                    return;
            }
            if (setting.TypeHint == OptionSettingTypeHint.BeanstalkEnvironment)
            {
                _toolInteractiveService.WriteLine(setting.Description);

                var applicationOptionSetting = recommendation.GetOptionSetting(setting.ParentSettingId);

                var applicationName = recommendation.GetOptionSettingValue(applicationOptionSetting) as string;
                var environments = await _awsResourceQueryer.ListOfElasticBeanstalkEnvironments(_session, applicationName);

                var userResponse = _consoleUtilities.AskUserToChooseOrCreateNew(
                    options: environments.Select(env => env.EnvironmentName),
                    title: "Select Beanstalk environment to deploy to:",
                    askNewName: true,
                    defaultNewName: currentValue.ToString());
                settingValue = userResponse.SelectedOption ?? userResponse.NewName;
            }
            else if (setting.TypeHint == OptionSettingTypeHint.DotnetPublishArgs)
            {
                settingValue =
                    _consoleUtilities
                        .AskUserForValue(
                                setting.Description,
                                recommendation.GetOptionSettingValue(setting).ToString(),
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
                var keyPairs = await _awsResourceQueryer.ListOfEC2KeyPairs(_session);

                var userInputConfiguration = new UserInputConfiguration<KeyPairInfo>
                {
                    DisplaySelector = kp => kp.KeyName,
                    DefaultSelector = kp => kp.KeyName.Equals(currentValue),
                    AskNewName = true
                };

                while (true)
                {
                    var userResponse = _consoleUtilities.AskUserToChooseOrCreateNew(keyPairs, "Select Key Pair to use:", userInputConfiguration);

                    settingValue = userResponse.SelectedOption?.KeyName ?? userResponse.NewName;

                    if (userResponse.CreateNew && !string.IsNullOrEmpty(userResponse.NewName))
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
            else if (setting.TypeHint == OptionSettingTypeHint.DotnetBeanstalkPlatformArn)
            {
                _toolInteractiveService.WriteLine(setting.Description);

                var platformArns = await _awsResourceQueryer.GetElasticBeanstalkPlatformArns(_session);

                var userInputConfiguration = new UserInputConfiguration<PlatformSummary>
                {
                    DisplaySelector = platform => $"{platform.PlatformBranchName} v{platform.PlatformVersion}",
                    DefaultSelector = platform => platform.PlatformArn.Equals(currentValue),
                    CreateNew = false
                };

                var userResponse = _consoleUtilities.AskUserToChooseOrCreateNew(platformArns, "Select the Platform to use:", userInputConfiguration);

                settingValue = userResponse.SelectedOption?.PlatformArn;
            }
            else if (setting.Type == OptionSettingValueType.Bool)
            {
                var answer = _consoleUtilities.AskYesNoQuestion(setting.Description, recommendation.GetOptionSettingValue(setting).ToString());
                settingValue = answer == ConsoleUtilities.YesNo.Yes ? "true" : "false";
            }
            else if (setting.Type == OptionSettingValueType.Object)
            {
                if (setting.TypeHint == OptionSettingTypeHint.IAMRole)
                {
                    _toolInteractiveService.WriteLine(setting.Description);
                    var typeHintData = setting.GetTypeHintData<IAMRoleTypeHintData>();
                    var existingRoles = await _awsResourceQueryer.ListOfIAMRoles(_session, typeHintData?.ServicePrincipal);
                    var currentTypeHintResponse = recommendation.GetOptionSettingValue<IAMRoleTypeHintResponse>(setting);

                    var userInputConfiguration = new UserInputConfiguration<Role>
                    {
                        DisplaySelector = role => role.RoleName,
                        DefaultSelector = role => currentTypeHintResponse.RoleArn?.Equals(role.Arn) ?? false,
                    };

                    var userResponse = _consoleUtilities.AskUserToChooseOrCreateNew(existingRoles ,"Select an IAM role", userInputConfiguration);

                    settingValue = new IAMRoleTypeHintResponse
                    {
                        CreateNew = userResponse.CreateNew,
                        RoleArn = userResponse.SelectedOption?.Arn
                    };
                }
                else if (setting.TypeHint == OptionSettingTypeHint.BeanstalkApplication)
                {
                    _toolInteractiveService.WriteLine(setting.Description);

                    var applications = await _awsResourceQueryer.ListOfElasticBeanstalkApplications(_session);
                    var currentTypeHintResponse = recommendation.GetOptionSettingValue<BeanstalkApplicationTypeHintResponse>(setting);

                    var userInputConfiguration = new UserInputConfiguration<ApplicationDescription>
                    {
                        DisplaySelector = app => app.ApplicationName,
                        DefaultSelector = app => app.ApplicationName.Equals(currentTypeHintResponse?.ApplicationName),
                        AskNewName = true,
                        DefaultNewName = currentTypeHintResponse.ApplicationName
                    };

                    var userResponse = _consoleUtilities.AskUserToChooseOrCreateNew(applications, "Select Beanstalk application to deploy to:", userInputConfiguration);

                    settingValue = new BeanstalkApplicationTypeHintResponse
                    {
                        CreateNew = userResponse.CreateNew,
                        ApplicationName = userResponse.SelectedOption?.ApplicationName ?? userResponse.NewName
                    };
                }
                else if (setting.TypeHint == OptionSettingTypeHint.Vpc)
                {
                    _toolInteractiveService.WriteLine(setting.Description);

                    var currentVpcTypeHintResponse = setting.GetTypeHintData<VpcTypeHintResponse>();

                    var vpcs = await _awsResourceQueryer.GetListOfVpcs(_session);

                    var userInputConfig = new UserInputConfiguration<Vpc>
                    {
                        DisplaySelector = vpc =>
                        {
                            var name = vpc.Tags?.FirstOrDefault(x => x.Key == "Name")?.Value ?? string.Empty;
                            var namePart =
                                string.IsNullOrEmpty(name)
                                    ? ""
                                    : $" ({name}) ";

                            var isDefaultPart =
                                vpc.IsDefault
                                    ? Constants.DEFAULT_LABEL
                                    : "";

                            return $"{vpc.VpcId}{namePart}{isDefaultPart}";
                        },
                        DefaultSelector = vpc =>
                            !string.IsNullOrEmpty(currentVpcTypeHintResponse?.VpcId)
                                ? vpc.VpcId == currentVpcTypeHintResponse.VpcId
                                : vpc.IsDefault
                    };

                    var userResponse = _consoleUtilities.AskUserToChooseOrCreateNew(
                        vpcs,
                        "Select a VPC",
                        userInputConfig);

                    settingValue = new VpcTypeHintResponse
                    {
                        IsDefault = userResponse.SelectedOption?.IsDefault == true,
                        CreateNew = userResponse.CreateNew,
                        VpcId = userResponse.SelectedOption?.VpcId ?? ""
                    };
                }
                else
                {
                    foreach (var childSetting in setting.ChildOptionSettings)
                    {
                        await ConfigureDeployment(recommendation, childSetting);
                    }
                }
            }
            else
            {
                settingValue = _consoleUtilities.AskUserForValue(setting.Description, currentValue?.ToString(), allowEmpty: true);
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
        private void DisplayValue(Recommendation recommendation, OptionSettingItem optionSetting, int optionSettingNumber, Type typeHintResponseType)
        {
            var methodInfo = typeof(Recommendation)
                .GetMethod(nameof(Recommendation.GetOptionSettingValue), 1, new[] {typeof(OptionSettingItem), typeof(bool)});
            var genericMethodInfo = methodInfo?.MakeGenericMethod(typeHintResponseType);
            var response = genericMethodInfo?.Invoke(recommendation, new object[] {optionSetting, false});
            _toolInteractiveService.WriteLine($"{optionSettingNumber}. {optionSetting.Name}: {((IDisplayable)response)?.ToDisplayString()}");
        }

        private void DisplayValues(Dictionary<string, object> objectValues, string indent)
        {
            foreach (var (key, value) in objectValues)
            {
                if (value is Dictionary<string, object> childObjectValue)
                {
                    _toolInteractiveService.WriteLine($"{indent}{key}");
                    DisplayValues(childObjectValue, $"{indent}\t");
                }
                else if (value is string stringValue)
                {
                    if (!string.IsNullOrEmpty(stringValue))
                    {
                        _toolInteractiveService.WriteLine($"{indent}{key}: {stringValue}");
                    }
                }
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
