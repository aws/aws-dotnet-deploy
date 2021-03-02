// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;
using AWS.Deploy.Orchestrator.CDK;
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
            _mockCdkManager.Setup(cm => cm.GetVersion(_workingDirectory, true)).Returns(Task.FromResult((true, installedVersion)));

            var isCDKInstalled = await _cdkManager.InstallIfNeeded(_workingDirectory, requiredVersion);

            Assert.True(isCDKInstalled);
            _mockCdkManager.Verify(cm => cm.GetVersion(_workingDirectory, false), Times.Never);
        }

        [Theory]
        [InlineData("1.0.1")]
        public async Task Install_NodeAppNotInitialized(string requiredVersion)
        {
            _mockCdkManager.Setup(cm => cm.GetVersion(_workingDirectory, true)).Returns(Task.FromResult((false, default(string))));
            _mockNodeInitializer.Setup(nodeInitializer => nodeInitializer.IsInitialized(_workingDirectory)).Returns(false);

            var isCDKInstalled = await _cdkManager.InstallIfNeeded(_workingDirectory, requiredVersion);

            Assert.True(isCDKInstalled);
            _mockNodeInitializer.Verify(nodeInitializer => nodeInitializer.Initialize(_workingDirectory, requiredVersion));
        }

        [Theory]
        [InlineData("1.0.1", "1.0.0")]
        [InlineData("1.0.1", "1.0.1")]
        public async Task Install_LocalCDKExists(string installedVersion, string requiredVersion)
        {
            _mockCdkManager.Setup(cm => cm.GetVersion(_workingDirectory, false)).Returns(Task.FromResult((true, installedVersion)));

            var isCDKInstalled = await _cdkManager.InstallIfNeeded(_workingDirectory, requiredVersion);

            Assert.True(isCDKInstalled);
            _mockCdkManager.Verify(cm => cm.Install(_workingDirectory, requiredVersion), Times.Never);
        }

        [Theory]
        [InlineData(null, "2.0.0")]
        public async Task Install_LocalCDKDoesNotExist(string installedVersion, string requiredVersion)
        {
            _mockCdkManager.Setup(cm => cm.GetVersion(_workingDirectory, true)).Returns(Task.FromResult((false, installedVersion)));
            _mockCdkManager.Setup(cm => cm.GetVersion(_workingDirectory, false)).Returns(Task.FromResult((false, installedVersion)));

            var isCDKInstalled = await _cdkManager.InstallIfNeeded(_workingDirectory, requiredVersion);

            Assert.True(isCDKInstalled);
            _mockCdkManager.Verify(cm => cm.Install(_workingDirectory, requiredVersion), Times.Once);
        }
    }
}
