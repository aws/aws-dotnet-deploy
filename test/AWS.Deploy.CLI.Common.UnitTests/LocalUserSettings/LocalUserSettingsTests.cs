// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Orchestration.LocalUserSettings;
using Xunit;

namespace AWS.Deploy.CLI.Common.UnitTests.LocalUserSettings
{
    public class LocalUserSettingsTests
    {
        private readonly IFileManager _fileManager;
        private readonly IDirectoryManager _directoryManager;
        private readonly ILocalUserSettingsEngine _localUserSettingsEngine;

        public LocalUserSettingsTests()
        {
            _fileManager = new TestFileManager();
            _directoryManager = new DirectoryManager();
            var targetApplicationPath = Path.Combine("testapps", "WebAppWithDockerFile", "WebAppWithDockerFile.csproj");
            _localUserSettingsEngine = new LocalUserSettingsEngine(_fileManager, _directoryManager);
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
            Assert.Null(userSettings.LastDeployedStacks[0].Stacks);
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
            Assert.Null(userSettings.LastDeployedStacks[0].Stacks);
        }
    }
}
