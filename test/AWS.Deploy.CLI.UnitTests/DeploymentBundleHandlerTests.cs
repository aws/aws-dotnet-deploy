// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Runtime;
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.CLI.UnitTests.Utilities;
using AWS.Deploy.Common;
using AWS.Deploy.Common.DeploymentManifest;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Recipes.Validation;
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
        private readonly TestToolCommandLineWrapper _commandLineWrapper;
        private readonly TestDirectoryManager _directoryManager;
        private readonly ProjectDefinitionParser _projectDefinitionParser;
        private readonly RecipeDefinition _recipeDefinition;
        private readonly IFileManager _fileManager;
        private readonly IDeploymentManifestEngine _deploymentManifestEngine;
        private readonly IOrchestratorInteractiveService _orchestratorInteractiveService;
        private readonly IRecipeHandler _recipeHandler;

        public DeploymentBundleHandlerTests()
        {
            var awsResourceQueryer = new TestToolAWSResourceQueryer();
            var interactiveService = new TestToolOrchestratorInteractiveService();
            var zipFileManager = new TestZipFileManager();

            _commandLineWrapper = new TestToolCommandLineWrapper();
            _fileManager = new TestFileManager();
            _directoryManager = new TestDirectoryManager();
            _deploymentManifestEngine = new DeploymentManifestEngine(_directoryManager, _fileManager);
            _orchestratorInteractiveService = new TestToolOrchestratorInteractiveService();
            var serviceProvider = new Mock<IServiceProvider>();
            var validatorFactory = new ValidatorFactory(serviceProvider.Object);
            var optionSettingHandler = new OptionSettingHandler(validatorFactory);
            _recipeHandler = new RecipeHandler(_deploymentManifestEngine, _orchestratorInteractiveService, _directoryManager, optionSettingHandler);
            _projectDefinitionParser = new ProjectDefinitionParser(new FileManager(), new DirectoryManager());

            _deploymentBundleHandler = new DeploymentBundleHandler(_commandLineWrapper, awsResourceQueryer, interactiveService, _directoryManager, zipFileManager);

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

            var recommendation = new Recommendation(_recipeDefinition, project, new List<OptionSettingItem>(), 100, new Dictionary<string, string>());

            var cloudApplication = new CloudApplication("ConsoleAppTask", String.Empty, CloudApplicationResourceType.CloudFormationStack, string.Empty);
            var imageTag = "imageTag";
            await _deploymentBundleHandler.BuildDockerImage(cloudApplication, recommendation, imageTag);

            var dockerFile = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(recommendation.ProjectPath)), "Dockerfile");
            var dockerExecutionDirectory = Directory.GetParent(Path.GetFullPath(recommendation.ProjectPath)).Parent.Parent;

            Assert.Equal($"docker build -t {imageTag} -f \"{dockerFile}\" .",
                _commandLineWrapper.CommandsToExecute.First().Command);
            Assert.Equal(dockerExecutionDirectory.FullName,
                _commandLineWrapper.CommandsToExecute.First().WorkingDirectory);
        }

        [Fact]
        public async Task BuildDockerImage_DockerExecutionDirectorySet()
        {
            var projectPath = SystemIOUtilities.ResolvePath("ConsoleAppTask");
            var project = await _projectDefinitionParser.Parse(projectPath);
            var recommendation = new Recommendation(_recipeDefinition, project, new List<OptionSettingItem>(), 100, new Dictionary<string, string>());

            recommendation.DeploymentBundle.DockerExecutionDirectory = projectPath;

            var cloudApplication = new CloudApplication("ConsoleAppTask", string.Empty, CloudApplicationResourceType.CloudFormationStack, string.Empty);
            var imageTag = "imageTag";
            await _deploymentBundleHandler.BuildDockerImage(cloudApplication, recommendation, imageTag);

            var dockerFile = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(recommendation.ProjectPath)), "Dockerfile");

            Assert.Equal($"docker build -t {imageTag} -f \"{dockerFile}\" .",
                _commandLineWrapper.CommandsToExecute.First().Command);
            Assert.Equal(projectPath,
                _commandLineWrapper.CommandsToExecute.First().WorkingDirectory);
        }

        [Fact]
        public async Task PushDockerImage_RepositoryNameCheck()
        {
            var projectPath = SystemIOUtilities.ResolvePath("ConsoleAppTask");
            var project = await _projectDefinitionParser.Parse(projectPath);
            var recommendation = new Recommendation(_recipeDefinition, project, new List<OptionSettingItem>(), 100, new Dictionary<string, string>());
            var repositoryName = "repository";

            await _deploymentBundleHandler.PushDockerImageToECR(recommendation, repositoryName, "ConsoleAppTask:latest");

            Assert.Equal(repositoryName, recommendation.DeploymentBundle.ECRRepositoryName);
        }

        [Fact]
        public async Task CreateDotnetPublishZip_NotSelfContained()
        {
            var projectPath = SystemIOUtilities.ResolvePath("ConsoleAppTask");
            var project = await _projectDefinitionParser.Parse(projectPath);
            var recommendation = new Recommendation(_recipeDefinition, project, new List<OptionSettingItem>(), 100, new Dictionary<string, string>());

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
            var recommendation = new Recommendation(_recipeDefinition, project, new List<OptionSettingItem>(), 100, new Dictionary<string, string>());

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
