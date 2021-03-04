// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;
using AWS.Deploy.Orchestrator.CDK;
using AWS.Deploy.Orchestrator.Utilities;
using Moq;
using Xunit;

namespace AWS.Deploy.Orchestrator.UnitTest.CDK
{
    public class CDKManagerTests
    {
        private readonly Mock<ICDKInstaller> _mockCdkManager;
        private readonly Mock<INodeInitializer> _mockNodeInitializer;
        private readonly CDKManager _cdkManager;
        private const string _workingDirectory = @"c:\fake\path";

        public CDKManagerTests()
        {
            _mockCdkManager = new Mock<ICDKInstaller>();
            _mockNodeInitializer = new Mock<INodeInitializer>();
            _cdkManager = new CDKManager(_mockCdkManager.Object, _mockNodeInitializer.Object);
        }

        [Theory]
        [InlineData("1.0.1", "1.0.0")]
        [InlineData("1.0.1", "1.0.1")]
        public async Task Install_GlobalCDKExists(string installedVersion, string requiredVersion)
        {
            // Arrange
            _mockCdkManager
                .Setup(cm => cm.GetGlobalVersion())
                .Returns(Task.FromResult(TryGetResult.FromResult(Version.Parse(installedVersion))));

            // Act
            var isCDKInstalled = await _cdkManager.InstallIfNeeded(_workingDirectory, Version.Parse(requiredVersion));

            // Assert
            Assert.True(isCDKInstalled);

            // when CDK CLI is installed in global node_modules, local node_modules must not be referenced.
            _mockCdkManager.Verify(cm => cm.GetLocalVersion(_workingDirectory), Times.Never);
        }

        [Theory]
        [InlineData("1.0.1")]
        public async Task Install_NodeAppNotInitialized(string requiredVersion)
        {
            // Arrange
            _mockCdkManager
                .Setup(cm => cm.GetGlobalVersion())
                .Returns(Task.FromResult(TryGetResult.Failure<Version>()));

            _mockCdkManager
                .Setup(cm => cm.GetLocalVersion(_workingDirectory))
                .Returns(Task.FromResult(TryGetResult.Failure<Version>()));

            _mockNodeInitializer.Setup(nodeInitializer => nodeInitializer.IsInitialized(_workingDirectory)).Returns(false);

            // Act
            var isCDKInstalled = await _cdkManager.InstallIfNeeded(_workingDirectory, Version.Parse(requiredVersion));

            // Assert
            Assert.True(isCDKInstalled);

            // Node app must be initialized if global node_modules doesn't contain AWS CDK CLI.
            _mockNodeInitializer.Verify(nodeInitializer => nodeInitializer.Initialize(_workingDirectory, Version.Parse(requiredVersion)), Times.Once);
        }

        [Theory]
        [InlineData("1.0.1", "1.0.0")]
        [InlineData("1.0.1", "1.0.1")]
        public async Task Install_LocalCDKExists(string installedVersion, string requiredVersion)
        {
            // Arrange
            _mockCdkManager
                .Setup(cm => cm.GetGlobalVersion())
                .Returns(Task.FromResult(TryGetResult.Failure<Version>()));

            _mockCdkManager
                .Setup(cm => cm.GetLocalVersion(_workingDirectory))
                .Returns(Task.FromResult(TryGetResult.FromResult(Version.Parse(installedVersion))));

            // Act
            var isCDKInstalled = await _cdkManager.InstallIfNeeded(_workingDirectory, Version.Parse(requiredVersion));

            // Assert
            Assert.True(isCDKInstalled);

            // When a local node_modules contains a compatible AWS CDK CLI, AWS CDK CLI installation must not be performed.
            _mockCdkManager.Verify(cm => cm.Install(_workingDirectory, Version.Parse(requiredVersion)), Times.Never);
        }

        [Theory]
        [InlineData("2.0.0")]
        public async Task Install_LocalCDKDoesNotExist(string requiredVersion)
        {
            // Arrange
            _mockCdkManager
                .Setup(cm => cm.GetGlobalVersion())
                .Returns(Task.FromResult(TryGetResult.Failure<Version>()));

            _mockCdkManager
                .Setup(cm => cm.GetLocalVersion(_workingDirectory))
                .Returns(Task.FromResult(TryGetResult.Failure<Version>()));

            // Act
            var isCDKInstalled = await _cdkManager.InstallIfNeeded(_workingDirectory, Version.Parse(requiredVersion));

            // Assert
            Assert.True(isCDKInstalled);

            // When a local node_modules doesn't contain a compatible AWS CDK CLI, AWS CDK CLI installation must be performed in local node_modules.
            _mockCdkManager.Verify(cm => cm.Install(_workingDirectory, Version.Parse(requiredVersion)), Times.Once);
        }
    }
}
