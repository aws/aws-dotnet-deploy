// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.CLI.UnitTests.Utilities;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
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
        private readonly TestToolOrchestratorInteractiveService _interactiveService;

        public DeploymentBundleHandlerTests()
        {
            var awsResourceQueryer = new TestToolAWSResourceQueryer();
            _interactiveService = new TestToolOrchestratorInteractiveService();
            var zipFileManager = new TestZipFileManager();
            
            _commandLineWrapper = new TestToolCommandLineWrapper();
            _directoryManager = new TestDirectoryManager();
            _projectDefinitionParser = new ProjectDefinitionParser(new FileManager(), new DirectoryManager());
            
            _deploymentBundleHandler = new DeploymentBundleHandler(_commandLineWrapper, awsResourceQueryer, _interactiveService, _directoryManager, zipFileManager);
        }

        [Fact]
        public async Task BuildDockerImage_DockerExecutionDirectoryNotSet()
        {
            var projectPath = SystemIOUtilities.ResolvePath("ConsoleAppTask");
            var project = await _projectDefinitionParser.Parse(projectPath);
            var recipeDefinition = new Mock<RecipeDefinition>();
            var recommendation = new Recommendation(recipeDefinition.Object, project, 100, new Dictionary<string, string>());

            var cloudApplication = new CloudApplication { Name = "ConsoleAppTask" };
            var result = await _deploymentBundleHandler.BuildDockerImage(cloudApplication, recommendation);

            var dockerFile = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(recommendation.ProjectPath)), "Dockerfile");
            var dockerExecutionDirectory = Directory.GetParent(Path.GetFullPath(recommendation.ProjectPath)).Parent.Parent;

            Assert.Equal($"docker build -t {result} -f \"{dockerFile}\" .",
                _commandLineWrapper.CommandsToExecute.First().Command);
            Assert.Equal(dockerExecutionDirectory.FullName,
                _commandLineWrapper.CommandsToExecute.First().WorkingDirectory);
        }

        [Fact]
        public async Task BuildDockerImage_DockerExecutionDirectorySet()
        {
            var projectPath = SystemIOUtilities.ResolvePath("ConsoleAppTask");
            var project = await _projectDefinitionParser.Parse(projectPath);
            var recipeDefinition = new Mock<RecipeDefinition>();
            var recommendation = new Recommendation(recipeDefinition.Object, project, 100, new Dictionary<string, string>());

            recommendation.DeploymentBundle.DockerExecutionDirectory = projectPath;

            var cloudApplication = new CloudApplication { Name = "ConsoleAppTask" };
            var result = await _deploymentBundleHandler.BuildDockerImage(cloudApplication, recommendation);

            var dockerFile = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(recommendation.ProjectPath)), "Dockerfile");

            Assert.Equal($"docker build -t {result} -f \"{dockerFile}\" .",
                _commandLineWrapper.CommandsToExecute.First().Command);
            Assert.Equal(projectPath,
                _commandLineWrapper.CommandsToExecute.First().WorkingDirectory);
        }

        [Fact]
        public async Task PushDockerImage_RepositoryNameCheck()
        {
            var projectPath = SystemIOUtilities.ResolvePath("ConsoleAppTask");
            var project = await _projectDefinitionParser.Parse(projectPath);
            var recipeDefinition = new Mock<RecipeDefinition>();
            var recommendation = new Recommendation(recipeDefinition.Object, project, 100, new Dictionary<string, string>());

            var cloudApplication = new CloudApplication { Name = "ConsoleAppTask" };
            await _deploymentBundleHandler.PushDockerImageToECR(cloudApplication, recommendation, "ConsoleAppTask:latest");

            Assert.Equal(cloudApplication.StackName.ToLower(), recommendation.DeploymentBundle.ECRRepositoryName);
        }

        [Fact]
        public async Task CreateDotnetPublishZip_NotSelfContained()
        {
            var projectPath = SystemIOUtilities.ResolvePath("ConsoleAppTask");
            var project = await _projectDefinitionParser.Parse(projectPath);
            var recipeDefinition = new Mock<RecipeDefinition>();
            var recommendation = new Recommendation(recipeDefinition.Object, project, 100, new Dictionary<string, string>());

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
            var recipeDefinition = new Mock<RecipeDefinition>();
            var recommendation = new Recommendation(recipeDefinition.Object, project, 100, new Dictionary<string, string>());

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

            var session =  new OrchestratorSession
            {
                ProjectDefinition = await parser.Parse(fullPath)
            };

            return new RecommendationEngine(new[] { RecipeLocator.FindRecipeDefinitionsPath() }, session, _interactiveService);
        }

        [Fact]
        public async Task DockerExecutionDirectory_SolutionLevel()
        {
            var projectPath = SystemIOUtilities.ResolvePath("docker\\WebAppWithSolutionParentLevel\\WebAppWithSolutionParentLevel");

            var session =  new OrchestratorSession
            {
                ProjectDefinition = await _projectDefinitionParser.Parse(projectPath)
            };

            var engine = new RecommendationEngine(new[] { RecipeLocator.FindRecipeDefinitionsPath() }, session, _interactiveService);

            var recommendations = await engine.ComputeRecommendations();
            var recommendation = recommendations.FirstOrDefault(x => x.Recipe.DeploymentBundle.Equals(DeploymentBundleTypes.Container));

            var cloudApplication = new CloudApplication { Name = "WebAppWithSolutionParentLevel" };
            var result = await _deploymentBundleHandler.BuildDockerImage(cloudApplication, recommendation);

            Assert.Equal(Directory.GetParent(projectPath).FullName, recommendation.DeploymentBundle.DockerExecutionDirectory);
        }

        [Fact]
        public async Task DockerExecutionDirectory_DockerfileLevel()
        {
            var projectPath = SystemIOUtilities.ResolvePath("docker\\WebAppNoSolution");

            var session =  new OrchestratorSession
            {
                ProjectDefinition = await _projectDefinitionParser.Parse(projectPath)
            };

            var engine = new RecommendationEngine(new[] { RecipeLocator.FindRecipeDefinitionsPath() }, session, _interactiveService);

            var recommendations = await engine.ComputeRecommendations();
            var recommendation = recommendations.FirstOrDefault(x => x.Recipe.DeploymentBundle.Equals(DeploymentBundleTypes.Container));

            var cloudApplication = new CloudApplication { Name = "WebAppNoSolution" };
            var result = await _deploymentBundleHandler.BuildDockerImage(cloudApplication, recommendation);

            Assert.Equal(Path.GetFullPath(projectPath), recommendation.DeploymentBundle.DockerExecutionDirectory);
        }
    }
}
