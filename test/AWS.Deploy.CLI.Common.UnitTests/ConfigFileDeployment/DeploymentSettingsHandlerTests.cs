using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Amazon.Runtime;
using AWS.Deploy.CLI.Common.UnitTests.Utilities;
using AWS.Deploy.Common;
using AWS.Deploy.Common.DeploymentManifest;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Recipes.Validation;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.RecommendationEngine;
using Moq;
using Xunit;

namespace AWS.Deploy.CLI.Common.UnitTests.ConfigFileDeployment
{
    public class DeploymentSettingsHandlerTests
    {
        private readonly IOptionSettingHandler _optionSettingHandler;
        private readonly IDeploymentManifestEngine _deploymentManifestEngine;
        private readonly IOrchestratorInteractiveService _orchestratorInteractiveService;
        private readonly IDirectoryManager _directoryManager;
        private readonly IFileManager _fileManager;
        private readonly IRecipeHandler _recipeHandler;
        private readonly IDeploymentSettingsHandler _deploymentSettingsHandler;
        private readonly RecommendationEngine _recommendationEngine;

        public DeploymentSettingsHandlerTests()
        {
            var projectPath = SystemIOUtilities.ResolvePath("WebAppNoDockerFile");
            _directoryManager = new DirectoryManager();
            _fileManager = new FileManager();
            _deploymentManifestEngine = new DeploymentManifestEngine(_directoryManager, _fileManager);
            _orchestratorInteractiveService = new Mock<IOrchestratorInteractiveService>().Object;

            var parser = new ProjectDefinitionParser(_fileManager, _directoryManager);
            var awsCredentials = new Mock<AWSCredentials>();
            var session = new OrchestratorSession(
                parser.Parse(projectPath).Result,
                awsCredentials.Object,
                "us-west-2",
                "123456789012")
            {
                AWSProfileName = "default"
            };

            var validatorFactory = new TestValidatorFactory();
            _optionSettingHandler = new OptionSettingHandler(validatorFactory);
            _recipeHandler = new RecipeHandler(_deploymentManifestEngine, _orchestratorInteractiveService, _directoryManager, _fileManager, _optionSettingHandler, validatorFactory);
            _deploymentSettingsHandler = new DeploymentSettingsHandler(_fileManager, _optionSettingHandler, _recipeHandler);
            _recommendationEngine = new RecommendationEngine(session, _recipeHandler);
        }

        [Fact]
        public async Task AppRunnerTests()
        {
            // ARRANGE
            var recommendations = _recommendationEngine.ComputeRecommendations().GetAwaiter().GetResult();
            var selectedRecommendation = recommendations.FirstOrDefault(x => string.Equals(x.Recipe.Id, "AspNetAppAppRunner"));
            var filePath = Path.Combine("ConfigFileDeployment", "TestFiles", "AppRunnerConfigFile.json");

            // ACT
            var deploymentSettings = await _deploymentSettingsHandler.ReadSettings(filePath);
            await _deploymentSettingsHandler.ApplySettings(deploymentSettings, selectedRecommendation, new Mock<IDeployToolValidationContext>().Object);

            // ASSERT
            Assert.Equal("default", deploymentSettings.AWSProfile);
            Assert.Equal("us-west-2", deploymentSettings.AWSRegion);
            Assert.Equal("MyAppStack", deploymentSettings.ApplicationName);
            Assert.Equal("AspNetAppAppRunner", deploymentSettings.RecipeId);

            Assert.Equal(true, GetOptionSettingValue(selectedRecommendation, "VPCConnector.CreateNew"));
            Assert.Contains("subnet-1234abcd", GetOptionSettingValue<SortedSet<string>>(selectedRecommendation, "VPCConnector.Subnets"));
            Assert.Contains("sg-1234abcd", GetOptionSettingValue<SortedSet<string>>(selectedRecommendation, "VPCConnector.SecurityGroups"));
        }

        [Fact]
        public async Task ECSFargateTests()
        {
            // ARRANGE
            var recommendations = _recommendationEngine.ComputeRecommendations().GetAwaiter().GetResult();
            var selectedRecommendation = recommendations.FirstOrDefault(x => string.Equals(x.Recipe.Id, "AspNetAppEcsFargate"));
            var filePath = Path.Combine("ConfigFileDeployment", "TestFiles", "ECSFargateConfigFile.json");

            // ACT
            var deploymentSettings = await _deploymentSettingsHandler.ReadSettings(filePath);
            await _deploymentSettingsHandler.ApplySettings(deploymentSettings, selectedRecommendation, new Mock<IDeployToolValidationContext>().Object);

            // ASSERT
            Assert.Equal("default", deploymentSettings.AWSProfile);
            Assert.Equal("us-west-2", deploymentSettings.AWSRegion);
            Assert.Equal("MyAppStack", deploymentSettings.ApplicationName);
            Assert.Equal("AspNetAppEcsFargate", deploymentSettings.RecipeId);

            Assert.Equal(true, GetOptionSettingValue(selectedRecommendation, "ECSCluster.CreateNew"));
            Assert.Equal("MyNewCluster", GetOptionSettingValue(selectedRecommendation, "ECSCluster.NewClusterName"));
            Assert.Equal("MyNewService", GetOptionSettingValue(selectedRecommendation, "ECSServiceName"));
            Assert.Equal(3, GetOptionSettingValue<int>(selectedRecommendation, "DesiredCount"));
            Assert.Equal(true, GetOptionSettingValue(selectedRecommendation, "ApplicationIAMRole.CreateNew"));
            Assert.Equal(true, GetOptionSettingValue(selectedRecommendation, "Vpc.IsDefault"));
            Assert.Equal(256, GetOptionSettingValue<int>(selectedRecommendation, "TaskCpu"));
            Assert.Equal(512, GetOptionSettingValue<int>(selectedRecommendation, "TaskMemory"));
            Assert.Equal("C:\\codebase", GetOptionSettingValue(selectedRecommendation, "DockerExecutionDirectory"));
        }

        [Fact]
        public async Task ElasticBeanStalkTests()
        {
            // ARRANGE
            var recommendations = _recommendationEngine.ComputeRecommendations().GetAwaiter().GetResult();
            var selectedRecommendation = recommendations.FirstOrDefault(x => string.Equals(x.Recipe.Id, "AspNetAppElasticBeanstalkLinux"));
            var filePath = Path.Combine("ConfigFileDeployment", "TestFiles", "ElasticBeanStalkConfigFile.json");

            // ACT
            var deploymentSettings = await _deploymentSettingsHandler.ReadSettings(filePath);
            await _deploymentSettingsHandler.ApplySettings(deploymentSettings, selectedRecommendation, new Mock<IDeployToolValidationContext>().Object);

            // ASSERT
            Assert.Equal("default", deploymentSettings.AWSProfile);
            Assert.Equal("us-west-2", deploymentSettings.AWSRegion);
            Assert.Equal("MyAppStack", deploymentSettings.ApplicationName);
            Assert.Equal("AspNetAppElasticBeanstalkLinux", deploymentSettings.RecipeId);

            Assert.Equal(true, GetOptionSettingValue(selectedRecommendation, "BeanstalkApplication.CreateNew"));
            Assert.Equal("MyApplication", GetOptionSettingValue(selectedRecommendation, "BeanstalkApplication.ApplicationName"));
            Assert.Equal("MyEnvironment", GetOptionSettingValue(selectedRecommendation, "BeanstalkEnvironment.EnvironmentName"));
            Assert.Equal("MyInstance", GetOptionSettingValue(selectedRecommendation, "InstanceType"));
            Assert.Equal("SingleInstance", GetOptionSettingValue(selectedRecommendation, "EnvironmentType"));
            Assert.Equal("application", GetOptionSettingValue(selectedRecommendation, "LoadBalancerType"));
            Assert.Equal(true, GetOptionSettingValue(selectedRecommendation, "ApplicationIAMRole.CreateNew"));
            Assert.Equal("MyPlatformArn", GetOptionSettingValue(selectedRecommendation, "ElasticBeanstalkPlatformArn"));
            Assert.Equal(true, GetOptionSettingValue(selectedRecommendation, "ElasticBeanstalkManagedPlatformUpdates.ManagedActionsEnabled"));
            Assert.Equal("Mon:12:00", GetOptionSettingValue(selectedRecommendation, "ElasticBeanstalkManagedPlatformUpdates.PreferredStartTime"));
            Assert.Equal("minor", GetOptionSettingValue(selectedRecommendation, "ElasticBeanstalkManagedPlatformUpdates.UpdateLevel"));

            var envVars = GetOptionSettingValue<Dictionary<string, string>>(selectedRecommendation, "ElasticBeanstalkEnvironmentVariables");
            Assert.Equal("VarValue", envVars["VarName"]);
        }

        private object GetOptionSettingValue(Recommendation recommendation, string fullyQualifiedId)
        {
            var optionSetting = _optionSettingHandler.GetOptionSetting(recommendation, fullyQualifiedId);
            return _optionSettingHandler.GetOptionSettingValue(recommendation, optionSetting);
        }

        private T GetOptionSettingValue<T>(Recommendation recommendation, string fullyQualifiedId)
        {
            var optionSetting = _optionSettingHandler.GetOptionSetting(recommendation, fullyQualifiedId);
            return _optionSettingHandler.GetOptionSettingValue<T>(recommendation, optionSetting);
        }
    }

    public class TestValidatorFactory : IValidatorFactory
    {
        public IOptionSettingItemValidator[] BuildValidators(OptionSettingItem optionSettingItem, Func<OptionSettingItemValidatorConfig, bool> filter = null) => new IOptionSettingItemValidator[0];
        public IRecipeValidator[] BuildValidators(RecipeDefinition recipeDefinition) => new IRecipeValidator[0];
    }
}
