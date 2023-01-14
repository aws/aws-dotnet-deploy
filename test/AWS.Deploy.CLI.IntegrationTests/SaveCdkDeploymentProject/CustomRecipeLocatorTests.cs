// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using AWS.Deploy.CLI.Utilities;
using AWS.Deploy.Common.DeploymentManifest;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Orchestration;
using Task = System.Threading.Tasks.Task;
using Should;
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.CLI.IntegrationTests.Services;
using AWS.Deploy.Common.Recipes;
using Moq;
using AWS.Deploy.Common.Recipes.Validation;
using NUnit.Framework;

namespace AWS.Deploy.CLI.IntegrationTests.SaveCdkDeploymentProject
{
    [TestFixture]
    public class CustomRecipeLocatorTests
    {
        private CommandLineWrapper _commandLineWrapper;
        private InMemoryInteractiveService _inMemoryInteractiveService;

        [SetUp]
        public void Initialize()
        {
            _inMemoryInteractiveService = new InMemoryInteractiveService();
            _commandLineWrapper = new CommandLineWrapper(_inMemoryInteractiveService);        }

        [Test]
        public async Task LocateCustomRecipePathsWithManifestFile()
        {
            var tempDirectoryPath = new TestAppManager().GetProjectPath(string.Empty);
            var webAppWithDockerFilePath = Path.Combine(tempDirectoryPath, "testapps", "WebAppWithDockerFile");
            var webAppWithDockerCsproj = Path.Combine(webAppWithDockerFilePath, "WebAppWithDockerFile.csproj");
            var solutionDirectoryPath = tempDirectoryPath;
            var recipeHandler = BuildRecipeHandler();
            await _commandLineWrapper.Run("git init", tempDirectoryPath);

            // ARRANGE - Create 2 CDK deployment projects that contain the custom recipe snapshot
            await Utilities.CreateCDKDeploymentProject(webAppWithDockerFilePath, Path.Combine(tempDirectoryPath, "MyCdkApp1"));
            await Utilities.CreateCDKDeploymentProject(webAppWithDockerFilePath, Path.Combine(tempDirectoryPath, "MyCdkApp2"));

            // ACT - Fetch custom recipes corresponding to the same target application that has a deployment-manifest file.
            var customRecipePaths = await recipeHandler.LocateCustomRecipePaths(webAppWithDockerCsproj, solutionDirectoryPath);

            // ASSERT
            File.Exists(Path.Combine(webAppWithDockerFilePath, "aws-deployments.json")).ShouldBeTrue();
            customRecipePaths.Count.ShouldEqual(2, $"Custom recipes found: {Environment.NewLine} {string.Join(Environment.NewLine, customRecipePaths)}");
            customRecipePaths.ShouldContain(Path.Combine(tempDirectoryPath, "MyCdkApp1"));
            customRecipePaths.ShouldContain(Path.Combine(tempDirectoryPath, "MyCdkApp1"));
        }

        [Test]
        public async Task LocateCustomRecipePathsWithoutManifestFile()
        {
            var tempDirectoryPath = new TestAppManager().GetProjectPath(string.Empty);
            var webAppWithDockerFilePath = Path.Combine(tempDirectoryPath, "testapps", "WebAppWithDockerFile");
            var webAppNoDockerFilePath = Path.Combine(tempDirectoryPath, "testapps", "WebAppNoDockerFile");
            var webAppWithDockerCsproj = Path.Combine(webAppWithDockerFilePath, "WebAppWithDockerFile.csproj");
            var webAppNoDockerCsproj = Path.Combine(webAppNoDockerFilePath, "WebAppNoDockerFile.csproj");
            var solutionDirectoryPath = tempDirectoryPath;
            var recipeHandler = BuildRecipeHandler();
            await _commandLineWrapper.Run("git init", tempDirectoryPath);

            // ARRANGE - Create 2 CDK deployment projects that contain the custom recipe snapshot
            await Utilities.CreateCDKDeploymentProject(webAppWithDockerFilePath, Path.Combine(tempDirectoryPath, "MyCdkApp1"));
            await Utilities.CreateCDKDeploymentProject(webAppWithDockerFilePath, Path.Combine(tempDirectoryPath, "MyCdkApp2"));

            // ACT - Fetch custom recipes corresponding to a different target application (under source control) without a deployment-manifest file.
            var customRecipePaths = await recipeHandler.LocateCustomRecipePaths(webAppNoDockerCsproj, solutionDirectoryPath);

            // ASSERT
            File.Exists(Path.Combine(webAppNoDockerFilePath, "aws-deployments.json")).ShouldBeFalse();
            customRecipePaths.Count.ShouldEqual(2, $"Custom recipes found: {Environment.NewLine} {string.Join(Environment.NewLine, customRecipePaths)}");
            customRecipePaths.ShouldContain(Path.Combine(tempDirectoryPath, "MyCdkApp1"));
            customRecipePaths.ShouldContain(Path.Combine(tempDirectoryPath, "MyCdkApp1"));
        }

        private IRecipeHandler BuildRecipeHandler()
        {
            var directoryManager = new DirectoryManager();
            var fileManager = new FileManager();
            var deploymentManifestEngine = new DeploymentManifestEngine(directoryManager, fileManager);
            var serviceProvider = new Mock<IServiceProvider>();
            var validatorFactory = new ValidatorFactory(serviceProvider.Object);
            var optionSettingHandler = new OptionSettingHandler(validatorFactory);
            return new RecipeHandler(deploymentManifestEngine, _inMemoryInteractiveService, directoryManager, fileManager, optionSettingHandler, validatorFactory);
        }

        [TearDown]
        public void Cleanup()
        {
            _inMemoryInteractiveService.ReadStdOutStartToEnd();
        }
    }
}
