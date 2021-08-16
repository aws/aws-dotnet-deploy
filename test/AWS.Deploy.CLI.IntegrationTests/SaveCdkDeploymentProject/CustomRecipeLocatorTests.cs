// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using AWS.Deploy.CLI.Utilities;
using AWS.Deploy.Common.DeploymentManifest;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Orchestration;
using Xunit;
using Task = System.Threading.Tasks.Task;
using Should;
using AWS.Deploy.CLI.Common.UnitTests.IO;

namespace AWS.Deploy.CLI.IntegrationTests.SaveCdkDeploymentProject
{
    public class CustomRecipeLocatorTests
    {
        private readonly CommandLineWrapper _commandLineWrapper;

        public CustomRecipeLocatorTests()
        {
            _commandLineWrapper = new CommandLineWrapper(new ConsoleOrchestratorLogger(new ConsoleInteractiveServiceImpl()));
        }

        [Fact]
        public async Task LocateCustomRecipePathsWithManifestFile()
        {
            var tempDirectoryPath = new TestAppManager().GetProjectPath(string.Empty);
            var webAppWithDockerFilePath = Path.Combine(tempDirectoryPath, "testapps", "WebAppWithDockerFile");
            var webAppWithDockerCsproj = Path.Combine(webAppWithDockerFilePath, "WebAppWithDockerFile.csproj");
            var solutionDirectoryPath = tempDirectoryPath;
            var customRecipeLocator = BuildCustomRecipeLocator();
            await _commandLineWrapper.Run("git init", tempDirectoryPath);

            // ARRANGE - Create 2 CDK deployment projects that contain the custom recipe snapshot
            await Utilities.CreateCDKDeploymentProject(webAppWithDockerFilePath, Path.Combine(tempDirectoryPath, "MyCdkApp1"));
            await Utilities.CreateCDKDeploymentProject(webAppWithDockerFilePath, Path.Combine(tempDirectoryPath, "MyCdkApp2"));

            // ACT - Fetch custom recipes corresponding to the same target application that has a deployment-manifest file.
            var customRecipePaths = await customRecipeLocator.LocateCustomRecipePaths(webAppWithDockerCsproj, solutionDirectoryPath);

            // ASSERT
            File.Exists(Path.Combine(webAppWithDockerFilePath, "aws-deployments.json")).ShouldBeTrue();
            customRecipePaths.Count.ShouldEqual(2);
            customRecipePaths.ShouldContain(Path.Combine(tempDirectoryPath, "MyCdkApp1"));
            customRecipePaths.ShouldContain(Path.Combine(tempDirectoryPath, "MyCdkApp1"));
        }

        [Fact]
        public async Task LocateCustomRecipePathsWithoutManifestFile()
        {
            var tempDirectoryPath = new TestAppManager().GetProjectPath(string.Empty);
            var webAppWithDockerFilePath = Path.Combine(tempDirectoryPath, "testapps", "WebAppWithDockerFile");
            var webAppNoDockerFilePath = Path.Combine(tempDirectoryPath, "testapps", "WebAppNoDockerFile");
            var webAppWithDockerCsproj = Path.Combine(webAppWithDockerFilePath, "WebAppWithDockerFile.csproj");
            var webAppNoDockerCsproj = Path.Combine(webAppNoDockerFilePath, "WebAppNoDockerFile.csproj");
            var solutionDirectoryPath = tempDirectoryPath;
            var customRecipeLocator = BuildCustomRecipeLocator();
            await _commandLineWrapper.Run("git init", tempDirectoryPath);

            // ARRANGE - Create 2 CDK deployment projects that contain the custom recipe snapshot
            await Utilities.CreateCDKDeploymentProject(webAppWithDockerFilePath, Path.Combine(tempDirectoryPath, "MyCdkApp1"));
            await Utilities.CreateCDKDeploymentProject(webAppWithDockerFilePath, Path.Combine(tempDirectoryPath, "MyCdkApp2"));

            // ACT - Fetch custom recipes corresponding to a different target application (under source control) without a deployment-manifest file.
            var customRecipePaths = await customRecipeLocator.LocateCustomRecipePaths(webAppNoDockerCsproj, solutionDirectoryPath);

            // ASSERT
            File.Exists(Path.Combine(webAppNoDockerFilePath, "aws-deployments.json")).ShouldBeFalse();
            customRecipePaths.Count.ShouldEqual(2);
            customRecipePaths.ShouldContain(Path.Combine(tempDirectoryPath, "MyCdkApp1"));
            customRecipePaths.ShouldContain(Path.Combine(tempDirectoryPath, "MyCdkApp1"));
        }

        private ICustomRecipeLocator BuildCustomRecipeLocator()
        {
            var directoryManager = new DirectoryManager();
            var fileManager = new FileManager();
            var deploymentManifestEngine = new DeploymentManifestEngine(directoryManager, fileManager);
            var consoleInteractiveServiceImpl = new ConsoleInteractiveServiceImpl();
            var consoleOrchestratorLogger = new ConsoleOrchestratorLogger(consoleInteractiveServiceImpl);
            var commandLineWrapper = new CommandLineWrapper(consoleOrchestratorLogger);
            return new CustomRecipeLocator(deploymentManifestEngine, consoleOrchestratorLogger, commandLineWrapper, directoryManager);
        }
    }
}
