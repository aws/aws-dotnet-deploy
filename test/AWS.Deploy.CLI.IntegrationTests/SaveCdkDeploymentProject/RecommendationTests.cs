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
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.Orchestration.LocalUserSettings;

namespace AWS.Deploy.CLI.IntegrationTests.SaveCdkDeploymentProject
{
    public class RecommendationTests 
    {
        private readonly CommandLineWrapper _commandLineWrapper;

        public RecommendationTests()
        {
            _commandLineWrapper = new CommandLineWrapper(new ConsoleOrchestratorLogger(new ConsoleInteractiveServiceImpl()));
        }

        [Fact]
        public async Task GenerateRecommendationsWithoutCustomRecipes()
        {
            // ARRANGE
            var tempDirectoryPath = new TestAppManager().GetProjectPath(string.Empty);
            var webAppWithDockerFilePath = Path.Combine(tempDirectoryPath, "testapps", "WebAppWithDockerFile");
            var orchestrator = await GetOrchestrator(webAppWithDockerFilePath);
            await _commandLineWrapper.Run("git init", tempDirectoryPath);

            // ACT
            var recommendations = await orchestrator.GenerateDeploymentRecommendations();

            // ASSERT
            recommendations.Count.ShouldEqual(3);
            recommendations[0].Name.ShouldEqual("ASP.NET Core App to Amazon ECS using Fargate"); // default recipe
            recommendations[1].Name.ShouldEqual("ASP.NET Core App to AWS App Runner"); // default recipe
            recommendations[2].Name.ShouldEqual("ASP.NET Core App to AWS Elastic Beanstalk on Linux"); // default recipe
        }

        [Fact]
        public async Task GenerateRecommendationsFromCustomRecipesWithManifestFile()
        {
            // ARRANGE
            var tempDirectoryPath = new TestAppManager().GetProjectPath(string.Empty);
            var webAppWithDockerFilePath = Path.Combine(tempDirectoryPath, "testapps", "WebAppWithDockerFile");
            var orchestrator = await GetOrchestrator(webAppWithDockerFilePath);
            await _commandLineWrapper.Run("git init", tempDirectoryPath);

            var saveDirectoryPathEcsProject = Path.Combine(tempDirectoryPath, "ECS-CDK");
            var saveDirectoryPathEbsProject = Path.Combine(tempDirectoryPath, "EBS-CDK");

            var customEcsRecipeName = "Custom ECS Fargate Recipe";
            var customEbsRecipeName = "Custom Elastic Beanstalk Recipe";

            // select ECS Fargate recipe
            await Utilities.CreateCDKDeploymentProjectWithRecipeName(webAppWithDockerFilePath, customEcsRecipeName, "1", saveDirectoryPathEcsProject);

            // select Elastic Beanstalk recipe
            await Utilities.CreateCDKDeploymentProjectWithRecipeName(webAppWithDockerFilePath, customEbsRecipeName, "3", saveDirectoryPathEbsProject);

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

            File.Exists(Path.Combine(webAppWithDockerFilePath, "aws-deployments.json")).ShouldBeTrue();
        }

        [Fact]
        public async Task GenerateRecommendationsFromCustomRecipesWithoutManifestFile()
        {
            // ARRANGE
            var tempDirectoryPath = new TestAppManager().GetProjectPath(string.Empty);
            var webAppWithDockerFilePath = Path.Combine(tempDirectoryPath, "testapps", "WebAppWithDockerFile");
            var webAppNoDockerFilePath = Path.Combine(tempDirectoryPath, "testapps", "WebAppNoDockerFile");
            var orchestrator = await GetOrchestrator(webAppNoDockerFilePath);
            await _commandLineWrapper.Run("git init", tempDirectoryPath);

            var saveDirectoryPathEcsProject = Path.Combine(tempDirectoryPath, "ECS-CDK");
            var saveDirectoryPathEbsProject = Path.Combine(tempDirectoryPath, "EBS-CDK");

            var customEcsRecipeName = "Custom ECS Fargate Recipe";
            var customEbsRecipeName = "Custom Elastic Beanstalk Recipe";

            // Select ECS Fargate recipe
            await Utilities.CreateCDKDeploymentProjectWithRecipeName(webAppWithDockerFilePath, customEcsRecipeName, "1", saveDirectoryPathEcsProject);

            // Select Elastic Beanstalk recipe
            await Utilities.CreateCDKDeploymentProjectWithRecipeName(webAppWithDockerFilePath, customEbsRecipeName, "3", saveDirectoryPathEbsProject);

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
            File.Exists(Path.Combine(webAppNoDockerFilePath, "aws-deployments.json")).ShouldBeFalse();
        }

        [Fact]
        public async Task GenerateRecommendationsFromCompatibleDeploymentProject()
        {
            // ARRANGE
            var tempDirectoryPath = new TestAppManager().GetProjectPath(string.Empty);
            var webAppWithDockerFilePath = Path.Combine(tempDirectoryPath, "testapps", "WebAppWithDockerFile");
            var orchestrator = await GetOrchestrator(webAppWithDockerFilePath);
            await _commandLineWrapper.Run("git init", tempDirectoryPath);

            var saveDirectoryPathEcsProject = Path.Combine(tempDirectoryPath, "ECS-CDK");
            var customEcsRecipeName = "Custom ECS Fargate Recipe";

            // Select ECS Fargate recipe
            await Utilities.CreateCDKDeploymentProjectWithRecipeName(webAppWithDockerFilePath, customEcsRecipeName, "1", saveDirectoryPathEcsProject);

            // Get custom recipe IDs
            var customEcsRecipeId = await GetCustomRecipeId(Path.Combine(saveDirectoryPathEcsProject, "ECS-CDK.recipe"));

            // ACT
            var recommendations = await orchestrator.GenerateRecommendationsFromSavedDeploymentProject(saveDirectoryPathEcsProject);

            // ASSERT 
            recommendations.Count.ShouldEqual(1);
            recommendations[0].Name.ShouldEqual("Custom ECS Fargate Recipe");
            recommendations[0].Recipe.Id.ShouldEqual(customEcsRecipeId);
            recommendations[0].Recipe.RecipePath.ShouldEqual(Path.Combine(saveDirectoryPathEcsProject, "ECS-CDK.recipe"));
        }

        [Fact]
        public async Task GenerateRecommendationsFromIncompatibleDeploymentProject()
        {
            // ARRANGE
            var tempDirectoryPath = new TestAppManager().GetProjectPath(string.Empty);
            var webAppWithDockerFilePath = Path.Combine(tempDirectoryPath, "testapps", "WebAppWithDockerFile");
            var blazorAppPath = Path.Combine(tempDirectoryPath, "testapps", "BlazorWasm50");
            var orchestrator = await GetOrchestrator(blazorAppPath);
            await _commandLineWrapper.Run("git init", tempDirectoryPath);

            var saveDirectoryPathEcsProject = Path.Combine(tempDirectoryPath, "ECS-CDK");
            await Utilities.CreateCDKDeploymentProject(webAppWithDockerFilePath, saveDirectoryPathEcsProject);

            // ACT
            var recommendations = await orchestrator.GenerateRecommendationsFromSavedDeploymentProject(saveDirectoryPathEcsProject);

            // ASSERT 
            recommendations.ShouldBeEmpty();
        }

        private async Task<Orchestrator> GetOrchestrator(string targetApplicationProjectPath)
        {
            var directoryManager = new DirectoryManager();
            var fileManager = new FileManager();
            var deploymentManifestEngine = new DeploymentManifestEngine(directoryManager, fileManager);
            var localUserSettingsEngine = new LocalUserSettingsEngine(fileManager, directoryManager);
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
                localUserSettingsEngine,
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
    }
}
