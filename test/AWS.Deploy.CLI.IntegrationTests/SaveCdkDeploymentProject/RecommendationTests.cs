// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AWS.Deploy.CLI.Utilities;
using AWS.Deploy.CLI.IntegrationTests.Utilities;
using AWS.Deploy.Common;
using AWS.Deploy.Common.DeploymentManifest;
using AWS.Deploy.Common.IO;
using AWS.Deploy.DockerEngine;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.CDK;
using AWS.Deploy.Recipes;
using Moq;
using Xunit;
using Should;
using AWS.Deploy.Common.Recipes;
using Newtonsoft.Json;

namespace AWS.Deploy.CLI.IntegrationTests.SaveCdkDeploymentProject
{
    [Collection("SaveCdkDeploymentProjectTests")]
    public class RecommendationTests : IDisposable
    {
        private readonly string _testArtifactsDirectoryPath;
        private readonly string _webAppWithDockerFilePath;
        private readonly string _webAppWithNoDockerFilePath;
        private readonly string _blazorAppPath;
        private bool _isDisposed;

        public RecommendationTests()
        {
            var testAppsDirectoryPath = Utilities.ResolvePathToTestApps();
            _webAppWithDockerFilePath = Path.Combine(testAppsDirectoryPath, "WebAppWithDockerFile");
            _webAppWithNoDockerFilePath = Path.Combine(testAppsDirectoryPath, "WebAppNoDockerFile");
            _blazorAppPath = Path.Combine(testAppsDirectoryPath, "BlazorWasm50");
            _testArtifactsDirectoryPath = Path.Combine(testAppsDirectoryPath, "TestArtifacts");
        }

        [Fact]
        public async Task GenerateRecommendationsWithoutCustomRecipes()
        {
            // ARRANGE
            var orchestrator = await GetOrchestrator(_webAppWithDockerFilePath);

            // ACT
            var recommendations = await orchestrator.GenerateDeploymentRecommendations();

            // ASSERT
            recommendations.Count.ShouldEqual(3);
            recommendations[0].Name.ShouldEqual("ASP.NET Core App to Amazon ECS using Fargate"); // default recipe
            recommendations[1].Name.ShouldEqual("ASP.NET Core App to AWS App Runner"); // default recipe
            recommendations[2].Name.ShouldEqual("ASP.NET Core App to AWS Elastic Beanstalk on Linux"); // default recipe

            CleanUp();
        }

        [Fact]
        public async Task GenerateRecommendationsFromCustomRecipesWithManifestFile()
        {
            // ARRANGE
            var orchestrator = await GetOrchestrator(_webAppWithDockerFilePath);

            var saveDirectoryPathEcsProject = Path.Combine(_testArtifactsDirectoryPath, "ECS-CDK");
            var saveDirectoryPathEbsProject = Path.Combine(_testArtifactsDirectoryPath, "EBS-CDK");

            var customEcsRecipeName = "Custom ECS Fargate Recipe";
            var customEbsRecipeName = "Custom Elastic Beanstalk Recipe";

            // select ECS Fargate recipe
            await Utilities.CreateCDKDeploymentProjectWithRecipeName(_webAppWithDockerFilePath, customEcsRecipeName, "1", saveDirectoryPathEcsProject);

            // select Elastic Beanstalk recipe
            await Utilities.CreateCDKDeploymentProjectWithRecipeName(_webAppWithDockerFilePath, customEbsRecipeName, "2", saveDirectoryPathEbsProject);

            // Get custom recipe IDs
            var customEcsRecipeId = await GetCustomRecipeId(Path.Combine(saveDirectoryPathEcsProject, "ECS-CDK.recipe"));
            var customEbsRecipeId = await GetCustomRecipeId(Path.Combine(saveDirectoryPathEbsProject, "EBS-CDK.recipe"));

            // ACT
            var recommendations = await orchestrator.GenerateDeploymentRecommendations();

            // ASSERT - Recipes are ordered by priority
            recommendations.Count.ShouldEqual(5);
            recommendations[0].Name.ShouldEqual(customEcsRecipeName); // custom recipe
            recommendations[1].Name.ShouldEqual(customEbsRecipeName); // custom recipe
            recommendations[2].Name.ShouldEqual("ASP.NET Core App to Amazon ECS using Fargate"); // default recipe
            recommendations[3].Name.ShouldEqual("ASP.NET Core App to AWS App Runner"); // default recipe
            recommendations[4].Name.ShouldEqual("ASP.NET Core App to AWS Elastic Beanstalk on Linux"); // default recipe

            // ASSERT - Recipe paths
            recommendations[0].Recipe.RecipePath.ShouldEqual(Path.Combine(saveDirectoryPathEcsProject, "ECS-CDK.recipe"));
            recommendations[1].Recipe.RecipePath.ShouldEqual(Path.Combine(saveDirectoryPathEbsProject, "EBS-CDK.recipe"));

            // ASSERT - custom recipe IDs
            recommendations[0].Recipe.Id.ShouldEqual(customEcsRecipeId);
            recommendations[1].Recipe.Id.ShouldEqual(customEbsRecipeId);

            File.Exists(Path.Combine(_webAppWithDockerFilePath, "aws-deployments.json")).ShouldBeTrue();

            CleanUp();
        }

        [Fact]
        public async Task GenerateRecommendationsFromCustomRecipesWithoutManifestFile()
        {
            // ARRANGE
            var orchestrator = await GetOrchestrator(_webAppWithNoDockerFilePath);

            var saveDirectoryPathEcsProject = Path.Combine(_testArtifactsDirectoryPath, "ECS-CDK");
            var saveDirectoryPathEbsProject = Path.Combine(_testArtifactsDirectoryPath, "EBS-CDK");

            var customEcsRecipeName = "Custom ECS Fargate Recipe";
            var customEbsRecipeName = "Custom Elastic Beanstalk Recipe";

            // Select ECS Fargate recipe
            await Utilities.CreateCDKDeploymentProjectWithRecipeName(_webAppWithDockerFilePath, customEcsRecipeName, "1", saveDirectoryPathEcsProject);

            // Select Elastic Beanstalk recipe
            await Utilities.CreateCDKDeploymentProjectWithRecipeName(_webAppWithDockerFilePath, customEbsRecipeName, "2", saveDirectoryPathEbsProject);

            // Get custom recipe IDs
            var customEcsRecipeId = await GetCustomRecipeId(Path.Combine(saveDirectoryPathEcsProject, "ECS-CDK.recipe"));
            var customEbsRecipeId = await GetCustomRecipeId(Path.Combine(saveDirectoryPathEbsProject, "EBS-CDK.recipe"));

            // ACT
            var recommendations = await orchestrator.GenerateDeploymentRecommendations();

            // ASSERT - Recipes are ordered by priority
            recommendations.Count.ShouldEqual(5);
            recommendations[0].Name.ShouldEqual(customEbsRecipeName);
            recommendations[1].Name.ShouldEqual(customEcsRecipeName);
            recommendations[2].Name.ShouldEqual("ASP.NET Core App to AWS Elastic Beanstalk on Linux");
            recommendations[3].Name.ShouldEqual("ASP.NET Core App to Amazon ECS using Fargate");
            recommendations[4].Name.ShouldEqual("ASP.NET Core App to AWS App Runner");

            // ASSERT - Recipe paths
            recommendations[0].Recipe.RecipePath.ShouldEqual(Path.Combine(saveDirectoryPathEbsProject, "EBS-CDK.recipe"));
            recommendations[1].Recipe.RecipePath.ShouldEqual(Path.Combine(saveDirectoryPathEcsProject, "ECS-CDK.recipe"));

            // ASSERT - Custom recipe IDs
            recommendations[0].Recipe.Id.ShouldEqual(customEbsRecipeId);
            recommendations[1].Recipe.Id.ShouldEqual(customEcsRecipeId);
            File.Exists(Path.Combine(_webAppWithNoDockerFilePath, "aws-deployments.json")).ShouldBeFalse();

            CleanUp();
        }

        [Fact]
        public async Task GenerateRecommendationsFromCompatibleDeploymentProject()
        {
            // ARRANGE
            var orchestrator = await GetOrchestrator(_webAppWithDockerFilePath);
            var saveDirectoryPathEcsProject = Path.Combine(_testArtifactsDirectoryPath, "ECS-CDK");
            var customEcsRecipeName = "Custom ECS Fargate Recipe";

            // Select ECS Fargate recipe
            await Utilities.CreateCDKDeploymentProjectWithRecipeName(_webAppWithDockerFilePath, customEcsRecipeName, "1", saveDirectoryPathEcsProject);

            // Get custom recipe IDs
            var customEcsRecipeId = await GetCustomRecipeId(Path.Combine(saveDirectoryPathEcsProject, "ECS-CDK.recipe"));

            // ACT
            var recommendations = await orchestrator.GenerateRecommendationsFromSavedDeploymentProject(saveDirectoryPathEcsProject);

            // ASSERT 
            recommendations.Count.ShouldEqual(1);
            recommendations[0].Name.ShouldEqual("Custom ECS Fargate Recipe");
            recommendations[0].Recipe.Id.ShouldEqual(customEcsRecipeId);
            recommendations[0].Recipe.RecipePath.ShouldEqual(Path.Combine(saveDirectoryPathEcsProject, "ECS-CDK.recipe"));

            CleanUp();
        }

        [Fact]
        public async Task GenerateRecommendationsFromIncompatibleDeploymentProject()
        {
            // ARRANGE
            var orchestrator = await GetOrchestrator(_blazorAppPath);
            var saveDirectoryPathEcsProject = Path.Combine(_testArtifactsDirectoryPath, "ECS-CDK");
            await Utilities.CreateCDKDeploymentProject(_webAppWithDockerFilePath, saveDirectoryPathEcsProject);

            // ACT
            var recommendations = await orchestrator.GenerateRecommendationsFromSavedDeploymentProject(saveDirectoryPathEcsProject);

            // ASSERT 
            recommendations.ShouldBeEmpty();

            CleanUp();
        }

        private async Task<Orchestrator> GetOrchestrator(string targetApplicationProjectPath)
        {
            var directoryManager = new DirectoryManager();
            var fileManager = new FileManager();
            var deploymentManifestEngine = new DeploymentManifestEngine(directoryManager, fileManager);
            var consoleInteractiveServiceImpl = new ConsoleInteractiveServiceImpl();
            var consoleOrchestratorLogger = new ConsoleOrchestratorLogger(consoleInteractiveServiceImpl);
            var commandLineWrapper = new CommandLineWrapper(consoleOrchestratorLogger);
            var customRecipeLocator = new CustomRecipeLocator(deploymentManifestEngine, consoleOrchestratorLogger, commandLineWrapper, directoryManager);

            var projectDefinition = await new ProjectDefinitionParser(fileManager, directoryManager).Parse(targetApplicationProjectPath);
            var session = new OrchestratorSession(projectDefinition);

            return new Orchestrator(session,
                consoleOrchestratorLogger,
                new Mock<ICdkProjectHandler>().Object,
                new Mock<ICDKManager>().Object,
                new TestToolAWSResourceQueryer(),
                new Mock<IDeploymentBundleHandler>().Object,
                new Mock<IDockerEngine>().Object,
                customRecipeLocator,
                new List<string> { RecipeLocator.FindRecipeDefinitionsPath() });
        }

        private async Task<string> GetCustomRecipeId(string recipeFilePath)
        {
            var recipeBody = await File.ReadAllTextAsync(recipeFilePath);
            var recipe = JsonConvert.DeserializeObject<RecipeDefinition>(recipeBody);
            return recipe.Id;
        }

        private void CleanUp()
        {
            if (Directory.Exists(_testArtifactsDirectoryPath))
                Directory.Delete(_testArtifactsDirectoryPath, true);

            if (File.Exists(Path.Combine(_webAppWithDockerFilePath, "aws-deployments.json")))
                File.Delete(Path.Combine(_webAppWithDockerFilePath, "aws-deployments.json"));

            if (File.Exists(Path.Combine(_webAppWithNoDockerFilePath, "aws-deployments.json")))
                File.Delete(Path.Combine(_webAppWithNoDockerFilePath, "aws-deployments.json"));

            if (File.Exists(Path.Combine(_blazorAppPath, "aws-deployments.json")))
                File.Delete(Path.Combine(_blazorAppPath, "aws-deployments.json"));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                CleanUp();
            }

            _isDisposed = true;
        }

        ~RecommendationTests()
        {
            Dispose(false);
        }
    }
}
