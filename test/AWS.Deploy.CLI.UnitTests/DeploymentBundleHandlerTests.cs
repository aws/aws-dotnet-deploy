// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Amazon.Runtime;
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.CLI.Common.UnitTests.Utilities;
using AWS.Deploy.CLI.UnitTests.Utilities;
using AWS.Deploy.Common;
using AWS.Deploy.Common.DeploymentManifest;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Recipes.Validation;
using AWS.Deploy.Constants;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.RecommendationEngine;
using AWS.Deploy.Recipes;
using Moq;
using Xunit;

namespace AWS.Deploy.CLI.UnitTests
{
    public class DeploymentBundleHandlerTests
    {
        private readonly DeploymentBundleHandler _deploymentBundleHandler;
        private readonly TestCommandLineWrapper _commandLineWrapper;
        private readonly TestDirectoryManager _directoryManager;
        private readonly ProjectDefinitionParser _projectDefinitionParser;
        private readonly RecipeDefinition _recipeDefinition;
        private readonly TestFileManager _fileManager;
        private readonly IDeploymentManifestEngine _deploymentManifestEngine;
        private readonly IOrchestratorInteractiveService _orchestratorInteractiveService;
        private readonly IRecipeHandler _recipeHandler;

        public DeploymentBundleHandlerTests()
        {
            var awsResourceQueryer = new TestToolAWSResourceQueryer();
            var interactiveService = new TestToolOrchestratorInteractiveService();
            var zipFileManager = new TestZipFileManager();
            var serviceProvider = new Mock<IServiceProvider>().Object;

            _commandLineWrapper = new TestCommandLineWrapper();
            _fileManager = new TestFileManager();
            _directoryManager = new TestDirectoryManager();
            var recipeFiles = Directory.GetFiles(RecipeLocator.FindRecipeDefinitionsPath(), "*.recipe", SearchOption.TopDirectoryOnly);
            _directoryManager.AddedFiles.Add(RecipeLocator.FindRecipeDefinitionsPath(), new HashSet<string> (recipeFiles));
            foreach (var recipeFile in recipeFiles)
                _fileManager.InMemoryStore.Add(recipeFile, File.ReadAllText(recipeFile));
            _deploymentManifestEngine = new DeploymentManifestEngine(_directoryManager, _fileManager);
            _orchestratorInteractiveService = new TestToolOrchestratorInteractiveService();
            var validatorFactory = new ValidatorFactory(serviceProvider);
            var optionSettingHandler = new OptionSettingHandler(validatorFactory);
            _recipeHandler = new RecipeHandler(_deploymentManifestEngine, _orchestratorInteractiveService, _directoryManager, _fileManager, optionSettingHandler, validatorFactory);
            _projectDefinitionParser = new ProjectDefinitionParser(new FileManager(), new DirectoryManager());

            _deploymentBundleHandler = new DeploymentBundleHandler(_commandLineWrapper, awsResourceQueryer, interactiveService, _directoryManager, zipFileManager, new FileManager(), optionSettingHandler);

            _recipeDefinition = new Mock<RecipeDefinition>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DeploymentTypes>(),
                It.IsAny<DeploymentBundleTypes>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()).Object;
        }

        [Fact]
        public async Task BuildDockerImage_DockerExecutionDirectoryNotSet()
        {
            var projectPath = SystemIOUtilities.ResolvePath("ConsoleAppTask");
            var project = await _projectDefinitionParser.Parse(projectPath);
            var options = new List<OptionSettingItem>()
            {
                new OptionSettingItem("DockerfilePath", "", "", "")
            };
            var recommendation = new Recommendation(_recipeDefinition, project, 100, new Dictionary<string, object>());

            var cloudApplication = new CloudApplication("ConsoleAppTask", String.Empty, CloudApplicationResourceType.CloudFormationStack, string.Empty);
            var imageTag = "imageTag";
            await _deploymentBundleHandler.BuildDockerImage(cloudApplication, recommendation, imageTag);

            var expectedDockerFile = Path.GetFullPath(Path.Combine(".", "Dockerfile"), recommendation.GetProjectDirectory());
            var dockerExecutionDirectory = Directory.GetParent(Path.GetFullPath(recommendation.ProjectPath)).Parent.Parent;

            if (RuntimeInformation.OSArchitecture.Equals(Architecture.X64))
            {
                Assert.Equal($"docker build -t {imageTag} -f \"{expectedDockerFile}\" .",
                    _commandLineWrapper.CommandsToExecute.First().Command);
            }
            else
            {
                Assert.Equal($"docker buildx build --platform linux/amd64 -t {imageTag} -f \"{expectedDockerFile}\" .",
                    _commandLineWrapper.CommandsToExecute.First().Command);
            }
            Assert.Equal(dockerExecutionDirectory.FullName,
                _commandLineWrapper.CommandsToExecute.First().WorkingDirectory);
        }

        [Fact]
        public async Task BuildDockerImage_DockerExecutionDirectorySet()
        {
            var projectPath = new DirectoryInfo(SystemIOUtilities.ResolvePath("ConsoleAppTask")).FullName;
            var project = await _projectDefinitionParser.Parse(projectPath);
            var options = new List<OptionSettingItem>()
            {
                new OptionSettingItem("DockerfilePath", "", "", "")
            };
            var recommendation = new Recommendation(_recipeDefinition, project, 100, new Dictionary<string, object>());

            recommendation.DeploymentBundle.DockerExecutionDirectory = projectPath;

            var cloudApplication = new CloudApplication("ConsoleAppTask", string.Empty, CloudApplicationResourceType.CloudFormationStack, string.Empty);
            var imageTag = "imageTag";
            await _deploymentBundleHandler.BuildDockerImage(cloudApplication, recommendation, imageTag);

            var expectedDockerFile = Path.GetFullPath(Path.Combine(".", "Dockerfile"), recommendation.GetProjectDirectory());

            if (RuntimeInformation.OSArchitecture.Equals(Architecture.X64))
            {
                Assert.Equal($"docker build -t {imageTag} -f \"{expectedDockerFile}\" .",
                    _commandLineWrapper.CommandsToExecute.First().Command);
            }
            else
            {
                Assert.Equal($"docker buildx build --platform linux/amd64 -t {imageTag} -f \"{expectedDockerFile}\" .",
                    _commandLineWrapper.CommandsToExecute.First().Command);
            }
            Assert.Equal(projectPath,
                _commandLineWrapper.CommandsToExecute.First().WorkingDirectory);
        }

        /// <summary>
        /// Tests the Dockerfile being located in a subfolder instead of the project root
        /// </summary>
        [Fact]
        public async Task BuildDockerImage_AlternativeDockerfilePathSet()
        {
            var projectPath = SystemIOUtilities.ResolvePath("ConsoleAppTask");
            var project = await _projectDefinitionParser.Parse(projectPath);
            var options = new List<OptionSettingItem>()
            {
                new OptionSettingItem("DockerfilePath", "", "", "")
            };
            var recommendation = new Recommendation(_recipeDefinition, project, 100, new Dictionary<string, object>());

            var dockerfilePath = Path.Combine(projectPath, "Docker", "Dockerfile");
            var expectedDockerExecutionDirectory = Directory.GetParent(Path.GetFullPath(recommendation.ProjectPath)).Parent.Parent;

            recommendation.DeploymentBundle.DockerfilePath = dockerfilePath;

            var cloudApplication = new CloudApplication("ConsoleAppTask", string.Empty, CloudApplicationResourceType.CloudFormationStack, recommendation.Recipe.Id);
            var imageTag = "imageTag";
            await _deploymentBundleHandler.BuildDockerImage(cloudApplication, recommendation, imageTag);
            if (RuntimeInformation.OSArchitecture.Equals(Architecture.X64))
            {
                Assert.Equal($"docker build -t {imageTag} -f \"{dockerfilePath}\" .",
                    _commandLineWrapper.CommandsToExecute.First().Command);
            }
            else
            {
                Assert.Equal($"docker buildx build --platform linux/amd64 -t {imageTag} -f \"{dockerfilePath}\" .",
                    _commandLineWrapper.CommandsToExecute.First().Command);
            }
            Assert.Equal(expectedDockerExecutionDirectory.FullName,
                _commandLineWrapper.CommandsToExecute.First().WorkingDirectory);
        }

        [Fact]
        public async Task PushDockerImage_RepositoryNameCheck()
        {
            var projectPath = SystemIOUtilities.ResolvePath("ConsoleAppTask");
            var project = await _projectDefinitionParser.Parse(projectPath);
            var recommendation = new Recommendation(_recipeDefinition, project, 100, new Dictionary<string, object>());
            var repositoryName = "repository";

            await _deploymentBundleHandler.PushDockerImageToECR(recommendation, repositoryName, "ConsoleAppTask:latest");

            Assert.Equal(repositoryName, recommendation.DeploymentBundle.ECRRepositoryName);
        }

        [Fact]
        public async Task InspectDockerImage_ExecutedCommandCheck()
        {
            var projectPath = new DirectoryInfo(SystemIOUtilities.ResolvePath("WebAppNet8WithCustomDockerFile")).FullName;
            var project = await _projectDefinitionParser.Parse(projectPath);
            var recommendation = new Recommendation(_recipeDefinition, project, 100, new Dictionary<string, object>());

            recommendation.DeploymentBundle.DockerExecutionDirectory = projectPath;

            await _deploymentBundleHandler.InspectDockerImageEnvironmentVariables(recommendation, "imageTag");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Assert.Equal("docker inspect --format '{{ index (index .Config.Env) }}' imageTag",
                _commandLineWrapper.CommandsToExecute.First().Command);
            }
            else
            {
                Assert.Equal("docker inspect --format \"{{ index (index .Config.Env) }}\" imageTag",
                _commandLineWrapper.CommandsToExecute.First().Command);
            }
        }

        [Fact]
        public async Task CreateDotnetPublishZip_NotSelfContained()
        {
            var projectPath = SystemIOUtilities.ResolvePath("ConsoleAppTask");
            var project = await _projectDefinitionParser.Parse(projectPath);
            _recipeDefinition.OptionSettings.Add(
                new OptionSettingItem(
                    "ElasticBeanstalkPlatformArn",
                    "ElasticBeanstalkPlatformArn",
                    "Beanstalk Platform",
                    "The name of the Elastic Beanstalk platform to use with the environment.") { DefaultValue = "arn:aws:elasticbeanstalk:us-west-2::platform/.NET 8 running on 64bit Amazon Linux 2023/3.1.3" });
            var recommendation = new Recommendation(_recipeDefinition, project, 100, new Dictionary<string, object>());

            recommendation.DeploymentBundle.DotnetPublishSelfContainedBuild = false;
            recommendation.DeploymentBundle.DotnetPublishBuildConfiguration = "Release";
            recommendation.DeploymentBundle.DotnetPublishAdditionalBuildArguments = "--nologo";

            await _deploymentBundleHandler.CreateDotnetPublishZip(recommendation);

            var expectedCommand =
                $"dotnet publish \"{project.ProjectPath}\"" +
                $" -o \"{_directoryManager.CreatedDirectories.First()}\"" +
                " -c Release" +
                " " +
                " --nologo";

            Assert.Equal(expectedCommand, _commandLineWrapper.CommandsToExecute.First().Command);
        }

        [Fact]
        public async Task CreateDotnetPublishZip_SelfContained()
        {
            var projectPath = SystemIOUtilities.ResolvePath("ConsoleAppTask");
            var project = await _projectDefinitionParser.Parse(projectPath);
            _recipeDefinition.OptionSettings.Add(
                new OptionSettingItem(
                    "ElasticBeanstalkPlatformArn",
                    "ElasticBeanstalkPlatformArn",
                    "Beanstalk Platform",
                    "The name of the Elastic Beanstalk platform to use with the environment.") { DefaultValue = "arn:aws:elasticbeanstalk:us-west-2::platform/.NET 8 running on 64bit Amazon Linux 2023/3.1.3" });
            var recommendation = new Recommendation(_recipeDefinition, project, 100, new Dictionary<string, object>());

            recommendation.DeploymentBundle.DotnetPublishSelfContainedBuild = true;
            recommendation.DeploymentBundle.DotnetPublishBuildConfiguration = "Release";
            recommendation.DeploymentBundle.DotnetPublishAdditionalBuildArguments = "--nologo";

            await _deploymentBundleHandler.CreateDotnetPublishZip(recommendation);

            var expectedCommand =
                $"dotnet publish \"{project.ProjectPath}\"" +
                $" -o \"{_directoryManager.CreatedDirectories.First()}\"" +
                " -c Release" +
                " --runtime linux-x64" +
                " --nologo" +
                " --self-contained true";

            Assert.Equal(expectedCommand, _commandLineWrapper.CommandsToExecute.First().Command);
        }

        /// <summary>
        /// Since Beanstalk doesn't currently have .NET 7 preinstalled we need to make sure we are doing a self-contained publish when creating the deployment bundle.
        /// This test checks when the target framework is net7.0, then we are performing a self-contained build.
        /// </summary>
        [Fact]
        public async Task CreateDotnetPublishZip_SelfContained_Net7()
        {
            var projectPath = SystemIOUtilities.ResolvePath(Path.Combine("docker", "WebAppNet7"));
            var project = await _projectDefinitionParser.Parse(projectPath);
            _recipeDefinition.TargetService = RecipeIdentifier.TARGET_SERVICE_ELASTIC_BEANSTALK;
            _recipeDefinition.OptionSettings.Add(
                new OptionSettingItem(
                    "ElasticBeanstalkPlatformArn",
                    "ElasticBeanstalkPlatformArn",
                    "Beanstalk Platform",
                    "The name of the Elastic Beanstalk platform to use with the environment.") { DefaultValue = "arn:aws:elasticbeanstalk:us-west-2::platform/.NET 8 running on 64bit Amazon Linux 2023/3.1.3" });
            var recommendation = new Recommendation(_recipeDefinition, project, 100, new Dictionary<string, object>());

            recommendation.DeploymentBundle.DotnetPublishBuildConfiguration = "Release";
            recommendation.DeploymentBundle.DotnetPublishAdditionalBuildArguments = "--nologo";

            Assert.False(recommendation.DeploymentBundle.DotnetPublishSelfContainedBuild);

            await _deploymentBundleHandler.CreateDotnetPublishZip(recommendation);

            Assert.True(recommendation.DeploymentBundle.DotnetPublishSelfContainedBuild);

            var expectedCommand =
                $"dotnet publish \"{project.ProjectPath}\"" +
                $" -o \"{_directoryManager.CreatedDirectories.First()}\"" +
                " -c Release" +
                " --runtime linux-x64" +
                " --nologo" +
                " --self-contained true";

            Assert.Equal(expectedCommand, _commandLineWrapper.CommandsToExecute.First().Command);
        }

        [Fact]
        public async Task CreateDotnetPublishZip_UnsupportedFramework_AlreadySetAsSelfContained()
        {
            var projectPath = SystemIOUtilities.ResolvePath(Path.Combine("docker", "WebAppNet7"));
            var project = await _projectDefinitionParser.Parse(projectPath);
            _recipeDefinition.TargetService = RecipeIdentifier.TARGET_SERVICE_ELASTIC_BEANSTALK;
            _recipeDefinition.OptionSettings.Add(
                new OptionSettingItem(
                    "ElasticBeanstalkPlatformArn",
                    "ElasticBeanstalkPlatformArn",
                    "Beanstalk Platform",
                    "The name of the Elastic Beanstalk platform to use with the environment.")
                { DefaultValue = "arn:aws:elasticbeanstalk:us-west-2::platform/.NET 8 running on 64bit Amazon Linux 2023/3.1.3" });
            var recommendation = new Recommendation(_recipeDefinition, project, 100, new Dictionary<string, object>());

            recommendation.DeploymentBundle.DotnetPublishBuildConfiguration = "Release";
            recommendation.DeploymentBundle.DotnetPublishSelfContainedBuild = true;

            Assert.True(recommendation.DeploymentBundle.DotnetPublishSelfContainedBuild);

            await _deploymentBundleHandler.CreateDotnetPublishZip(recommendation);

            Assert.True(recommendation.DeploymentBundle.DotnetPublishSelfContainedBuild);

            var expectedCommand =
                $"dotnet publish \"{project.ProjectPath}\"" +
                $" -o \"{_directoryManager.CreatedDirectories.First()}\"" +
                " -c Release" +
                " --runtime linux-x64 " +
                " --self-contained true";

            Assert.Equal(expectedCommand, _commandLineWrapper.CommandsToExecute.First().Command);
        }

        [Theory]
        [InlineData("arn:aws:elasticbeanstalk:us-west-2::platform/.NET 8 running on 64bit Amazon Linux 2023")]
        [InlineData("arn:aws:elasticbeanstalk:us-west-2::platform/.NET 8 running on 64bit Amazon Linux 2023/invalidversion")]
        public async Task CreateDotnetPublishZip_InvalidPlatformArn(string platformArn)
        {
            var projectPath = SystemIOUtilities.ResolvePath(Path.Combine("ConsoleAppTask"));
            var project = await _projectDefinitionParser.Parse(projectPath);
            _recipeDefinition.TargetService = RecipeIdentifier.TARGET_SERVICE_ELASTIC_BEANSTALK;
            _recipeDefinition.OptionSettings.Add(
                new OptionSettingItem(
                    "ElasticBeanstalkPlatformArn",
                    "ElasticBeanstalkPlatformArn",
                    "Beanstalk Platform",
                    "The name of the Elastic Beanstalk platform to use with the environment.")
                {
                    DefaultValue = platformArn
                });
            var recommendation = new Recommendation(_recipeDefinition, project, 100, new Dictionary<string, object>());

            await _deploymentBundleHandler.CreateDotnetPublishZip(recommendation);

            Assert.False(recommendation.DeploymentBundle.DotnetPublishSelfContainedBuild);

            var expectedCommand =
                $"dotnet publish \"{project.ProjectPath}\"" +
                $" -o \"{_directoryManager.CreatedDirectories.First()}\"" +
                " -c Release  ";

            Assert.Equal(expectedCommand, _commandLineWrapper.CommandsToExecute.First().Command);
        }

        [Theory]
        [InlineData("arn:aws:elasticbeanstalk:us-west-2::platform/.NET Core running on 64bit Amazon Linux 2/2.7.3")]
        [InlineData("arn:aws:elasticbeanstalk:us-west-2::platform/.NET 6 running on 64bit Amazon Linux 2023/3.1.3")]
        public async Task CreateDotnetPublishZip_PlatformDoesntSupportNet8(string platformArn)
        {
            var projectPath = SystemIOUtilities.ResolvePath(Path.Combine("docker", "WebAppNet8"));
            var project = await _projectDefinitionParser.Parse(projectPath);
            _recipeDefinition.TargetService = RecipeIdentifier.TARGET_SERVICE_ELASTIC_BEANSTALK;
            _recipeDefinition.OptionSettings.Add(
                new OptionSettingItem(
                    "ElasticBeanstalkPlatformArn",
                    "ElasticBeanstalkPlatformArn",
                    "Beanstalk Platform",
                    "The name of the Elastic Beanstalk platform to use with the environment.")
                {
                    DefaultValue = platformArn
                });
            var recommendation = new Recommendation(_recipeDefinition, project, 100, new Dictionary<string, object>());
            recommendation.DeploymentBundle.DotnetPublishBuildConfiguration = "Release";
            recommendation.DeploymentBundle.DotnetPublishAdditionalBuildArguments = "--nologo";

            Assert.False(recommendation.DeploymentBundle.DotnetPublishSelfContainedBuild);

            await _deploymentBundleHandler.CreateDotnetPublishZip(recommendation);

            Assert.True(recommendation.DeploymentBundle.DotnetPublishSelfContainedBuild);

            var expectedCommand =
                $"dotnet publish \"{project.ProjectPath}\"" +
                $" -o \"{_directoryManager.CreatedDirectories.First()}\"" +
                " -c Release" +
                " --runtime linux-x64" +
                " --nologo" +
                " --self-contained true";

            Assert.Equal(expectedCommand, _commandLineWrapper.CommandsToExecute.First().Command);
        }

        private async Task<RecommendationEngine> BuildRecommendationEngine(string testProjectName)
        {
            var fullPath = SystemIOUtilities.ResolvePath(testProjectName);

            var parser = new ProjectDefinitionParser(new FileManager(), new DirectoryManager());
            var awsCredentials = new Mock<AWSCredentials>();
            var session =  new OrchestratorSession(
                await parser.Parse(fullPath),
                awsCredentials.Object,
                "us-west-2",
                "123456789012")
            {
                AWSProfileName = "default"
            };

            return new RecommendationEngine(session, _recipeHandler);
        }

        [Fact]
        public async Task DockerExecutionDirectory_SolutionLevel()
        {
            var projectPath = Path.Combine("docker", "WebAppWithSolutionParentLevel", "WebAppWithSolutionParentLevel");
            var engine = await BuildRecommendationEngine(projectPath);

            var recommendations = await engine.ComputeRecommendations();
            var recommendation = recommendations.FirstOrDefault(x => x.Recipe.DeploymentBundle.Equals(DeploymentBundleTypes.Container));

            var cloudApplication = new CloudApplication("WebAppWithSolutionParentLevel", string.Empty, CloudApplicationResourceType.CloudFormationStack, string.Empty);
            var imageTag = "imageTag";
            await _deploymentBundleHandler.BuildDockerImage(cloudApplication, recommendation, imageTag);

            Assert.Equal(Directory.GetParent(SystemIOUtilities.ResolvePath(projectPath)).FullName, recommendation.DeploymentBundle.DockerExecutionDirectory);
        }

        [Fact]
        public async Task DockerExecutionDirectory_DockerfileLevel()
        {
            var projectPath = Path.Combine("docker", "WebAppNoSolution");
            var engine = await BuildRecommendationEngine(projectPath);

            var recommendations = await engine.ComputeRecommendations();
            var recommendation = recommendations.FirstOrDefault(x => x.Recipe.DeploymentBundle.Equals(DeploymentBundleTypes.Container));

            var cloudApplication = new CloudApplication("WebAppNoSolution", string.Empty, CloudApplicationResourceType.CloudFormationStack, string.Empty);
            var imageTag = "imageTag";
            await _deploymentBundleHandler.BuildDockerImage(cloudApplication, recommendation, imageTag);

            Assert.Equal(Path.GetFullPath(SystemIOUtilities.ResolvePath(projectPath)), recommendation.DeploymentBundle.DockerExecutionDirectory);
        }
    }
}
