// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.CLI.Utilities;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace AWS.Deploy.CLI.IntegrationTests.SaveCdkDeploymentProject
{
    public class SaveCdkDeploymentProjectTests 
    {
        private readonly CommandLineWrapper _commandLineWrapper;

        public SaveCdkDeploymentProjectTests()
        {
            _commandLineWrapper = new CommandLineWrapper(new ConsoleOrchestratorLogger(new ConsoleInteractiveServiceImpl()));
        }

        [Fact]
        public async Task DefaultSaveDirectory()
        {
            var tempDirectoryPath = new TestAppManager().GetProjectPath(string.Empty);
            await _commandLineWrapper.Run("git init", tempDirectoryPath);
            var targetApplicationProjectPath = Path.Combine(tempDirectoryPath, "testapps", "WebAppWithDockerFile");

            await Utilities.CreateCDKDeploymentProject(targetApplicationProjectPath);
        }

        [Fact]
        public async Task CustomSaveDirectory()
        {
            var tempDirectoryPath = new TestAppManager().GetProjectPath(string.Empty);
            await _commandLineWrapper.Run("git init", tempDirectoryPath);
            var targetApplicationProjectPath = Path.Combine(tempDirectoryPath, "testapps", "WebAppWithDockerFile");

            var saveDirectoryPath = Path.Combine(tempDirectoryPath, "DeploymentProjects", "MyCdkApp");
            await Utilities.CreateCDKDeploymentProject(targetApplicationProjectPath, saveDirectoryPath);
        }

        [Fact]
        public async Task InvalidSaveCdkDirectoryInsideProjectDirectory()
        {
            var tempDirectoryPath = new TestAppManager().GetProjectPath(string.Empty);
            await _commandLineWrapper.Run("git init", tempDirectoryPath);
            var targetApplicationProjectPath = Path.Combine(tempDirectoryPath, "testapps", "WebAppWithDockerFile");

            var saveDirectoryPath = Path.Combine(tempDirectoryPath, "testapps", "WebAppWithDockerFile", "MyCdkApp");
            await Utilities.CreateCDKDeploymentProject(targetApplicationProjectPath, saveDirectoryPath, false);
        }

        [Fact]
        public async Task InvalidNonEmptySaveCdkDirectory()
        {
            var tempDirectoryPath = new TestAppManager().GetProjectPath(string.Empty);
            await _commandLineWrapper.Run("git init", tempDirectoryPath);
            var targetApplicationProjectPath = Path.Combine(tempDirectoryPath, "testapps", "WebAppWithDockerFile");

            Directory.CreateDirectory(Path.Combine(tempDirectoryPath, "MyCdkApp", "MyFolder"));
            var saveDirectoryPath = Path.Combine(tempDirectoryPath, "MyCdkApp");
            await Utilities.CreateCDKDeploymentProject(targetApplicationProjectPath, saveDirectoryPath, false);
        }
    }
}
