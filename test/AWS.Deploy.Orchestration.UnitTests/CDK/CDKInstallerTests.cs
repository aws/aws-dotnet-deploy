// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Orchestration.CDK;
using AWS.Deploy.Orchestration.Utilities;
using Xunit;

namespace AWS.Deploy.Orchestration.UnitTests.CDK
{
    public class CDKInstallerTests
    {
        private readonly TestCommandLineWrapper _commandLineWrapper;
        private readonly CDKInstaller _cdkInstaller;
        private readonly IDirectoryManager _directoryManager;
        private const string _workingDirectory = @"c:\fake\path";

        public CDKInstallerTests()
        {
            _commandLineWrapper = new TestCommandLineWrapper();
            _directoryManager = new TestDirectoryManager();
            _cdkInstaller = new CDKInstaller(_commandLineWrapper, _directoryManager);
        }

        [Fact]
        public async Task GetVersion_OutputContainsVersionOnly()
        {
            // Arrange: add empty version information to return
            _commandLineWrapper.Results.Add(new TryRunResult
            {
                StandardOut = @"1.127.0 (build 0ea309a)"
            });

            // Act
            var version = await _cdkInstaller.GetVersion(_workingDirectory);

            // Assert
            Assert.True(version.Success);
            Assert.Equal(0, Version.Parse("1.127.0").CompareTo(version.Result));
            Assert.Contains(("npx --no-install cdk --version", _workingDirectory, false), _commandLineWrapper.Commands);
        }

        [Fact]
        public async Task GetVersion_OutputContainsMessage()
        {
            // Arrange: add fake version information to return
            _commandLineWrapper.Results.Add(new TryRunResult
            {
                StandardOut =
@"!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
!!                                                                                  !!
!!  Node v10.19.0 has reached end-of-life and is not supported.                     !!
!!  You may to encounter runtime issues, and should switch to a supported release.  !!
!!                                                                                  !!
!!  As of the current release, supported versions of node are:                      !!
!!  - ^12.7.0                                                                       !!
!!  - ^14.5.0                                                                       !!
!!  - ^16.3.0                                                                       !!
!!                                                                                  !!
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
1.127.0 (build 0ea309a)"
            });

            // Act
            var version = await _cdkInstaller.GetVersion(_workingDirectory);

            // Assert
            Assert.True(version.Success);
            Assert.Equal(0, Version.Parse("1.127.0").CompareTo(version.Result));
            Assert.Contains(("npx --no-install cdk --version", _workingDirectory, false), _commandLineWrapper.Commands);
        }

        [Fact]
        public async Task Install_InLocalNodeModules()
        {
            // Act
            await _cdkInstaller.Install(_workingDirectory, Version.Parse("1.0.2"));

            // Assert
            Assert.Contains(("npm install aws-cdk@1.0.2", _workingDirectory, false), _commandLineWrapper.Commands);
        }
    }
}
