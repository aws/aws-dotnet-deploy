// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.CLI.Utilities;
using AWS.Deploy.CLI.IntegrationTests.Utilities;
using AWS.Deploy.Common;
using AWS.Deploy.Common.DeploymentManifest;
using AWS.Deploy.Common.IO;
using AWS.Deploy.DockerEngine;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.CDK;
using Moq;
using Should;
using AWS.Deploy.Common.Recipes;
using Newtonsoft.Json;
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.CLI.IntegrationTests.Services;
using AWS.Deploy.Orchestration.LocalUserSettings;
using AWS.Deploy.Orchestration.Utilities;
using AWS.Deploy.Orchestration.ServiceHandlers;
using AWS.Deploy.Common.Recipes.Validation;
using NUnit.Framework;

namespace AWS.Deploy.CLI.IntegrationTests.SaveCdkDeploymentProject
{
    [TestFixture]
    public class RecommendationTests
    {
        private CommandLineWrapper _commandLineWrapper;
        private InMemoryInteractiveService _inMemoryInteractiveService;

        [SetUp]
        public void Initialize()
        {
            _inMemoryInteractiveService  = new InMemoryInteractiveService();
            _commandLineWrapper = new CommandLineWrapper(_inMemoryInteractiveService);
        }

        [Test]
        public async Task GenerateRecommendationsForDeploymentProject()
        {
            // ARRANGE
            var tempDirectoryPath = new TestAppManager().GetProjectPath(string.Empty);
            var webAppWithDockerFilePath = Path.Combine(tempDirectoryPath, "testapps", "WebAppWithDockerFile");
            var orchestrator = await GetOrchestrator(webAppWithDockerFilePath);
            await _commandLineWrapper.Run("git init", tempDirectoryPath);

            // ACT
            var recommendations = await orchestrator.GenerateRecommendationsToSaveDeploymentProject();

            // ASSERT
            var anyNonCdkRecommendations = recommendations.Where(x => x.Recipe.DeploymentType != DeploymentTypes.CdkProject);
            Assert.False(anyNonCdkRecommendations.Any());

            Assert.NotNull(recommendations.FirstOrDefault(x => x.Recipe.Id == "AspNetAppEcsFargate"));
            Assert.NotNull(recommendations.FirstOrDefault(x => x.Recipe.Id == "AspNetAppElasticBeanstalkLinux"));
        }

        [Test]
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
            recommendations.Count.ShouldEqual(7);
            recommendations[0].Name.ShouldEqual("ASP.NET Core App to Amazon ECS using AWS Fargate"); // default recipe
            recommendations[1].Name.ShouldEqual("ASP.NET Core App to AWS App Runner"); // default recipe
            recommendations[2].Name.ShouldEqual("ASP.NET Core App to AWS Elastic Beanstalk on Linux"); // default recipe
            recommendations[3].Name.ShouldEqual("ASP.NET Core App to AWS Elastic Beanstalk on Windows"); // default recipe
            recommendations[4].Name.ShouldEqual("ASP.NET Core App to Existing AWS Elastic Beanstalk Environment"); // default recipe
            recommendations[5].Name.ShouldEqual("ASP.NET Core App to Existing AWS Elastic Beanstalk Windows Environment"); // default recipe
            recommendations[6].Name.ShouldEqual("Container Image to Amazon Elastic Container Registry (ECR)"); // default recipe
        }

        [Test]
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
            recommendations.Count.ShouldEqual(9);
            recommendations[0].Name.ShouldEqual(customEcsRecipeName); // custom recipe
            recommendations[1].Name.ShouldEqual(customEbsRecipeName); // custom recipe
            recommendations[2].Name.ShouldEqual("ASP.NET Core App to Amazon ECS using AWS Fargate"); // default recipe
            recommendations[3].Name.ShouldEqual("ASP.NET Core App to AWS App Runner"); // default recipe
            recommendations[4].Name.ShouldEqual("ASP.NET Core App to AWS Elastic Beanstalk on Linux"); // default recipe
            recommendations[5].Name.ShouldEqual("ASP.NET Core App to AWS Elastic Beanstalk on Windows"); // default recipe
            recommendations[6].Name.ShouldEqual("ASP.NET Core App to Existing AWS Elastic Beanstalk Environment"); // default recipe
            recommendations[7].Name.ShouldEqual("ASP.NET Core App to Existing AWS Elastic Beanstalk Windows Environment"); // default recipe
            recommendations[8].Name.ShouldEqual("Container Image to Amazon Elastic Container Registry (ECR)"); // default recipe

            // ASSERT - Recipe paths
            recommendations[0].Recipe.RecipePath.ShouldEqual(Path.Combine(saveDirectoryPathEcsProject, "ECS-CDK.recipe"));
            recommendations[1].Recipe.RecipePath.ShouldEqual(Path.Combine(saveDirectoryPathEbsProject, "EBS-CDK.recipe"));

            // ASSERT - custom recipe IDs
            recommendations[0].Recipe.Id.ShouldEqual(customEcsRecipeId);
            recommendations[1].Recipe.Id.ShouldEqual(customEbsRecipeId);

            File.Exists(Path.Combine(webAppWithDockerFilePath, "aws-deployments.json")).ShouldBeTrue();
        }

        [Test]
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
            recommendations.Count.ShouldEqual(8);
            recommendations[0].Name.ShouldEqual(customEbsRecipeName);
            recommendations[1].Name.ShouldEqual(customEcsRecipeName);
            recommendations[2].Name.ShouldEqual("ASP.NET Core App to AWS Elastic Beanstalk on Linux");
            recommendations[3].Name.ShouldEqual("ASP.NET Core App to AWS Elastic Beanstalk on Windows");
            recommendations[4].Name.ShouldEqual("ASP.NET Core App to Amazon ECS using AWS Fargate");
            recommendations[5].Name.ShouldEqual("ASP.NET Core App to AWS App Runner");
            recommendations[6].Name.ShouldEqual("ASP.NET Core App to Existing AWS Elastic Beanstalk Environment");
            recommendations[7].Name.ShouldEqual("ASP.NET Core App to Existing AWS Elastic Beanstalk Windows Environment");

            // ASSERT - Recipe paths
            recommendations[0].Recipe.RecipePath.ShouldEqual(Path.Combine(saveDirectoryPathEbsProject, "EBS-CDK.recipe"));
            recommendations[1].Recipe.RecipePath.ShouldEqual(Path.Combine(saveDirectoryPathEcsProject, "ECS-CDK.recipe"));

            // ASSERT - Custom recipe IDs
            recommendations[0].Recipe.Id.ShouldEqual(customEbsRecipeId);
            recommendations[1].Recipe.Id.ShouldEqual(customEcsRecipeId);
            File.Exists(Path.Combine(webAppNoDockerFilePath, "aws-deployments.json")).ShouldBeFalse();
        }

        [Test]
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

        [Test]
        public async Task GenerateRecommendationsFromIncompatibleDeploymentProject()
        {
            // ARRANGE
            var tempDirectoryPath = new TestAppManager().GetProjectPath(string.Empty);
            var webAppWithDockerFilePath = Path.Combine(tempDirectoryPath, "testapps", "WebAppWithDockerFile");
            var blazorAppPath = Path.Combine(tempDirectoryPath, "testapps", "BlazorWasm60");
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
            var awsResourceQueryer = new TestToolAWSResourceQueryer();
            var directoryManager = new DirectoryManager();
            var fileManager = new FileManager();
            var deploymentManifestEngine = new DeploymentManifestEngine(directoryManager, fileManager);
            var environmentVariableManager = new Mock<IEnvironmentVariableManager>().Object;
            var deployToolWorkspaceMetadata = new DeployToolWorkspaceMetadata(directoryManager, environmentVariableManager, fileManager);
            var localUserSettingsEngine = new LocalUserSettingsEngine(fileManager, directoryManager, deployToolWorkspaceMetadata);
            var serviceProvider = new Mock<IServiceProvider>();
            var validatorFactory = new ValidatorFactory(serviceProvider.Object);
            var optionSettingHandler = new OptionSettingHandler(validatorFactory);
            var recipeHandler = new RecipeHandler(deploymentManifestEngine, _inMemoryInteractiveService, directoryManager, fileManager, optionSettingHandler, validatorFactory);
            var projectDefinition = await new ProjectDefinitionParser(fileManager, directoryManager).Parse(targetApplicationProjectPath);
            var session = new OrchestratorSession(projectDefinition);

            return new Orchestrator(session,
                _inMemoryInteractiveService,
                new Mock<ICdkProjectHandler>().Object,
                new Mock<ICDKManager>().Object,
                new Mock<ICDKVersionDetector>().Object,
                awsResourceQueryer,
                new Mock<IDeploymentBundleHandler>().Object,
                localUserSettingsEngine,
                new Mock<IDockerEngine>().Object,
                recipeHandler,
                fileManager,
                directoryManager,
                new Mock<IAWSServiceHandler>().Object,
                new OptionSettingHandler(new Mock<IValidatorFactory>().Object),
                deployToolWorkspaceMetadata);
        }

        private async Task<string> GetCustomRecipeId(string recipeFilePath)
        {
            var recipeBody = await File.ReadAllTextAsync(recipeFilePath);
            var recipe = JsonConvert.DeserializeObject<RecipeDefinition>(recipeBody);
            return recipe.Id;
        }

        [TearDown]
        public void Cleanup()
        {
            _inMemoryInteractiveService.ReadStdOutStartToEnd();
        }
    }
}
