// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.Common;
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
                cloudApplicationName = _consoleUtilities.AskUserForValue("Enter name for Cloud Application", GetDefaultApplicationName(new ProjectDefinition(_session.ProjectPath).ProjectPath));
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

            // If there was a previous deployment be sure to make that recipe be the top recommendation.
            if (previousDeployment != null)
            {
                var lastRecommendation = recommendations.FirstOrDefault(x => string.Equals(x.Recipe.Id, previousDeployment.RecipeId, StringComparison.InvariantCultureIgnoreCase));
                if (lastRecommendation != null)
                {
                    recommendations.Remove(lastRecommendation);
                    recommendations.Insert(0, lastRecommendation);
                }
            }

            var selectedRecommendation = _consoleUtilities.AskUserToChoose(recommendations, "Available options to deploy project", recommendations[0]);
            selectedRecommendation.ApplyPreviousSettings(previousDeployment?.RecipeOverrideSettings);

            _toolInteractiveService.WriteDebugLine("Confirming System Capabilities");
            await _session.SystemCapabilities;
            _toolInteractiveService.WriteDebugLine("Confirmed System Capabilities");

            if (selectedRecommendation.Recipe.DeploymentType == RecipeDefinition.DeploymentTypes.CdkProject &&
                !(await _session.SystemCapabilities).NodeJsMinVersionInstalled)
            {
                _toolInteractiveService.WriteErrorLine("The selected Recipe requires NodeJS 10.3 or later.  Please install NodeJS https://nodejs.org/en/download/");
                throw new MissingNodeJsException();
            }

            if (selectedRecommendation.Recipe.DeploymentBundle == RecipeDefinition.DeploymentBundleTypes.Container &&
                !(await _session.SystemCapabilities).DockerInstalled)
            {
                _toolInteractiveService.WriteErrorLine("The selected Recipe requires docker but docker was not detected as running.  Please install and start docker: https://docs.docker.com/engine/install/");
                throw new MissingDockerException();
            }

            DisplaySettings(selectedRecommendation, false);

            var configureSettings = _consoleUtilities.AskYesNoQuestion("Do you wish to change the default options before deploying?", ConsoleUtilities.YesNo.No);

            while (configureSettings == ConsoleUtilities.YesNo.Yes)
            {
                await ConfigureDeployment(selectedRecommendation);

                _toolInteractiveService.WriteLine("Configuration complete:");
                DisplaySettings(selectedRecommendation, true);

                _toolInteractiveService.WriteLine(string.Empty);
                configureSettings = _consoleUtilities.AskYesNoQuestion("Do you wish to change any of these settings?", ConsoleUtilities.YesNo.No);
            }

            await orchestrator.DeployRecommendation(cloudApplicationName, selectedRecommendation);
        }

        private async Task ConfigureDeployment(Recommendation recommendation)
        {
            Console.WriteLine(string.Empty);

            foreach (var setting in recommendation.Recipe.OptionSettings)
            {
                var isDisplayed = true;
                foreach(var dependency in setting.DependsOn)
                {
                    var dependsOnValue = recommendation.GetOptionSettingValue(dependency.Id);
                    if (!dependsOnValue.Equals(dependency.Value))
                    {
                        isDisplayed = false;
                        break;
                    }
                }
                if (!isDisplayed)
                {
                    recommendation.SetOverrideOptionSettingValue(setting.Id, setting.DefaultValue);
                    continue;
                }

                _toolInteractiveService.WriteLine($"{setting.Name}:");

                var currentValue = recommendation.GetOptionSettingValue(setting.Id);
                object settingValue = null;
                if (setting.AllowedValues?.Count > 0)
                {
                    _toolInteractiveService.WriteLine(setting.Description);
                    settingValue = _consoleUtilities.AskUserToChoose(setting.AllowedValues, null, currentValue?.ToString());
                    if (setting.ValueMapping.ContainsKey(settingValue.ToString()))
                        settingValue = setting.ValueMapping[settingValue.ToString()];

                    // If they didn't change the value then don't store so we can rely on using the default in the recipe.
                    if (Equals(settingValue, currentValue))
                        continue;
                }
                else if (setting.TypeHint == RecipeDefinition.OptionSettingTypeHint.BeanstalkApplication)
                {
                    _toolInteractiveService.WriteLine(setting.Description);

                    var applications = await _awsResourceQueryer.GetListOfElasticBeanstalkApplications(_session);

                    settingValue = _consoleUtilities.AskUserToChooseOrCreateNew(applications,
                        "Select Beanstalk application to deploy to:",
                        currentValue?.ToString());
                }
                else if (setting.TypeHint == RecipeDefinition.OptionSettingTypeHint.BeanstalkEnvironment)
                {
                    _toolInteractiveService.WriteLine(setting.Description);

                    var applicationName = recommendation.GetOptionSettingValue(setting.ParentSettingId) as string;
                    var environments = await _awsResourceQueryer.GetListOfElasticBeanstalkEnvironments(_session, applicationName);
                    
                    settingValue = _consoleUtilities.AskUserToChooseOrCreateNew(environments,
                        "Select Beanstalk environment to deploy to:",
                        currentValue?.ToString());
                }
                else if (setting.TypeHint == RecipeDefinition.OptionSettingTypeHint.ECSCluster)
                {
                    _toolInteractiveService.WriteLine(setting.Description);

                    var clusters = await _awsResourceQueryer.GetListOfECSClusters(_session);

                    settingValue = _consoleUtilities.AskUserToChoose(clusters,
                        "Select ECS Cluster to deploy to:",
                        currentValue?.ToString());
                }
                else if (setting.TypeHint == RecipeDefinition.OptionSettingTypeHint.Vpc)
                {
                    _toolInteractiveService.WriteLine(setting.Description);

                    var vpcs = await _awsResourceQueryer.GetListOfVpcEndpoints(_session);

                    settingValue = _consoleUtilities.AskUserToChoose(vpcs,
                        "Select Vpc:",
                        currentValue?.ToString());
                }
                else if (setting.Type == RecipeDefinition.OptionSettingValueType.Bool)
                {
                    var answer = _consoleUtilities.AskYesNoQuestion(setting.Description, recommendation.GetOptionSettingValue(setting.Id).ToString());
                    settingValue = answer == ConsoleUtilities.YesNo.Yes ? "true" : "false";
                }
                else
                {
                    _toolInteractiveService.WriteLine(setting.Description);
                    _toolInteractiveService.WriteLine($"(default: {recommendation.GetOptionSettingValue(setting.Id)}):");
                    settingValue = _toolInteractiveService.ReadLine();
                }

                if (settingValue == null || (settingValue as string) == string.Empty)
                {
                    continue;
                }

                recommendation.SetOverrideOptionSettingValue(setting.Id, settingValue);
                _toolInteractiveService.WriteLine(string.Empty);
            }
        }

        private void DisplaySettings(Recommendation recommendation, bool showAdvancedSettings)
        {
            _toolInteractiveService.WriteLine($"The project will be deploy to {recommendation.Recipe.TargetService} using the following settings:");
            foreach (var option in recommendation.Recipe.OptionSettings)
            {
                if (option.AdvancedSetting && !showAdvancedSettings)
                    continue;

                _toolInteractiveService.WriteLine($"{option.Name}: {recommendation.GetOptionSettingValue(option.Id)}");
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
