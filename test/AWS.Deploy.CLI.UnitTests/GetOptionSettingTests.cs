// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Runtime;
using AWS.Deploy.CLI.UnitTests.Utilities;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.RecommendationEngine;
using AWS.Deploy.Recipes;
using Moq;
using Xunit;

namespace AWS.Deploy.CLI.UnitTests
{
    public class GetOptionSettingTests
    {
        private readonly IOptionSettingHandler _optionSettingHandler;

        public GetOptionSettingTests()
        {
            _optionSettingHandler = new OptionSettingHandler();
        }

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

            var optionSetting = _optionSettingHandler.GetOptionSetting(beanstalkRecommendation, jsonPath);

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

            Assert.Throws<OptionSettingItemDoesNotExistException>(() => _optionSettingHandler.GetOptionSetting(beanstalkRecommendation, jsonPath));
        }

        [Theory]
        [InlineData("ElasticBeanstalkManagedPlatformUpdates", "ManagedActionsEnabled", true, 3)]
        [InlineData("ElasticBeanstalkManagedPlatformUpdates", "ManagedActionsEnabled", false, 1)]
        public async Task GetOptionSettingTests_GetDisplayableChildren(string optionSetting, string childSetting, bool childValue, int displayableCount)
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID);

            var managedActionsEnabled = _optionSettingHandler.GetOptionSetting(beanstalkRecommendation, $"{optionSetting}.{childSetting}");
            _optionSettingHandler.SetOptionSettingValue(managedActionsEnabled, childValue);

            var elasticBeanstalkManagedPlatformUpdates = _optionSettingHandler.GetOptionSetting(beanstalkRecommendation, optionSetting);
            var elasticBeanstalkManagedPlatformUpdatesValue = _optionSettingHandler.GetOptionSettingValue<Dictionary<string, object>>(beanstalkRecommendation, elasticBeanstalkManagedPlatformUpdates);

            Assert.Equal(displayableCount, elasticBeanstalkManagedPlatformUpdatesValue.Count);
        }

        [Fact]
        public async Task GetOptionSettingTests_ListType_InvalidValue()
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var appRunnerRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_APPRUNNER_ID);

            var subnets = _optionSettingHandler.GetOptionSetting(appRunnerRecommendation, "VPCConnector.Subnets");
            var securityGroups = _optionSettingHandler.GetOptionSetting(appRunnerRecommendation, "VPCConnector.SecurityGroups");

            Assert.Throws<ValidationFailedException>(() => _optionSettingHandler.SetOptionSettingValue(subnets, new SortedSet<string>(){ "subnet1" }));
            Assert.Throws<ValidationFailedException>(() => _optionSettingHandler.SetOptionSettingValue(securityGroups, new SortedSet<string>(){ "securityGroup1" }));
        }

        [Fact]
        public async Task GetOptionSettingTests_ListType()
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var appRunnerRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_APPRUNNER_ID);

            var subnets = _optionSettingHandler.GetOptionSetting(appRunnerRecommendation, "VPCConnector.Subnets");
            var emptySubnetsValue = _optionSettingHandler.GetOptionSettingValue(appRunnerRecommendation, subnets);

            _optionSettingHandler.SetOptionSettingValue(subnets, new SortedSet<string>(){ "subnet-1234abcd" });
            var subnetsValue = _optionSettingHandler.GetOptionSettingValue(appRunnerRecommendation, subnets);

            var emptySubnetsString = Assert.IsType<string>(emptySubnetsValue);
            Assert.True(string.IsNullOrEmpty(emptySubnetsString));

            var subnetsList = Assert.IsType<SortedSet<string>>(subnetsValue);
            Assert.Single(subnetsList);
            Assert.Contains("subnet-1234abcd", subnetsList);
        }
    }
}
