// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.CLI.UnitTests.Utilities;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.RecommendationEngine;
using AWS.Deploy.Recipes;
using Xunit;

namespace AWS.Deploy.CLI.UnitTests
{
    public class GetOptionSettingTests
    {
        private async Task<RecommendationEngine> BuildRecommendationEngine(string testProjectName)
        {
            var fullPath = SystemIOUtilities.ResolvePath(testProjectName);

            var parser = new ProjectDefinitionParser(new FileManager(), new DirectoryManager());

            var session =  new OrchestratorSession
            {
                ProjectDefinition = await parser.Parse(fullPath)
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

            var optionSetting = beanstalkRecommendation.GetOptionSetting(jsonPath);

            Assert.Null(optionSetting);
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
    }
}
