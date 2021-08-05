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
using System;
using AWS.Deploy.Common;

namespace AWS.Deploy.CLI.IntegrationTests.SaveCdkDeploymentProject
{
    [Collection("SaveCdkDeploymentProjectTests")]
    public class CustomRecipeLocatorTests : IDisposable
    {
        private readonly string _webAppWithDockerFilePath;
        private readonly string _webAppWithNoDockerFilePath;
        private readonly string _webAppWithDockerCsproj;
        private readonly string _webAppNoDockerCsproj;
        private readonly string _testArtifactsDirectoryPath;
        private readonly string _solutionDirectoryPath;
        private readonly ICustomRecipeLocator _customRecipeLocator;

        private bool _isDisposed;

        public CustomRecipeLocatorTests()
        {
            var testAppsDirectoryPath = Utilities.ResolvePathToTestApps();
            
            _webAppWithDockerFilePath = Path.Combine(testAppsDirectoryPath, "WebAppWithDockerFile");
            _webAppWithNoDockerFilePath = Path.Combine(testAppsDirectoryPath, "WebAppNoDockerFile");

            _webAppWithDockerCsproj = Path.Combine(_webAppWithDockerFilePath, "WebAppWithDockerFile.csproj");
            _webAppNoDockerCsproj = Path.Combine(_webAppWithNoDockerFilePath, "WebAppNoDockerFile.csproj");
            
            _testArtifactsDirectoryPath = Path.Combine(testAppsDirectoryPath, "TestArtifacts");

            var directoryManager = new DirectoryManager();
            var fileManager = new FileManager();
            var deploymentManifestEngine = new DeploymentManifestEngine(directoryManager, fileManager);
            var consoleInteractiveServiceImpl = new ConsoleInteractiveServiceImpl();
            var consoleOrchestratorLogger = new ConsoleOrchestratorLogger(consoleInteractiveServiceImpl);
            var commandLineWrapper = new CommandLineWrapper(consoleOrchestratorLogger);
            _customRecipeLocator = new CustomRecipeLocator(deploymentManifestEngine, consoleOrchestratorLogger, commandLineWrapper, directoryManager);

            var solutionPath = new ProjectDefinitionParser(fileManager, directoryManager).Parse(_webAppWithDockerFilePath).Result.ProjectSolutionPath;
            _solutionDirectoryPath = directoryManager.GetDirectoryInfo(solutionPath).Parent.FullName;
        }

        [Fact]
        public async Task LocateCustomRecipePathsWithManifestFile()
        {
            // ARRANGE - Create 2 CDK deployment projects that contain the custom recipe snapshot
            await Utilities.CreateCDKDeploymentProject(_webAppWithDockerFilePath, Path.Combine(_testArtifactsDirectoryPath, "MyCdkApp1"));
            await Utilities.CreateCDKDeploymentProject(_webAppWithDockerFilePath, Path.Combine(_testArtifactsDirectoryPath, "MyCdkApp2"));

            // ACT - Fetch custom recipes corresponding to the same target application that has a deployment-manifest file.
            var customRecipePaths = await _customRecipeLocator.LocateCustomRecipePaths(_webAppWithDockerCsproj, _solutionDirectoryPath);

            // ASSERT
            File.Exists(Path.Combine(_webAppWithDockerFilePath, "aws-deployments.json")).ShouldBeTrue();
            customRecipePaths.Count.ShouldEqual(2);
            customRecipePaths.ShouldContain(Path.Combine(_testArtifactsDirectoryPath, "MyCdkApp1"));
            customRecipePaths.ShouldContain(Path.Combine(_testArtifactsDirectoryPath, "MyCdkApp1"));

            CleanUp();
        }

        [Fact]
        public async Task LocateCustomRecipePathsWithoutManifestFile()
        {
            // ARRANGE - Create 2 CDK deployment projects that contain the custom recipe snapshot
            await Utilities.CreateCDKDeploymentProject(_webAppWithDockerFilePath, Path.Combine(_testArtifactsDirectoryPath, "MyCdkApp1"));
            await Utilities.CreateCDKDeploymentProject(_webAppWithDockerFilePath, Path.Combine(_testArtifactsDirectoryPath, "MyCdkApp2"));

            // ACT - Fetch custom recipes corresponding to a different target application (under source control) without a deployment-manifest file.
            var customRecipePaths = await _customRecipeLocator.LocateCustomRecipePaths(_webAppNoDockerCsproj, _solutionDirectoryPath);

            // ASSERT
            File.Exists(Path.Combine(_webAppWithNoDockerFilePath, "aws-deployments.json")).ShouldBeFalse();
            customRecipePaths.Count.ShouldEqual(2);
            customRecipePaths.ShouldContain(Path.Combine(_testArtifactsDirectoryPath, "MyCdkApp1"));
            customRecipePaths.ShouldContain(Path.Combine(_testArtifactsDirectoryPath, "MyCdkApp1"));

            CleanUp();
        }

        private void CleanUp()
        {
            if (Directory.Exists(_testArtifactsDirectoryPath))
                Directory.Delete(_testArtifactsDirectoryPath, true);

            if (File.Exists(Path.Combine(_webAppWithDockerFilePath, "aws-deployments.json")))
                File.Delete(Path.Combine(_webAppWithDockerFilePath, "aws-deployments.json"));

            if (File.Exists(Path.Combine(_webAppWithNoDockerFilePath, "aws-deployments.json")))
                File.Delete(Path.Combine(_webAppWithNoDockerFilePath, "aws-deployments.json"));
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

        ~CustomRecipeLocatorTests()
        {
            Dispose(false);
        }
    }
}
