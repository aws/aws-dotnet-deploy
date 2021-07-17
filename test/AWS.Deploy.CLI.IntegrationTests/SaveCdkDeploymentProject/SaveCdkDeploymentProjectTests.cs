// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using AWS.Deploy.CLI.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace AWS.Deploy.CLI.IntegrationTests.SaveCdkDeploymentProject
{
    public class SaveCdkDeploymentProjectTests : IDisposable
    {
        private readonly App _app;
        private readonly InMemoryInteractiveService _interactiveService;
        private readonly string _targetApplicationProjectPath;
        private readonly string _deploymentManifestFilePath;

        private bool _isDisposed;
        private string _saveDirectoryPath;

        public SaveCdkDeploymentProjectTests()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddCustomServices();
            serviceCollection.AddTestServices();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            _app = serviceProvider.GetService<App>();
            Assert.NotNull(_app);

            _interactiveService = serviceProvider.GetService<InMemoryInteractiveService>();
            Assert.NotNull(_interactiveService);

            _targetApplicationProjectPath = Path.Combine("testapps", "WebAppWithDockerFile", "WebAppWithDockerFile.csproj");
            _deploymentManifestFilePath = Path.Combine("testapps", "WebAppWithDockerFile", "aws-deployments.json");
        }

        [Fact]
        public async Task DefaultSaveDirectory()
        {
            // Arrange input for saving the CDK deployment project
            await _interactiveService.StdInWriter.WriteAsync(Environment.NewLine); // Select default recommendation

            // Generate and save the CDK deployment project
            var deployArgs = new[] { "deployment-project", "generate", "--project-path", _targetApplicationProjectPath};
            await _app.Run(deployArgs);

            // Verify project is saved
            _saveDirectoryPath = Path.Combine("testapps", "WebAppWithDockerFileCDK");
            var directoryInfo = new DirectoryInfo(_saveDirectoryPath);
            var stdOut = _interactiveService.StdOutReader.ReadAllLines();
            var successMessage = $"The CDK deployment project is saved at: {directoryInfo.FullName}";

            Assert.Contains(stdOut, (line) => successMessage.Equals(line));
            Assert.True(Directory.Exists(_saveDirectoryPath));
            Assert.True(File.Exists(Path.Combine(_saveDirectoryPath, "WebAppWithDockerFileCDK.csproj")));
            Assert.True(File.Exists(Path.Combine(_saveDirectoryPath, "AppStack.cs")));
            Assert.True(File.Exists(Path.Combine(_saveDirectoryPath, "Program.cs")));
            Assert.True(File.Exists(Path.Combine(_saveDirectoryPath, "cdk.json")));
            Assert.True(Directory.EnumerateFiles(_saveDirectoryPath, "*.recipe").Any());

            // Verify deployment-manifest file is generated
            Assert.True(File.Exists(_deploymentManifestFilePath));

            // Delete generated artifacts
            Directory.Delete(_saveDirectoryPath, true);
            File.Delete(_deploymentManifestFilePath);

            // Verify artifacts are deleted
            Assert.False(Directory.Exists(_saveDirectoryPath));
            Assert.False(File.Exists(_deploymentManifestFilePath));
        }

        [Fact]
        public async Task CustomSaveDirectory()
        {
            // Arrange input for saving the CDK deployment project
            await _interactiveService.StdInWriter.WriteAsync(Environment.NewLine); // Select default recommendation

            // Generate and save the CDK deployment project
            _saveDirectoryPath = Path.Combine("DeploymentProjects", "MyCdkApp");
            var deployArgs = new[] { "deployment-project", "generate", "--project-path", _targetApplicationProjectPath, "--output", _saveDirectoryPath };
            var returnCode = await _app.Run(deployArgs);

            // Verify project is saved
            var directoryInfo = new DirectoryInfo(_saveDirectoryPath);
            var stdOut = _interactiveService.StdOutReader.ReadAllLines();
            var successMessage = $"The CDK deployment project is saved at: {directoryInfo.FullName}";

            Assert.Equal(CommandReturnCodes.SUCCESS, returnCode);
            Assert.Contains(stdOut, (line) => successMessage.Equals(line));
            Assert.True(Directory.Exists(_saveDirectoryPath));
            Assert.True(File.Exists(Path.Combine(_saveDirectoryPath, "MyCdkApp.csproj")));
            Assert.True(File.Exists(Path.Combine(_saveDirectoryPath, "AppStack.cs")));
            Assert.True(File.Exists(Path.Combine(_saveDirectoryPath, "Program.cs")));
            Assert.True(File.Exists(Path.Combine(_saveDirectoryPath, "cdk.json")));
            Assert.True(Directory.EnumerateFiles(_saveDirectoryPath, "*.recipe").Any());

            // Verify deployment-manifest file is generated
            Assert.True(File.Exists(_deploymentManifestFilePath));

            // Delete generated artifacts
            Directory.Delete(_saveDirectoryPath, true);
            File.Delete(_deploymentManifestFilePath);

            // Verify artifacts are deleted
            Assert.False(Directory.Exists(_saveDirectoryPath));
            Assert.False(File.Exists(_deploymentManifestFilePath));
        }

        [Fact]
        public async Task InvalidSaveCdkDirectoryInsideProjectDirectory()
        {
            // Arrange input for saving the CDK deployment project
            await _interactiveService.StdInWriter.WriteAsync(Environment.NewLine); // Select default recommendation

            // Generate and save the CDK deployment project
            _saveDirectoryPath = Path.Combine("testapps", "WebAppWithDockerFile", "MyCdkApp");
            var deployArgs = new[] { "deployment-project", "generate", "--project-path", _targetApplicationProjectPath, "--output", _saveDirectoryPath };
            var returnCode = await _app.Run(deployArgs);

            Assert.Equal(CommandReturnCodes.USER_ERROR, returnCode);
            Assert.False(Directory.Exists(_saveDirectoryPath));
        }

        [Fact]
        public async Task InvalidNonEmptySaveCdkDirectory()
        {
            // Arrange input for saving the CDK deployment project
            await _interactiveService.StdInWriter.WriteAsync(Environment.NewLine); // Select default recommendation

            // create a non-empty directory inside which we intend to save the CDK deployment project.
            Directory.CreateDirectory(Path.Combine("DeploymentProjects", "MyCdkApp"));

            // Generate and save the CDK deployment project
            _saveDirectoryPath = "DeploymentProjects";
            var deployArgs = new[] { "deployment-project", "generate", "--project-path", _targetApplicationProjectPath, "--output", _saveDirectoryPath };
            var returnCode = await _app.Run(deployArgs);

            Assert.Equal(CommandReturnCodes.USER_ERROR, returnCode);

            Directory.Delete("DeploymentProjects", true);
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
                if (Directory.Exists(_saveDirectoryPath))
                    Directory.Delete(_saveDirectoryPath, true);

                if (File.Exists(_deploymentManifestFilePath))
                    File.Delete(_deploymentManifestFilePath);
            }

            _isDisposed = true;
        }

        ~SaveCdkDeploymentProjectTests()
        {
            Dispose(false);
        }
    }
}
