using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.ElasticBeanstalk;
using AWS.DefaultDotNETRecipes;
using AWS.DeploymentCommon;
using AWS.DeploymentOrchestrator;

namespace AWS.DeploymentNETCoreToolApp.Commands
{
    public class DeployCommand
    {
        private readonly IAWSClientFactory _awsClientFactory;
        private readonly IToolInteractiveService _interactiveService;
        private readonly ConsoleUtilities _consoleUtilities;

        private readonly OrchestratorSession _session;

        public DeployCommand(IAWSClientFactory awsClientFactory, IToolInteractiveService interactiveService, OrchestratorSession session)
        {
            _awsClientFactory = awsClientFactory;
            _interactiveService = interactiveService;
            _consoleUtilities = new ConsoleUtilities(interactiveService);

            _session = session;
        }

        public async Task ExecuteAsync(bool saveCdkProject)
        {
            var orchestrator = new Orchestrator(_session, new ConsoleOrchestratorLogger(_interactiveService), RecipeLocator.FindRecipeDefinitionsPath());

            var previousSettings = orchestrator.GetPreviousDeploymentSettings();
            var previousDeploymentNames = previousSettings.GetDeploymentNames();

            _interactiveService.WriteLine(string.Empty);

            string cloudApplicationName;
            if(previousSettings.Deployments.Count == 0)
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
                _interactiveService.WriteErrorLine($"Unable to determine a method for deploying application: {_session.ProjectPath}");
                return;
            }

            // If there was a previous deployment be sure to make that recipe be the top recommendation.
            if(previousDeployment != null)
            {
                var lastRecommendation = recommendations.FirstOrDefault(x => string.Equals(x.Recipe.Id, previousDeployment.RecipeId, StringComparison.InvariantCultureIgnoreCase));
                if(lastRecommendation != null)
                {
                    recommendations.Remove(lastRecommendation);
                    recommendations.Insert(0, lastRecommendation);
                }
            }

            var selectedRecommendation = _consoleUtilities.AskUserToChoose(recommendations, "Available options to deploy project", recommendations[0]);
            selectedRecommendation.ApplyPreviousSettings(previousDeployment?.RecipeOverrideSettings);

            DisplaySettings(selectedRecommendation, false);

            ConsoleUtilities.YesNo configureSettings = _consoleUtilities.AskYesNoQuestion("Do you wish to change the default options before deploying?", ConsoleUtilities.YesNo.No);

            while (configureSettings == ConsoleUtilities.YesNo.Yes)
            {
                await ConfigureDeployment(selectedRecommendation);

                _interactiveService.WriteLine("Configuration complete:");
                DisplaySettings(selectedRecommendation, true);

                _interactiveService.WriteLine(string.Empty);
                configureSettings = _consoleUtilities.AskYesNoQuestion("Do you wish to change any of these settings?", ConsoleUtilities.YesNo.No);
            }

            orchestrator.DeployRecommendation(cloudApplicationName, selectedRecommendation);
        }

        private async Task ConfigureDeployment(Recommendation recommendation)
        {
            Console.WriteLine(string.Empty);
            
            var awsUtilities = new AWSUtilities(_interactiveService);
            foreach(var setting in recommendation.Recipe.OptionSettings)
            {
                _interactiveService.WriteLine($"{setting.Name}:");
                _interactiveService.WriteLine(setting.Description);

                var currentValue = recommendation.GetOptionSettingValue(setting.Id);
                object settingValue = null;
                if(setting.AllowedValues?.Count > 0)
                {
                    settingValue = _consoleUtilities.AskUserToChoose(setting.AllowedValues, null, currentValue?.ToString());

                    // If they didn't change the value then don't store so we can rely on using the default in the recipe.
                    if (Equals(settingValue, currentValue))
                        continue;
                }
                else if (setting.TypeHint == RecipeDefinition.OptionSettingTypeHint.BeanstalkApplication)
                {
                    var applications = await awsUtilities.GetListOfElasticBeanstalkApplications(
                        _awsClientFactory.GetAWSClient<IAmazonElasticBeanstalk>(_session.AWSCredentials, _session.AWSRegion));

                    settingValue = _consoleUtilities.AskUserToChooseOrCreateNew(applications,
                        "Select Beanstalk application to deploy to:", currentValue?.ToString());
                }
                else if (setting.TypeHint == RecipeDefinition.OptionSettingTypeHint.BeanstalkEnvironment)
                {
                    var applicationName = recommendation.GetOptionSettingValue(setting.ParentSettingId) as string;
                    var environments = await awsUtilities.GetListOfElasticBeanstalkEnvironments(
                        _awsClientFactory.GetAWSClient<IAmazonElasticBeanstalk>(_session.AWSCredentials, _session.AWSRegion), applicationName);
                    settingValue = _consoleUtilities.AskUserToChooseOrCreateNew(environments,
                        "Select Beanstalk environment to deploy to:", currentValue?.ToString());
                }
                else
                {
                    _interactiveService.WriteLine($"(default: {recommendation.GetOptionSettingValue(setting.Id)}):");
                    settingValue = _interactiveService.ReadLine();
                }

                if (settingValue == null || (settingValue as string) == string.Empty)
                {
                    continue;
                }

                recommendation.SetOverrideOptionSettingValue(setting.Id, settingValue);
                _interactiveService.WriteLine(string.Empty);
            }
        }

        private void DisplaySettings(Recommendation recommendation, bool showAdvancedSettings)
        {
            _interactiveService.WriteLine($"The project will be deploy to {recommendation.Recipe.TargetService} using the following settings:");
            foreach (var option in recommendation.Recipe.OptionSettings)
            {
                if (option.AdvancedSetting && !showAdvancedSettings)
                    continue;

                _interactiveService.WriteLine($"{option.Name}: {recommendation.GetOptionSettingValue(option.Id)}");
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
