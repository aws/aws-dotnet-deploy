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
    public class ElasticBeanStalkDeploymentTest
    {
        private readonly UserDeploymentSettings _userDeploymentSettings;
        public ElasticBeanStalkDeploymentTest()
        {
            var filePath = Path.Combine("ConfigFileDeployment", "TestFiles", "UnitTestFiles", "ElasticBeanStalkConfigFile.json");
            var userDeploymentSettings = UserDeploymentSettings.ReadSettings(filePath);
            _userDeploymentSettings = userDeploymentSettings;
        }

        [Fact]
        public void VerifyJsonParsing()
        {
            Assert.Equal("default", _userDeploymentSettings.AWSProfile);
            Assert.Equal("us-west-2", _userDeploymentSettings.AWSRegion);
            Assert.Equal("MyAppStack", _userDeploymentSettings.StackName);
            Assert.Equal("AspNetAppElasticBeanstalkLinux", _userDeploymentSettings.RecipeId);

            var optionSettingDictionary = _userDeploymentSettings.LeafOptionSettingItems;
            Assert.Equal("True", optionSettingDictionary["BeanstalkApplication.CreateNew"]);
            Assert.Equal("MyApplication", optionSettingDictionary["BeanstalkApplication.ApplicationName"]);
            Assert.Equal("MyEnvironment", optionSettingDictionary["EnvironmentName"]);
            Assert.Equal("MyInstance", optionSettingDictionary["InstanceType"]);
            Assert.Equal("SingleInstance", optionSettingDictionary["EnvironmentType"]);
            Assert.Equal("application", optionSettingDictionary["LoadBalancerType"]);
            Assert.Equal("True", optionSettingDictionary["ApplicationIAMRole.CreateNew"]);
            Assert.Equal("MyPlatformArn", optionSettingDictionary["ElasticBeanstalkPlatformArn"]);
            Assert.Equal("True", optionSettingDictionary["ElasticBeanstalkManagedPlatformUpdates.ManagedActionsEnabled"]);
            Assert.Equal("Mon:12:00", optionSettingDictionary["ElasticBeanstalkManagedPlatformUpdates.PreferredStartTime"]);
            Assert.Equal("minor", optionSettingDictionary["ElasticBeanstalkManagedPlatformUpdates.UpdateLevel"]);
        }
    }
}
