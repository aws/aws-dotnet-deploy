// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;
using AWS.Deploy.Orchestrator.CDK;
using AWS.Deploy.Orchestrator.Utilities;
using Xunit;

namespace AWS.Deploy.Orchestrator.UnitTest.CDK
{
    public class CDKInstallerTests
    {
        private readonly TestCommandLineWrapper _commandLineWrapper;
        private readonly CDKInstaller _cdkInstaller;
        private const string _workingDirectory = @"c:\fake\path";

        public CDKInstallerTests()
        {
            _commandLineWrapper = new TestCommandLineWrapper();
            _cdkInstaller = new CDKInstaller(_commandLineWrapper);
        }

        [Fact]
        public async Task GetVersion_InGlobalNodeModules_CDKExists()
        {
            _commandLineWrapper.Results.Add(new TryRunResult()
            {
                StandardOut = @"C:\Users\user\AppData\Roaming\npm
+-- aws-cdk@1.91.0"
            });
            var globalCDKVersionResult = await _cdkInstaller.GetGlobalVersion();

            Assert.True(globalCDKVersionResult.Success);
            Assert.Equal(0, Version.Parse("1.91.0").CompareTo(globalCDKVersionResult.Result));
            Assert.Contains(("npm list aws-cdk --global", string.Empty, false), _commandLineWrapper.Commands);
        }

        [Fact]
        public async Task GetVersion_InLocalNodeModules_CDKExists()
        {
            _commandLineWrapper.Results.Add(new TryRunResult()
            {
                StandardOut = @"C:\fake\path
+-- aws-cdk@1.91.0"
            });
            var localCDKVersionResult = await _cdkInstaller.GetLocalVersion(_workingDirectory);

            Assert.True(localCDKVersionResult.Success);
            Assert.Equal(0, Version.Parse("1.91.0").CompareTo(localCDKVersionResult.Result));
            Assert.Contains(("npm list aws-cdk", _workingDirectory, false), _commandLineWrapper.Commands);
        }

        [Fact]
        public async Task GetVersion_InLocalNodeModules_CDKDependentExists()
        {
            _commandLineWrapper.Results.Add(new TryRunResult()
            {
                StandardOut = @"C:\Users\user\AppData\Roaming\npm
`-- cdk-assume-role-credential-plugin@1.0.0 (git+https://github.com/aws-samples/cdk-assume-role-credential-plugin.git#5167c798a50bc9c96a9d660b28306428be4e99fb)"
            });
            var localCDKVersionResult = await _cdkInstaller.GetLocalVersion(_workingDirectory);

            Assert.False(localCDKVersionResult.Success);
            Assert.Null(localCDKVersionResult.Result);
            Assert.Contains(("npm list aws-cdk", _workingDirectory, false), _commandLineWrapper.Commands);
        }

        [Fact]
        public async Task GetVersion_InLocalNodeModules_CDKDoesNotExist()
        {
            _commandLineWrapper.Results.Add(new TryRunResult()
            {
                StandardOut = @"C:\Users\user\AppData\Local\Temp\AWS.Deploy\Projects
`-- (empty)"
            });
            var localCDKVersionResult = await _cdkInstaller.GetLocalVersion(_workingDirectory);

            Assert.False(localCDKVersionResult.Success);
            Assert.Null(localCDKVersionResult.Result);
            Assert.Contains(("npm list aws-cdk", _workingDirectory, false), _commandLineWrapper.Commands);
        }

        [Fact]
        public async Task Update()
        {
            await _cdkInstaller.Install(_workingDirectory, Version.Parse("1.0.2"));

            Assert.Contains(("npm install aws-cdk@1.0.2", _workingDirectory, false), _commandLineWrapper.Commands);
        }
    }
}
