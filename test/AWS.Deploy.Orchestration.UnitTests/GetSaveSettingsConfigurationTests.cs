// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Deploy.Orchestration.Utilities;
using AWS.Deploy.Common;
using Moq;
using Xunit;
using System.IO;
using AWS.Deploy.Common.IO;

namespace AWS.Deploy.Orchestration.UnitTests
{
    public class GetSaveSettingsConfigurationTests
    {
        [Fact]
        public void GetSaveSettingsConfiguration_InvalidConfiguration_ThrowsException()
        {
            // ARRANGE
            var saveSettingsPath = "Path/To/JSONFile/1";
            var saveAllSettingsPath = "Path/To/JSONFile/2";
            var projectDirectory = "Path/To/ProjectDirectory";
            var fileManager = new Mock<IFileManager>();
            fileManager.Setup(x => x.IsFileValidPath(It.IsAny<string>())).Returns(true);

            //ACT and ASSERT
            // Its throws an exception because saveSettings and saveSettingsAll both hold a non-null value
            Assert.Throws<FailedToSaveDeploymentSettingsException>(() => Helpers.GetSaveSettingsConfiguration(saveSettingsPath, saveAllSettingsPath, projectDirectory, fileManager.Object));
        }

        [Fact]
        public void GetSaveSettingsConfiguration_ModifiedSettings()
        {
            // ARRANGE
            var temp = Path.GetTempPath();
            var saveSettingsPath = Path.Combine(temp, "Path", "To", "JSONFile");
            var projectDirectory = Path.Combine(temp, "Path", "To", "ProjectDirectory");
            var fileManager = new Mock<IFileManager>();
            fileManager.Setup(x => x.IsFileValidPath(It.IsAny<string>())).Returns(true);

            // ACT
            var saveSettingsConfiguration = Helpers.GetSaveSettingsConfiguration(saveSettingsPath, null, projectDirectory, fileManager.Object);

            // ASSERT
            Assert.Equal(SaveSettingsType.Modified, saveSettingsConfiguration.SettingsType);
            Assert.Equal(saveSettingsPath, saveSettingsConfiguration.FilePath);
        }

        [Fact]
        public void GetSaveSettingsConfiguration_AllSettings()
        {
            // ARRANGE
            var temp = Path.GetTempPath();
            var saveAllSettingsPath = Path.Combine(temp, "Path", "To", "JSONFile");
            var projectDirectory = Path.Combine(temp, "Path", "To", "ProjectDirectory");
            var fileManager = new Mock<IFileManager>();
            fileManager.Setup(x => x.IsFileValidPath(It.IsAny<string>())).Returns(true);

            // ACT
            var saveSettingsConfiguration = Helpers.GetSaveSettingsConfiguration(null, saveAllSettingsPath, projectDirectory, fileManager.Object);

            // ASSERT
            Assert.Equal(SaveSettingsType.All, saveSettingsConfiguration.SettingsType);
            Assert.Equal(saveAllSettingsPath, saveSettingsConfiguration.FilePath);
        }

        [Fact]
        public void GetSaveSettingsConfiguration_None()
        {
            // ARRANGE
            var temp = Path.GetTempPath();
            var projectDirectory = Path.Combine(temp, "Path", "To", "ProjectDirectory");
            var fileManager = new Mock<IFileManager>();
            fileManager.Setup(x => x.IsFileValidPath(It.IsAny<string>())).Returns(true);

            // ACT
            var saveSettingsConfiguration = Helpers.GetSaveSettingsConfiguration(null, null, projectDirectory, fileManager.Object);

            // ASSERT
            Assert.Equal(SaveSettingsType.None, saveSettingsConfiguration.SettingsType);
            Assert.Equal(string.Empty, saveSettingsConfiguration.FilePath);
        }
    }
}
