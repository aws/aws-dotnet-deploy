// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;
using AWS.Deploy.Orchestrator.CDK;
using Moq;
using Xunit;

namespace AWS.Deploy.Orchestrator.UnitTest.CDK
{
    public class CDKManagerTests
    {
        private readonly Mock<ICDKInstaller> _mockCdkManager;
        private readonly Mock<INPMPackageInitializer> _mockNodeInitializer;
        private readonly CDKManager _cdkManager;
        private const string _workingDirectory = @"c:\fake\path";

        public CDKManagerTests()
        {
            _mockCdkManager = new Mock<ICDKInstaller>();
            _mockNodeInitializer = new Mock<INPMPackageInitializer>();
            _cdkManager = new CDKManager(_mockCdkManager.Object, _mockNodeInitializer.Object);
        }

        [Theory]
        [InlineData("1.0.1", "1.0.0")]
        [InlineData("1.0.1", "1.0.1")]
        public async Task Install_CompatibleGlobalCDKExists(string installedVersion, string requiredVersion)
        {
            // Arrange
            _mockCdkManager
                .Setup(cm => cm.GetGlobalVersion())
                .Returns(Task.FromResult(TryGetResult.FromResult(Version.Parse(installedVersion))));

            // Act
            await _cdkManager.EnsureCompatibleCDKExists(_workingDirectory, Version.Parse(requiredVersion));

            // Assert: when CDK CLI is installed in global node_modules, local node_modules must not be referenced.
            _mockCdkManager.Verify(cm => cm.GetLocalVersion(_workingDirectory), Times.Never);
        }

        [Theory]
        [InlineData("1.0.1")]
        public async Task Install_NPMPackageIsNotInitialized(string requiredVersion)
        {
            // Arrange
            _mockCdkManager
                .Setup(cm => cm.GetGlobalVersion())
                .Returns(Task.FromResult(TryGetResult.Failure<Version>()));

            _mockNodeInitializer
                .Setup(nodeInitializer => nodeInitializer.IsInitialized(_workingDirectory))
                .Returns(false);

            // Act
            await _cdkManager.EnsureCompatibleCDKExists(_workingDirectory, Version.Parse(requiredVersion));

            // Assert: node app must be initialized if global node_modules doesn't contain CDK CLI.
            _mockNodeInitializer.Verify(nodeInitializer => nodeInitializer.Initialize(_workingDirectory, Version.Parse(requiredVersion)), Times.Once);
        }

        [Theory]
        [InlineData("1.0.1", "1.0.0")]
        [InlineData("1.0.1", "1.0.1")]
        public async Task Install_CompatibleLocalCDKExists(string installedVersion, string requiredVersion)
        {
            // Arrange
            _mockCdkManager
                .Setup(cm => cm.GetGlobalVersion())
                .Returns(Task.FromResult(TryGetResult.Failure<Version>()));

            _mockNodeInitializer
                .Setup(nodeInitializer => nodeInitializer.IsInitialized(_workingDirectory))
                .Returns(true);

            _mockCdkManager
                .Setup(cm => cm.GetLocalVersion(_workingDirectory))
                .Returns(Task.FromResult(TryGetResult.FromResult(Version.Parse(installedVersion))));

            // Act
            await _cdkManager.EnsureCompatibleCDKExists(_workingDirectory, Version.Parse(requiredVersion));

            // Assert: when a local node_modules contains a compatible CDK CLI, CDK CLI installation must not be performed.
            _mockCdkManager.Verify(cm => cm.Install(_workingDirectory, Version.Parse(requiredVersion)), Times.Never);
        }

        [Theory]
        [InlineData("1.0.0", "2.0.0")]
        public async Task Install_CompatibleLocalCDKDoesNotExist(string localVersion, string requiredVersion)
        {
            // Arrange
            _mockCdkManager
                .Setup(cm => cm.GetGlobalVersion())
                .Returns(Task.FromResult(TryGetResult.Failure<Version>()));

            _mockNodeInitializer
                .Setup(nodeInitializer => nodeInitializer.IsInitialized(_workingDirectory))
                .Returns(true);

            _mockCdkManager
                .Setup(cm => cm.GetLocalVersion(_workingDirectory))
                .Returns(Task.FromResult(TryGetResult.FromResult(Version.Parse(localVersion))));

            // Act
            await _cdkManager.EnsureCompatibleCDKExists(_workingDirectory, Version.Parse(requiredVersion));

            // Assert: when a local node_modules doesn't contain a compatible CDK CLI, CDKManager installs required CDK CLI package.
            _mockCdkManager.Verify(cm => cm.Install(_workingDirectory, Version.Parse(requiredVersion)), Times.Once);
        }
    }
}
