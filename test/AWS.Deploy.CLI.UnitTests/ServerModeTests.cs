// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.CLI.Commands;
using AWS.Deploy.CLI.ServerMode.Controllers;
using AWS.Deploy.CLI.ServerMode.Models;
using AWS.Deploy.Orchestration;
using Microsoft.AspNetCore.Mvc;
using Xunit;

using Moq;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.DeploymentManifest;
using AWS.Deploy.Orchestration.LocalUserSettings;
using AWS.Deploy.CLI.Utilities;
using AWS.Deploy.Common;
using AWS.Deploy.CLI.UnitTests.Utilities;

using System.Collections.Generic;
using System.IO;
using AWS.Deploy.Common.Recipes;
using DeploymentTypes = AWS.Deploy.CLI.ServerMode.Models.DeploymentTypes;
using System;
using AWS.Deploy.Common.Recipes.Validation;
using AWS.Deploy.Recipes;

namespace AWS.Deploy.CLI.UnitTests
{
    public class ServerModeTests
    {
        /// <summary>
        /// This test is to make sure the CLI is using at least version 5.0.0 of System.Text.Json. If we use verisons less then
        /// 5.0.0 types that do not have a parameterless constructor will fail to deserialize. This affects the server mode model types.
        /// The unit and integ test project force a newer version of System.Text.Json then would be used by default in the CLI masking the issue.
        /// </summary>
        [Fact]
        public void EnsureMinimumSystemTextJsonVersion()
        {
            var assembly = typeof(ServerModeCommand).Assembly;
            var jsonAssemblyName = assembly.GetReferencedAssemblies().FirstOrDefault(x => string.Equals("System.Text.Json", x.Name, StringComparison.OrdinalIgnoreCase));
            Assert.True(jsonAssemblyName.Version >= Version.Parse("5.0.0"));
        }


        [Fact]
        public async Task TcpPortIsInUseTest()
        {
            var serverModeCommand1 = new ServerModeCommand(new TestToolInteractiveServiceImpl(), 1234, null, true);
            var serverModeCommand2 = new ServerModeCommand(new TestToolInteractiveServiceImpl(), 1234, null, true);

            var serverModeTask1 = serverModeCommand1.ExecuteAsync();
            var serverModeTask2 = serverModeCommand2.ExecuteAsync();

            await Task.WhenAny(serverModeTask1, serverModeTask2);

            Assert.False(serverModeTask1.IsCompleted);

            Assert.True(serverModeTask2.IsCompleted);
            Assert.True(serverModeTask2.IsFaulted);

            Assert.NotNull(serverModeTask2.Exception);
            Assert.Single(serverModeTask2.Exception.InnerExceptions);

            Assert.IsType<TcpPortInUseException>(serverModeTask2.Exception.InnerException);
        }

        [Theory]
        [InlineData("")]
        [InlineData("InvalidId")]
        public async Task RecipeController_GetRecipe_EmptyId(string recipeId)
        {
            var directoryManager = new DirectoryManager();
            var fileManager = new FileManager();
            var deploymentManifestEngine = new DeploymentManifestEngine(directoryManager, fileManager);
            var consoleInteractiveServiceImpl = new ConsoleInteractiveServiceImpl();
            var consoleOrchestratorLogger = new ConsoleOrchestratorLogger(consoleInteractiveServiceImpl);
            var serviceProvider = new Mock<IServiceProvider>();
            var validatorFactory = new ValidatorFactory(serviceProvider.Object);
            var optionSettingHandler = new OptionSettingHandler(validatorFactory);
            var recipeHandler = new RecipeHandler(deploymentManifestEngine, consoleOrchestratorLogger, directoryManager, fileManager, optionSettingHandler, validatorFactory);
            var projectDefinitionParser = new ProjectDefinitionParser(fileManager, directoryManager);

            var recipeController = new RecipeController(recipeHandler, projectDefinitionParser);
            var response = await recipeController.GetRecipe(recipeId);

            Assert.IsType<BadRequestObjectResult>(response);
        }

        [Fact]
        public async Task RecipeController_GetRecipe_HappyPath()
        {
            var directoryManager = new DirectoryManager();
            var fileManager = new FileManager();
            var deploymentManifestEngine = new DeploymentManifestEngine(directoryManager, fileManager);
            var consoleInteractiveServiceImpl = new ConsoleInteractiveServiceImpl();
            var consoleOrchestratorLogger = new ConsoleOrchestratorLogger(consoleInteractiveServiceImpl);
            var projectDefinitionParser = new ProjectDefinitionParser(fileManager, directoryManager);
            var serviceProvider = new Mock<IServiceProvider>();
            var validatorFactory = new ValidatorFactory(serviceProvider.Object);
            var optionSettingHandler = new OptionSettingHandler(validatorFactory);
            var recipeHandler = new RecipeHandler(deploymentManifestEngine, consoleOrchestratorLogger, directoryManager, fileManager, optionSettingHandler, validatorFactory);

            var recipeController = new RecipeController(recipeHandler, projectDefinitionParser);
            var recipeDefinitions = await recipeHandler.GetRecipeDefinitions(null);
            var recipe = recipeDefinitions.First();

            var response = await recipeController.GetRecipe(recipe.Id);

            var result = Assert.IsType<OkObjectResult>(response);
            var resultRecipe = Assert.IsType<RecipeSummary>(result.Value);
            Assert.Equal(recipe.Id, resultRecipe.Id);
        }

        [Fact]
        public async Task RecipeController_GetRecipe_WithProjectPath()
        {
            var directoryManager = new DirectoryManager();
            var fileManager = new FileManager();
            var projectDefinitionParser = new ProjectDefinitionParser(fileManager, directoryManager);

            var deploymentManifestEngine = new Mock<IDeploymentManifestEngine>();
            var serviceProvider = new Mock<IServiceProvider>();
            var validatorFactory = new ValidatorFactory(serviceProvider.Object);
            var optionSettingHandler = new OptionSettingHandler(validatorFactory);

            var customLocatorCalls = 0;
            var sourceProjectDirectory = SystemIOUtilities.ResolvePath("WebAppWithDockerFile");
            deploymentManifestEngine
                .Setup(x => x.GetRecipeDefinitionPaths(It.IsAny<string>()))
                .Callback<string>((csProjectPath) =>
                {
                    customLocatorCalls++;
                    Assert.Equal(new DirectoryInfo(sourceProjectDirectory).FullName, Directory.GetParent(csProjectPath).FullName);
                })
                .ReturnsAsync(new List<string>());
            var orchestratorInteractiveService = new TestToolOrchestratorInteractiveService();
            var recipeHandler = new RecipeHandler(deploymentManifestEngine.Object, orchestratorInteractiveService, directoryManager, fileManager, optionSettingHandler, validatorFactory);


            var projectDefinition = await projectDefinitionParser.Parse(sourceProjectDirectory);
            var recipePaths = new HashSet<string> { RecipeLocator.FindRecipeDefinitionsPath() };
            var customRecipePaths = await recipeHandler.LocateCustomRecipePaths(projectDefinition);
            var recipeDefinitions = await recipeHandler.GetRecipeDefinitions(recipeDefinitionPaths: recipePaths.Union(customRecipePaths).ToList());
            var recipe = recipeDefinitions.First();
            Assert.NotEqual(0, customLocatorCalls);

            customLocatorCalls = 0;
            var recipeController = new RecipeController(recipeHandler, projectDefinitionParser);
            var response = await recipeController.GetRecipe(recipe.Id, sourceProjectDirectory);
            Assert.NotEqual(0, customLocatorCalls);

            var result = Assert.IsType<OkObjectResult>(response);
            var resultRecipe = Assert.IsType<RecipeSummary>(result.Value);
            Assert.Equal(recipe.Id, resultRecipe.Id);
        }

        [Theory]
        [InlineData(CloudApplicationResourceType.CloudFormationStack, DeploymentTypes.CloudFormationStack)]
        [InlineData(CloudApplicationResourceType.BeanstalkEnvironment, DeploymentTypes.BeanstalkEnvironment)]
        public void ExistingDeploymentSummary_ContainsCorrectDeploymentType(CloudApplicationResourceType resourceType, DeploymentTypes expectedDeploymentType)
        {
            var existingDeploymentSummary = new ExistingDeploymentSummary(
                "name",
                "baseRecipeId",
                "recipeId",
                "recipeName",
                new List<CategorySummary>(),
                false,
                "shortDescription",
                "description",
                "targetService",
                System.DateTime.Now,
                true,
                resourceType,
                "uniqueId");

            Assert.Equal(expectedDeploymentType, existingDeploymentSummary.DeploymentType);
        }

        [Theory]
        [InlineData(Deploy.Common.Recipes.DeploymentTypes.CdkProject, DeploymentTypes.CloudFormationStack)]
        [InlineData(Deploy.Common.Recipes.DeploymentTypes.BeanstalkEnvironment, DeploymentTypes.BeanstalkEnvironment)]
        public void RecommendationSummary_ContainsCorrectDeploymentType(Deploy.Common.Recipes.DeploymentTypes deploymentType, DeploymentTypes expectedDeploymentType)
        {
            var recommendationSummary = new RecommendationSummary(
                "baseRecipeId",
                "recipeId",
                "name",
                new List<CategorySummary>(),
                false,
                "shortDescription",
                "description",
                "targetService",
                deploymentType);

            Assert.Equal(expectedDeploymentType, recommendationSummary.DeploymentType);
        }
    }
}
