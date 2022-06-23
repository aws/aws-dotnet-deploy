// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AWS.Deploy.Orchestration.Utilities;
using Xunit;

namespace AWS.Deploy.Orchestration.UnitTests
{
    public class DeployToolWorkspaceTests
    {
        [Theory]
        [InlineData("C:/Users/Bob")]
        [InlineData("C:/Users/Bob/Alice")]
        public void WithoutOverride_UserProfile_WithoutSpaces(string userProfile)
        {
            // ARRANGE
            var directoryManager = new TestDirectoryManager();
            var environmentVariableManager = new TestEnvironmentVariableManager();

            // ACT
            var actualWorkspace = Helpers.GetDeployToolWorkspaceDirectoryRoot(userProfile, directoryManager, environmentVariableManager);

            // ASSERT
            var expectedWorkspace = Path.Combine(userProfile, ".aws-dotnet-deploy");
            Assert.Equal(expectedWorkspace, actualWorkspace);
            Assert.Null(environmentVariableManager.GetEnvironmentVariable(Constants.CLI.WORKSPACE_ENV_VARIABLE));
            Assert.Null(environmentVariableManager.GetEnvironmentVariable("TMP"));
            Assert.Null(environmentVariableManager.GetEnvironmentVariable("TEMP"));
        }

        [Theory]
        [InlineData("C:/Users/Bob Mike")]
        [InlineData("C:/My users/Bob/Alice")]
        [InlineData("C:/Users/Bob Mike/Alice")]
        public void WithoutOverride_UserProfile_WithSpaces_ThrowsException(string userProfile)
        {
            // ARRANGE
            var directoryManager = new TestDirectoryManager();
            var environmentVariableManager = new TestEnvironmentVariableManager();

            // ACT and ASSERT
            Assert.Throws<InvalidDeployToolWorkspaceException>(() => Helpers.GetDeployToolWorkspaceDirectoryRoot(userProfile, directoryManager, environmentVariableManager));
            Assert.Null(environmentVariableManager.GetEnvironmentVariable(Constants.CLI.WORKSPACE_ENV_VARIABLE));
            Assert.Null(environmentVariableManager.GetEnvironmentVariable("TMP"));
            Assert.Null(environmentVariableManager.GetEnvironmentVariable("TEMP"));
        }

        [Theory]
        [InlineData("C:/workspace/deploy-tool-workspace", "C:/Users/Bob")]
        [InlineData("C:/workspace/deploy-tool-workspace", "C:/Users/Admin/Bob Alice")]
        [InlineData("C:/aws-workspaces/deploy-tool-workspace", "C:/Users/Admin/Bob Alice")]
        public void WithOverride_ValidWorkspace(string workspaceOverride, string userProfile)
        {
            // ARRANGE
            var directoryManager = new TestDirectoryManager();
            var environmentVariableManager = new TestEnvironmentVariableManager();
            environmentVariableManager.store["AWS_DOTNET_DEPLOYTOOL_WORKSPACE"] = workspaceOverride;
            directoryManager.CreateDirectory(workspaceOverride);

            // ACT
            var actualWorkspace = Helpers.GetDeployToolWorkspaceDirectoryRoot(userProfile, directoryManager, environmentVariableManager);

            // ASSERT
            var expectedWorkspace = workspaceOverride;
            var expectedTempDir = Path.Combine(workspaceOverride, "temp");
            Assert.True(directoryManager.Exists(expectedWorkspace));
            Assert.True(directoryManager.Exists(expectedTempDir));
            Assert.Equal(expectedWorkspace, actualWorkspace);
            Assert.Equal(expectedWorkspace, environmentVariableManager.GetEnvironmentVariable(Constants.CLI.WORKSPACE_ENV_VARIABLE));
            Assert.Equal(expectedTempDir, environmentVariableManager.GetEnvironmentVariable("TEMP"));
            Assert.Equal(expectedTempDir, environmentVariableManager.GetEnvironmentVariable("TMP"));
        }

        [Theory]
        [InlineData("C:/workspace/deploy-tool-workspace", "C:/Users/Bob")]
        [InlineData("C:/workspace/deploy-tool-workspace", "C:/Users/Admin/Bob Alice")]
        [InlineData("C:/aws-workspaces/deploy-tool-workspace", "C:/Users/Admin/Bob Alice")]
        public void WithOverride_DirectoryDoesNotExist_ThrowsException(string workspaceOverride, string userProfile)
        {
            // ARRANGE
            var directoryManager = new TestDirectoryManager();
            var environmentVariableManager = new TestEnvironmentVariableManager();
            environmentVariableManager.store[Constants.CLI.WORKSPACE_ENV_VARIABLE] = workspaceOverride;

            // ACT and ASSERT
            Assert.Throws<InvalidDeployToolWorkspaceException>(() => Helpers.GetDeployToolWorkspaceDirectoryRoot(userProfile, directoryManager, environmentVariableManager));
            Assert.False(directoryManager.Exists(workspaceOverride));
            Assert.Equal(workspaceOverride, environmentVariableManager.GetEnvironmentVariable(Constants.CLI.WORKSPACE_ENV_VARIABLE));
            Assert.Null(environmentVariableManager.GetEnvironmentVariable("TMP"));
            Assert.Null(environmentVariableManager.GetEnvironmentVariable("TEMP"));
        }

        [Theory]
        [InlineData("C:/workspace/deploy tool workspace", "C:/Users/Bob")]
        [InlineData("C:/workspace/deploy tool workspace", "C:/Users/Admin/Bob Alice")]
        [InlineData("C:/aws workspaces/deploy-tool-workspace", "C:/Users/Admin/Bob Alice")]
        public void WithOverride_DirectoryContainsSpaces_ThrowsException(string workspaceOverride, string userProfile)
        {
            // ARRANGE
            var directoryManager = new TestDirectoryManager();
            var environmentVariableManager = new TestEnvironmentVariableManager();
            environmentVariableManager.store["AWS_DOTNET_DEPLOYTOOL_WORKSPACE"] = workspaceOverride;
            directoryManager.CreateDirectory(workspaceOverride);

            // ACT and ASSERT
            Assert.Throws<InvalidDeployToolWorkspaceException>(() => Helpers.GetDeployToolWorkspaceDirectoryRoot(userProfile, directoryManager, environmentVariableManager));
            Assert.True(directoryManager.Exists(workspaceOverride));
            Assert.False(directoryManager.Exists(Path.Combine(workspaceOverride, "temp")));
            Assert.Equal(workspaceOverride, environmentVariableManager.GetEnvironmentVariable(Constants.CLI.WORKSPACE_ENV_VARIABLE));
            Assert.Null(environmentVariableManager.GetEnvironmentVariable("TMP"));
            Assert.Null(environmentVariableManager.GetEnvironmentVariable("TEMP"));
        }
    }

    public class TestEnvironmentVariableManager : IEnvironmentVariableManager
    {
        public readonly Dictionary<string, string> store = new();

        public string GetEnvironmentVariable(string variable)
        {
            return store.ContainsKey(variable) ? store[variable] : null;
        }

        public void SetEnvironmentVariable(string variable, string value)
        {
            store[variable] = value;
        }
    }
}
