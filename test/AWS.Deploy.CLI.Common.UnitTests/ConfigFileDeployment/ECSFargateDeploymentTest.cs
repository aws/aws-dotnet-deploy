// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AWS.Deploy.Common;
using Xunit;

namespace AWS.Deploy.CLI.Common.UnitTests.ConfigFileDeployment
{
    public class ECSFargateDeploymentTest
    {
        private readonly UserDeploymentSettings _userDeploymentSettings;
        public ECSFargateDeploymentTest()
        {
            var filePath = Path.Combine("ConfigFileDeployment", "TestFiles", "UnitTestFiles", "ECSFargateConfigFile.json");
            var userDeploymentSettings = UserDeploymentSettings.ReadSettings(filePath);
            _userDeploymentSettings = userDeploymentSettings;
        }

        [Fact]
        public void VerifyJsonParsing()
        {
            Assert.Equal("default", _userDeploymentSettings.AWSProfile);
            Assert.Equal("us-west-2", _userDeploymentSettings.AWSRegion);
            Assert.Equal("MyAppStack", _userDeploymentSettings.ApplicationName);
            Assert.Equal("AspNetAppEcsFargate", _userDeploymentSettings.RecipeId);

            var optionSettingDictionary = _userDeploymentSettings.LeafOptionSettingItems;
            Assert.Equal("True", optionSettingDictionary["ECSCluster.CreateNew"]);
            Assert.Equal("MyNewCluster", optionSettingDictionary["ECSCluster.NewClusterName"]);
            Assert.Equal("MyNewService", optionSettingDictionary["ECSServiceName"]);
            Assert.Equal("3", optionSettingDictionary["DesiredCount"]);
            Assert.Equal("True", optionSettingDictionary["ApplicationIAMRole.CreateNew"]);
            Assert.Equal("True", optionSettingDictionary["Vpc.IsDefault"]);
            Assert.Equal("256", optionSettingDictionary["TaskCpu"]);
            Assert.Equal("512", optionSettingDictionary["TaskMemory"]);
            Assert.Equal("C:\\codebase", optionSettingDictionary["DockerExecutionDirectory"]);
        }
    }
}
