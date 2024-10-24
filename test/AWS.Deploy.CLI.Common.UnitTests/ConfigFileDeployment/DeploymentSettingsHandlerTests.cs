using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private readonly string _projectPath;
        private readonly IOptionSettingHandler _optionSettingHandler;
        private readonly IDeploymentManifestEngine _deploymentManifestEngine;
        private readonly IOrchestratorInteractiveService _orchestratorInteractiveService;
        private readonly IDirectoryManager _directoryManager;
        private readonly IFileManager _fileManager;
        private readonly IRecipeHandler _recipeHandler;
        private readonly IDeploymentSettingsHandler _deploymentSettingsHandler;
        private readonly RecommendationEngine _recommendationEngine;
        private readonly OrchestratorSession _orchestratorSession;

        private const string BEANSTALK_PLATFORM_ARN_TOKEN = "{LatestDotnetBeanstalkPlatformArn}";
        private const string STACK_NAME_TOKEN = "{StackName}";
        private const string DEFAULT_VPC_ID_TOKEN = "{DefaultVpcId}";
        private const string DEFAULT_CONTAINER_PORT_TOKEN = "{DefaultContainerPort}";

        public DeploymentSettingsHandlerTests()
        {
            _projectPath = SystemIOUtilities.ResolvePath("WebAppWithDockerFile");
            _directoryManager = new DirectoryManager();
            _fileManager = new FileManager();
            _deploymentManifestEngine = new DeploymentManifestEngine(_directoryManager, _fileManager);
            _orchestratorInteractiveService = new Mock<IOrchestratorInteractiveService>().Object;

            var parser = new ProjectDefinitionParser(_fileManager, _directoryManager);
            var awsCredentials = new Mock<AWSCredentials>();
            _orchestratorSession = new OrchestratorSession(
                parser.Parse(_projectPath).Result,
                awsCredentials.Object,
                "us-west-2",
                "123456789012")
            {
                AWSProfileName = "default"
            };

            var validatorFactory = new TestValidatorFactory();
            _optionSettingHandler = new OptionSettingHandler(validatorFactory);
            _recipeHandler = new RecipeHandler(_deploymentManifestEngine, _orchestratorInteractiveService, _directoryManager, _fileManager, _optionSettingHandler, validatorFactory);
            _deploymentSettingsHandler = new DeploymentSettingsHandler(_fileManager, _directoryManager, _optionSettingHandler, _recipeHandler);
            _recommendationEngine = new RecommendationEngine(_orchestratorSession, _recipeHandler);
        }

        [Fact]
        public async Task ApplySettings_AppRunner()
        {
            // ARRANGE
            var recommendations = await _recommendationEngine.ComputeRecommendations();
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
        public async Task ApplySettings_ECSFargate()
        {
            // ARRANGE
            var recommendations = await _recommendationEngine.ComputeRecommendations();
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
        public async Task ApplySettings_ElasticBeanStalk()
        {
            // ARRANGE
            var recommendations = await _recommendationEngine.ComputeRecommendations();
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

        [Theory]
        [InlineData(SaveSettingsType.All, "ConfigFileDeployment", "TestFiles", "SettingsSnapshot_NonContainer.json")]
        [InlineData(SaveSettingsType.Modified, "ConfigFileDeployment", "TestFiles", "SettingsSnapshot_NonContainer_ModifiedOnly.json")]
        public async Task SaveSettings_NonContainerBased(SaveSettingsType saveSettingsType, string path1, string path2, string path3)
        {
            // ARRANGE
            var recommendations = await _recommendationEngine.ComputeRecommendations();
            var selectedRecommendation = recommendations.FirstOrDefault(x => string.Equals(x.Recipe.Id, "AspNetAppElasticBeanstalkLinux"));
            var expectedSnapshotfilePath = Path.Combine(path1, path2, path3);
            var actualSnapshotFilePath = Path.Combine(Path.GetTempPath(), $"DeploymentSettings-{Guid.NewGuid().ToString().Split('-').Last()}.json");
            var cloudApplication = new CloudApplication("MyAppStack", "", CloudApplicationResourceType.CloudFormationStack, "AspNetAppElasticBeanstalkLinux");

            // ARRANGE - add replacement tokens
            selectedRecommendation.AddReplacementToken(BEANSTALK_PLATFORM_ARN_TOKEN, "Latest-ARN");
            selectedRecommendation.AddReplacementToken(STACK_NAME_TOKEN, "MyAppStack");
            selectedRecommendation.AddReplacementToken(DEFAULT_VPC_ID_TOKEN, "vpc-12345678");

            // ARRANGE - Modify option setting items
            await _optionSettingHandler.SetOptionSettingValue(selectedRecommendation, "BeanstalkApplication", "MyBeanstalkApplication");
            await _optionSettingHandler.SetOptionSettingValue(selectedRecommendation, "BeanstalkEnvironment.EnvironmentName", "MyBeanstalkEnvironment");
            await _optionSettingHandler.SetOptionSettingValue(selectedRecommendation, "EnvironmentType", "LoadBalanced");
            await _optionSettingHandler.SetOptionSettingValue(selectedRecommendation, "XRayTracingSupportEnabled", true);
            await _optionSettingHandler.SetOptionSettingValue(selectedRecommendation, "ElasticBeanstalkEnvironmentVariables", new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            });
            await _optionSettingHandler.SetOptionSettingValue(selectedRecommendation, "IMDSv1Access", "Disabled");

            // ACT
            await _deploymentSettingsHandler.SaveSettings(new SaveSettingsConfiguration(saveSettingsType, actualSnapshotFilePath), selectedRecommendation, cloudApplication, _orchestratorSession);

            // ASSERT
            var actualSnapshot = await _fileManager.ReadAllTextAsync(actualSnapshotFilePath);
            var expectedSnapshot = await _fileManager.ReadAllTextAsync(expectedSnapshotfilePath);
            actualSnapshot = SanitizeFileContents(actualSnapshot);
            expectedSnapshot = SanitizeFileContents(expectedSnapshot);
            Assert.Equal(expectedSnapshot, actualSnapshot);
        }

        [Theory]
        [InlineData(SaveSettingsType.All, "ConfigFileDeployment", "TestFiles", "SettingsSnapshot_Container.json")]
        [InlineData(SaveSettingsType.Modified, "ConfigFileDeployment", "TestFiles", "SettingsSnapshot_Container_ModifiedOnly.json")]
        public async Task SaveSettings_ContainerBased(SaveSettingsType saveSettingsType, string path1, string path2, string path3)
        {
            // ARRANGE
            var recommendations = await _recommendationEngine.ComputeRecommendations();
            var selectedRecommendation = recommendations.FirstOrDefault(x => string.Equals(x.Recipe.Id, "AspNetAppAppRunner"));
            var expectedSnapshotfilePath = Path.Combine(path1, path2, path3);
            var actualSnapshotFilePath = Path.Combine(Path.GetTempPath(), $"DeploymentSettings-{Guid.NewGuid().ToString().Split('-').Last()}.json");
            var cloudApplication = new CloudApplication("MyAppStack", "", CloudApplicationResourceType.CloudFormationStack, "AspNetAppAppRunner");

            // ARRANGE - add replacement tokens
            selectedRecommendation.AddReplacementToken(STACK_NAME_TOKEN, "MyAppStack");
            selectedRecommendation.AddReplacementToken(DEFAULT_VPC_ID_TOKEN, "vpc-12345678");
            selectedRecommendation.AddReplacementToken(DEFAULT_CONTAINER_PORT_TOKEN, 80);

            // ARRANGE - Modify option setting items
            await _optionSettingHandler.SetOptionSettingValue(selectedRecommendation, "ServiceName", "MyAppRunnerService");
            await _optionSettingHandler.SetOptionSettingValue(selectedRecommendation, "Port", "100");
            await _optionSettingHandler.SetOptionSettingValue(selectedRecommendation, "ECRRepositoryName", "my-ecr-repository");
            await _optionSettingHandler.SetOptionSettingValue(selectedRecommendation, "DockerfilePath", Path.Combine("DockerAssets", "Dockerfile")); // relative path
            await _optionSettingHandler.SetOptionSettingValue(selectedRecommendation, "DockerExecutionDirectory", Path.Combine(_projectPath, "DockerAssets")); // absolute path

            // ACT
            await _deploymentSettingsHandler.SaveSettings(new SaveSettingsConfiguration(saveSettingsType, actualSnapshotFilePath), selectedRecommendation, cloudApplication, _orchestratorSession);

            // ASSERT
            var actualSnapshot = await _fileManager.ReadAllTextAsync(actualSnapshotFilePath);
            var expectedSnapshot = await _fileManager.ReadAllTextAsync(expectedSnapshotfilePath);
            actualSnapshot = SanitizeFileContents(actualSnapshot);
            expectedSnapshot = SanitizeFileContents(expectedSnapshot);
            Assert.Equal(expectedSnapshot, actualSnapshot);
        }

        [Fact]
        public async Task SaveSettings_PushImageToECR()
        {
            // ARRANGE
            var recommendations = await _recommendationEngine.ComputeRecommendations();
            var selectedRecommendation = recommendations.FirstOrDefault(x => string.Equals(x.Recipe.Id, "PushContainerImageEcr"));
            var expectedSnapshotfilePath = Path.Combine("ConfigFileDeployment", "TestFiles", "SettingsSnapshot_PushImageECR.json");
            var actualSnapshotFilePath = Path.Combine(Path.GetTempPath(), $"DeploymentSettings-{Guid.NewGuid().ToString().Split('-').Last()}.json");
            var cloudApplication = new CloudApplication("my-ecr-repository", "", CloudApplicationResourceType.ElasticContainerRegistryImage, "PushContainerImageEcr");

            // ARRANGE - Modify option setting items
            await _optionSettingHandler.SetOptionSettingValue(selectedRecommendation, "ImageTag", "123456789");
            await _optionSettingHandler.SetOptionSettingValue(selectedRecommendation, "ECRRepositoryName", "my-ecr-repository");
            await _optionSettingHandler.SetOptionSettingValue(selectedRecommendation, "DockerfilePath", Path.Combine("DockerAssets", "Dockerfile")); // relative path
            await _optionSettingHandler.SetOptionSettingValue(selectedRecommendation, "DockerExecutionDirectory", Path.Combine(_projectPath, "DockerAssets")); // absolute path

            // ACT
            await _deploymentSettingsHandler.SaveSettings(new SaveSettingsConfiguration(SaveSettingsType.All, actualSnapshotFilePath), selectedRecommendation, cloudApplication, _orchestratorSession);

            // ASSERT
            var actualSnapshot = await _fileManager.ReadAllTextAsync(actualSnapshotFilePath);
            var expectedSnapshot = await _fileManager.ReadAllTextAsync(expectedSnapshotfilePath);
            actualSnapshot = SanitizeFileContents(actualSnapshot);
            expectedSnapshot = SanitizeFileContents(expectedSnapshot);
            Assert.Equal(expectedSnapshot, actualSnapshot);
        }

        [Fact]
        public async Task ReadSettings_InvalidJson()
        {
            // ARRANGE
            var recommendations = await _recommendationEngine.ComputeRecommendations();
            var selectedRecommendation = recommendations.FirstOrDefault(x => string.Equals(x.Recipe.Id, "AspNetAppAppRunner"));
            var filePath = Path.Combine("ConfigFileDeployment", "TestFiles", "InvalidConfigFile.json");

            // ACT
            var readAction = async () => await _deploymentSettingsHandler.ReadSettings(filePath);

            // ASSERT
            var ex = await Assert.ThrowsAsync<InvalidDeploymentSettingsException>(readAction);
            Assert.Equal(DeployToolErrorCode.FailedToDeserializeUserDeploymentFile, ex.ErrorCode);
        }

        [Fact]
        public async Task ReadSettings_FileNotFound()
        {
            // ARRANGE
            var recommendations = await _recommendationEngine.ComputeRecommendations();
            var selectedRecommendation = recommendations.FirstOrDefault(x => string.Equals(x.Recipe.Id, "AspNetAppAppRunner"));
            var filePath = Path.Combine("ConfigFileDeployment", "TestFiles", "AppRunnerConfigFile");

            // ACT
            var readAction = async () => await _deploymentSettingsHandler.ReadSettings(filePath);

            // ASSERT
            var ex = await Assert.ThrowsAsync<InvalidDeploymentSettingsException>(readAction);
            Assert.Equal(DeployToolErrorCode.UserDeploymentFileNotFound, ex.ErrorCode);
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

        private string SanitizeFileContents(string content)
        {
            return content.Replace("\r\n", Environment.NewLine)
                .Replace("\n", Environment.NewLine)
                .Replace("\r\r\n", Environment.NewLine)
                .Trim();
        }
    }

    public class TestValidatorFactory : IValidatorFactory
    {
        public IOptionSettingItemValidator[] BuildValidators(OptionSettingItem optionSettingItem, Func<OptionSettingItemValidatorConfig, bool> filter = null) => new IOptionSettingItemValidator[0];
        public IRecipeValidator[] BuildValidators(RecipeDefinition recipeDefinition) => new IRecipeValidator[0];
    }
}
