// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using AWS.Deploy.CLI.IntegrationTests.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Services;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace AWS.Deploy.CLI.IntegrationTests.SaveCdkDeploymentProject
{
    [Collection("SaveCdkDeploymentProjectTests")]
    public class SaveCdkDeploymentProjectTests : IDisposable
    {
        private readonly string _targetApplicationProjectPath;
        private readonly string _deploymentManifestFilePath;
        private readonly string _testArtifactsDirectoryPath;

        private bool _isDisposed;
        private string _saveDirectoryPath;

        public SaveCdkDeploymentProjectTests()
        {
            var testAppsDirectoryPath = Utilities.ResolvePathToTestApps();
            _targetApplicationProjectPath = Path.Combine(testAppsDirectoryPath, "WebAppWithDockerFile");
            _deploymentManifestFilePath = Path.Combine(testAppsDirectoryPath, "WebAppWithDockerFile", "aws-deployments.json");
            _testArtifactsDirectoryPath = Path.Combine(testAppsDirectoryPath, "TestArtifacts");
        }

        [Fact]
        public async Task DefaultSaveDirectory()
        {
            _saveDirectoryPath = _targetApplicationProjectPath + "CDK";
            await Utilities.CreateCDKDeploymentProject(_targetApplicationProjectPath);
            CleanUp();
        }

        [Fact]
        public async Task CustomSaveDirectory()
        {
            _saveDirectoryPath = Path.Combine(_testArtifactsDirectoryPath, "MyCdkApp");
            await Utilities.CreateCDKDeploymentProject(_targetApplicationProjectPath, _saveDirectoryPath);
            CleanUp();
        }

        [Fact]
        public async Task InvalidSaveCdkDirectoryInsideProjectDirectory()
        {
            
            _saveDirectoryPath = Path.Combine(_targetApplicationProjectPath, "MyCdkApp");
            await Utilities.CreateCDKDeploymentProject(_targetApplicationProjectPath, _saveDirectoryPath, false);
            CleanUp();
        }

        [Fact]
        public async Task InvalidNonEmptySaveCdkDirectory()
        {
            Directory.CreateDirectory(Path.Combine(_testArtifactsDirectoryPath, "MyCdkAPP", "MyFolder"));
            _saveDirectoryPath = Path.Combine(_testArtifactsDirectoryPath, "MyCdkApp");
            await Utilities.CreateCDKDeploymentProject(_targetApplicationProjectPath, _saveDirectoryPath, false);
            CleanUp();
        }

        private void CleanUp()
        {
            if (Directory.Exists(_testArtifactsDirectoryPath))
                Directory.Delete(_testArtifactsDirectoryPath, true);

            if (File.Exists(_deploymentManifestFilePath))
                File.Delete(_deploymentManifestFilePath);

            if (Directory.Exists(_saveDirectoryPath))
                Directory.Delete(_saveDirectoryPath, true);
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

        ~SaveCdkDeploymentProjectTests()
        {
            Dispose(false);
        }
    }
}
