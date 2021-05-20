// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.CLI.Common.UnitTests;
using AWS.Deploy.Orchestration.CDK;
using AWS.Deploy.Orchestration.Utilities;
using AWS.Deploy.Shell;
using Xunit;

namespace AWS.Deploy.Orchestration.UnitTests.CDK
{
    public class CDKInstallerTests
    {
        private readonly TestCommandRunner _commandRunner;
        private readonly CDKInstaller _cdkInstaller;
        private const string _workingDirectory = @"c:\fake\path";

        public CDKInstallerTests()
        {
            _commandRunner = new TestCommandRunner();
            _cdkInstaller = new CDKInstaller(_commandRunner);
        }

        [Fact]
        public async Task GetGlobalVersion_CDKExists()
        {
            // Arrange: add fake version information to return
            _commandRunner.Results.Add(new TryRunResult
            {
                StandardOut = @"C:\Users\user\AppData\Roaming\npm
+-- aws-cdk@1.91.0"
            });

            // Act
            var globalCDKVersionResult = await _cdkInstaller.GetGlobalVersion();

            // Assert
            Assert.True(globalCDKVersionResult.Success);
            Assert.Equal(0, Version.Parse("1.91.0").CompareTo(globalCDKVersionResult.Result));

            var npmListCommand = _commandRunner.CommandsToExecute.FirstOrDefault(command => command.Command.Equals("npm list aws-cdk --global"));
            Assert.NotNull(npmListCommand);
            Assert.Equal(string.Empty, npmListCommand.WorkingDirectory);
            Assert.False(npmListCommand.StreamOutputToInteractiveService);
        }

        [Fact]
        public async Task GetLocalVersion_CDKExists()
        {
            // Arrange: add fake version information to return
            _commandRunner.Results.Add(new TryRunResult
            {
                StandardOut = @"C:\fake\path
+-- aws-cdk@1.91.0"
            });

            // Act
            var localCDKVersionResult = await _cdkInstaller.GetLocalVersion(_workingDirectory);

            // Assert
            Assert.True(localCDKVersionResult.Success);
            Assert.Equal(0, Version.Parse("1.91.0").CompareTo(localCDKVersionResult.Result));

            var npmListCommand = _commandRunner.CommandsToExecute.FirstOrDefault(command => command.Command.Equals("npm list aws-cdk"));
            Assert.NotNull(npmListCommand);
            Assert.Equal(_workingDirectory, npmListCommand.WorkingDirectory);
            Assert.False(npmListCommand.StreamOutputToInteractiveService);
        }

        [Fact]
        public async Task GetLocalVersion_CDKDependentExists()
        {
            // Arrange: add fake version information to return for a CDK CLI plugin
            _commandRunner.Results.Add(new TryRunResult
            {
                StandardOut = @"C:\Users\user\AppData\Roaming\npm
`-- cdk-assume-role-credential-plugin@1.0.0 (git+https://github.com/aws-samples/cdk-assume-role-credential-plugin.git#5167c798a50bc9c96a9d660b28306428be4e99fb)"
            });

            // Act
            var localCDKVersionResult = await _cdkInstaller.GetLocalVersion(_workingDirectory);

            // Assert
            Assert.False(localCDKVersionResult.Success);
            Assert.Null(localCDKVersionResult.Result);

            var npmListCommand = _commandRunner.CommandsToExecute.FirstOrDefault(command => command.Command.Equals("npm list aws-cdk"));
            Assert.NotNull(npmListCommand);
            Assert.Equal(_workingDirectory, npmListCommand.WorkingDirectory);
            Assert.False(npmListCommand.StreamOutputToInteractiveService);
        }

        [Fact]
        public async Task GetLocalVersion_CDKDoesNotExist()
        {
            // Arrange: add empty version information to return
            _commandRunner.Results.Add(new TryRunResult
            {
                StandardOut = @"C:\Users\user\AppData\Local\Temp\AWS.Deploy\Projects
`-- (empty)"
            });

            // Act
            var localCDKVersionResult = await _cdkInstaller.GetLocalVersion(_workingDirectory);

            // Assert
            Assert.False(localCDKVersionResult.Success);
            Assert.Null(localCDKVersionResult.Result);

            var npmListCommand = _commandRunner.CommandsToExecute.FirstOrDefault(command => command.Command.Equals("npm list aws-cdk"));
            Assert.NotNull(npmListCommand);
            Assert.Equal(_workingDirectory, npmListCommand.WorkingDirectory);
            Assert.False(npmListCommand.StreamOutputToInteractiveService);
        }

        [Fact]
        public async Task Install_InLocalNodeModules()
        {
            // Act
            await _cdkInstaller.Install(_workingDirectory, Version.Parse("1.0.2"));

            // Assert
            var npmInstallCommand = _commandRunner.CommandsToExecute.FirstOrDefault(command => command.Command.Equals("npm install aws-cdk@1.0.2"));
            Assert.NotNull(npmInstallCommand);
            Assert.Equal(_workingDirectory, npmInstallCommand.WorkingDirectory);
            Assert.False(npmInstallCommand.StreamOutputToInteractiveService);
        }
    }
}
