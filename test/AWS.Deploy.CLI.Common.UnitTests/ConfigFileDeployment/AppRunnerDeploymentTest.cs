// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AWS.Deploy.Common;
using Newtonsoft.Json;
using Xunit;

namespace AWS.Deploy.CLI.Common.UnitTests.ConfigFileDeployment
{
    public class AppRunnerDeploymentTest
    {
        private readonly UserDeploymentSettings _userDeploymentSettings;
        public AppRunnerDeploymentTest()
        {
            var filePath = Path.Combine("ConfigFileDeployment", "TestFiles", "UnitTestFiles", "AppRunnerConfigFile.json");
            var userDeploymentSettings = UserDeploymentSettings.ReadSettings(filePath);
            _userDeploymentSettings = userDeploymentSettings;
        }

        [Fact]
        public void VerifyJsonParsing()
        {
            Assert.Equal("default", _userDeploymentSettings.AWSProfile);
            Assert.Equal("us-west-2", _userDeploymentSettings.AWSRegion);
            Assert.Equal("MyAppStack", _userDeploymentSettings.ApplicationName);
            Assert.Equal("AspNetAppAppRunner", _userDeploymentSettings.RecipeId);

            var optionSettingDictionary = _userDeploymentSettings.LeafOptionSettingItems;
            Assert.Equal("True", optionSettingDictionary["VPCConnector.CreateNew"]);

            var subnetsString = optionSettingDictionary["VPCConnector.Subnets"];
            var subnets = JsonConvert.DeserializeObject<SortedSet<string>>(subnetsString);
            Assert.Single(subnets);
            Assert.Contains("subnet-1234abcd", subnets);

            var securityGroupsString = optionSettingDictionary["VPCConnector.SecurityGroups"];
            var securityGroups = JsonConvert.DeserializeObject<SortedSet<string>>(securityGroupsString);
            Assert.Single(securityGroups);
            Assert.Contains("sg-1234abcd", securityGroups);
        }
    }
}
