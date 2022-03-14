// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Runtime;
using AWS.Deploy.CLI.UnitTests.Utilities;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.RecommendationEngine;
using AWS.Deploy.Recipes;
using Moq;
using Xunit;

namespace AWS.Deploy.CLI.UnitTests
{
    public class GetOptionSettingTests
    {
        private async Task<RecommendationEngine> BuildRecommendationEngine(string testProjectName)
        {
            var fullPath = SystemIOUtilities.ResolvePath(testProjectName);

            var parser = new ProjectDefinitionParser(new FileManager(), new DirectoryManager());
            var awsCredentials = new Mock<AWSCredentials>();
            var session =  new OrchestratorSession(
                await parser.Parse(fullPath),
                awsCredentials.Object,
                "us-west-2",
                "123456789012")
            {
                AWSProfileName = "default"
            };

            return new RecommendationEngine(new[] { RecipeLocator.FindRecipeDefinitionsPath() }, session);
        }

        [Theory]
        [InlineData("ApplicationIAMRole.RoleArn", "RoleArn")]
        public async Task GetOptionSettingTests_OptionSettingExists(string jsonPath, string targetId)
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID);

            var optionSetting = beanstalkRecommendation.GetOptionSetting(jsonPath);

            Assert.NotNull(optionSetting);
            Assert.Equal(optionSetting.Id, targetId);
        }

        [Theory]
        [InlineData("ApplicationIAMRole.Foo")]
        public async Task GetOptionSettingTests_OptionSettingDoesNotExist(string jsonPath)
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID);

            Assert.Throws<OptionSettingItemDoesNotExistException>(() => beanstalkRecommendation.GetOptionSetting(jsonPath));
        }

        [Theory]
        [InlineData("ElasticBeanstalkManagedPlatformUpdates", "ManagedActionsEnabled", true, 3)]
        [InlineData("ElasticBeanstalkManagedPlatformUpdates", "ManagedActionsEnabled", false, 1)]
        public async Task GetOptionSettingTests_GetDisplayableChildren(string optionSetting, string childSetting, bool childValue, int displayableCount)
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID);

            var managedActionsEnabled = beanstalkRecommendation.GetOptionSetting($"{optionSetting}.{childSetting}");
            managedActionsEnabled.SetValueOverride(childValue);

            var elasticBeanstalkManagedPlatformUpdates = beanstalkRecommendation.GetOptionSetting(optionSetting);
            var elasticBeanstalkManagedPlatformUpdatesValue = beanstalkRecommendation.GetOptionSettingValue<Dictionary<string, object>>(elasticBeanstalkManagedPlatformUpdates);

            Assert.Equal(displayableCount, elasticBeanstalkManagedPlatformUpdatesValue.Count);
        }

        [Fact]
        public async Task GetOptionSettingTests_ListType_InvalidValue()
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var appRunnerRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_APPRUNNER_ID);

            var subnets = appRunnerRecommendation.GetOptionSetting("VPCConnector.Subnets");
            var securityGroups = appRunnerRecommendation.GetOptionSetting("VPCConnector.SecurityGroups");

            Assert.Throws<ValidationFailedException>(() => subnets.SetValueOverride(new SortedSet<string>(){ "subnet1" }));
            Assert.Throws<ValidationFailedException>(() => securityGroups.SetValueOverride(new SortedSet<string>(){ "securityGroup1" }));
        }

        [Fact]
        public async Task GetOptionSettingTests_ListType()
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var appRunnerRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_APPRUNNER_ID);

            var subnets = appRunnerRecommendation.GetOptionSetting("VPCConnector.Subnets");
            var emptySubnetsValue = appRunnerRecommendation.GetOptionSettingValue(subnets);

            subnets.SetValueOverride(new SortedSet<string>(){ "subnet-1234abcd" });
            var subnetsValue = appRunnerRecommendation.GetOptionSettingValue(subnets);

            var emptySubnetsString = Assert.IsType<string>(emptySubnetsValue);
            Assert.True(string.IsNullOrEmpty(emptySubnetsString));

            var subnetsList = Assert.IsType<SortedSet<string>>(subnetsValue);
            Assert.Single(subnetsList);
            Assert.Contains("subnet-1234abcd", subnetsList);
        }
    }
}
