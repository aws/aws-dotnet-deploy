// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.LocalUserSettings;
using AWS.Deploy.Orchestration.Utilities;
using Moq;
using Xunit;

namespace AWS.Deploy.CLI.Common.UnitTests.LocalUserSettings
{
    public class LocalUserSettingsTests
    {
        private readonly IFileManager _fileManager;
        private readonly IDirectoryManager _directoryManager;
        private readonly ILocalUserSettingsEngine _localUserSettingsEngine;
        private readonly Mock<IEnvironmentVariableManager> _environmentVariableManager;
        private readonly IDeployToolWorkspaceMetadata _deployToolWorkspaceMetadata;

        public LocalUserSettingsTests()
        {
            _fileManager = new TestFileManager();
            _directoryManager = new DirectoryManager();
            var targetApplicationPath = Path.Combine("testapps", "WebAppWithDockerFile", "WebAppWithDockerFile.csproj");

            _environmentVariableManager = new Mock<IEnvironmentVariableManager>();
            _environmentVariableManager
                .Setup(x => x.GetEnvironmentVariable(It.IsAny<string>()))
                .Returns(() => null);

            _deployToolWorkspaceMetadata = new DeployToolWorkspaceMetadata(_directoryManager, _environmentVariableManager.Object);

            _localUserSettingsEngine = new LocalUserSettingsEngine(_fileManager, _directoryManager, _deployToolWorkspaceMetadata);
        }

        [Fact]
        public async Task UpdateLastDeployedStackTest()
        {
            var stackName = "WebAppWithDockerFile";
            var awsAccountId = "1234567890";
            var awsRegion = "us-west-2";
            var settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aws-dotnet-deploy", "local-user-settings.json");

            await _localUserSettingsEngine.UpdateLastDeployedStack(stackName, stackName, awsAccountId, awsRegion);
            var userSettings = await _localUserSettingsEngine.GetLocalUserSettings();

            Assert.True(_fileManager.Exists(settingsFilePath));
            Assert.NotNull(userSettings);
            Assert.NotNull(userSettings.LastDeployedStacks);
            Assert.Single(userSettings.LastDeployedStacks);
            Assert.Equal(awsAccountId, userSettings.LastDeployedStacks[0].AWSAccountId);
            Assert.Equal(awsRegion, userSettings.LastDeployedStacks[0].AWSRegion);
            Assert.Single(userSettings.LastDeployedStacks[0].Stacks);
            Assert.Equal(stackName, userSettings.LastDeployedStacks[0].Stacks[0]);
        }

        [Fact]
        public async Task UpdateLastDeployedStackTest_ExistingStacks()
        {
            var stackName = "WebAppWithDockerFile";
            var awsAccountId = "1234567890";
            var awsRegion = "us-west-2";
            var settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aws-dotnet-deploy", "local-user-settings.json");
            await _fileManager.WriteAllTextAsync(settingsFilePath, "{\"LastDeployedStacks\": [{\"AWSAccountId\": \"1234567890\",\"AWSRegion\": \"us-west-2\",\"ProjectName\": \"WebApp1\",\"Stacks\": [\"WebApp1\"]}]}");

            await _localUserSettingsEngine.UpdateLastDeployedStack(stackName, stackName, awsAccountId, awsRegion);
            var userSettings = await _localUserSettingsEngine.GetLocalUserSettings();

            Assert.True(_fileManager.Exists(settingsFilePath));
            Assert.NotNull(userSettings);
            Assert.NotNull(userSettings.LastDeployedStacks);
            Assert.Equal(2, userSettings.LastDeployedStacks.Count);
            Assert.Equal(awsAccountId, userSettings.LastDeployedStacks[1].AWSAccountId);
            Assert.Equal(awsRegion, userSettings.LastDeployedStacks[1].AWSRegion);
            Assert.Single(userSettings.LastDeployedStacks[1].Stacks);
            Assert.Equal(stackName, userSettings.LastDeployedStacks[1].Stacks[0]);
        }

        [Fact]
        public async Task DeleteLastDeployedStackTest()
        {
            var stackName = "WebAppWithDockerFile";
            var awsAccountId = "1234567890";
            var awsRegion = "us-west-2";
            var settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aws-dotnet-deploy", "local-user-settings.json");

            await _localUserSettingsEngine.UpdateLastDeployedStack(stackName, stackName, awsAccountId, awsRegion);
            await _localUserSettingsEngine.DeleteLastDeployedStack(stackName, stackName, awsAccountId, awsRegion);
            var userSettings = await _localUserSettingsEngine.GetLocalUserSettings();

            Assert.True(_fileManager.Exists(settingsFilePath));
            Assert.NotNull(userSettings);
            Assert.NotNull(userSettings.LastDeployedStacks);
            Assert.Single(userSettings.LastDeployedStacks);
            Assert.Equal(awsAccountId, userSettings.LastDeployedStacks[0].AWSAccountId);
            Assert.Equal(awsRegion, userSettings.LastDeployedStacks[0].AWSRegion);
        }

        [Fact]
        public async Task CleanOrphanStacksTest()
        {
            var stackName = "WebAppWithDockerFile";
            var awsAccountId = "1234567890";
            var awsRegion = "us-west-2";
            var settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aws-dotnet-deploy", "local-user-settings.json");

            await _localUserSettingsEngine.UpdateLastDeployedStack(stackName, stackName, awsAccountId, awsRegion);
            await _localUserSettingsEngine.CleanOrphanStacks(new List<string> { "WebAppWithDockerFile1" }, stackName, awsAccountId, awsRegion);
            var userSettings = await _localUserSettingsEngine.GetLocalUserSettings();

            Assert.True(_fileManager.Exists(settingsFilePath));
            Assert.NotNull(userSettings);
            Assert.NotNull(userSettings.LastDeployedStacks);
            Assert.Single(userSettings.LastDeployedStacks);
            Assert.Equal(awsAccountId, userSettings.LastDeployedStacks[0].AWSAccountId);
            Assert.Equal(awsRegion, userSettings.LastDeployedStacks[0].AWSRegion);

            // Attempt to clean orphans again. This is to make sure if the underlying stacks array collection is null we don't throw an exception.
            await _localUserSettingsEngine.CleanOrphanStacks(new List<string> { "WebAppWithDockerFile1" }, stackName, awsAccountId, awsRegion);
            userSettings = await _localUserSettingsEngine.GetLocalUserSettings();

            Assert.True(_fileManager.Exists(settingsFilePath));
            Assert.NotNull(userSettings);
            Assert.NotNull(userSettings.LastDeployedStacks);
            Assert.Single(userSettings.LastDeployedStacks);
            Assert.Equal(awsAccountId, userSettings.LastDeployedStacks[0].AWSAccountId);
            Assert.Equal(awsRegion, userSettings.LastDeployedStacks[0].AWSRegion);
        }
    }
}
