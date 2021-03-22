// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.CLI.UnitTests.Utilities;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.RecommendationEngine;
using AWS.Deploy.Recipes;
using Xunit;

namespace AWS.Deploy.CLI.UnitTests
{
    public class GetOptionSettingTests
    {
        [Theory]
        [InlineData("ApplicationIAMRole.RoleArn", "RoleArn")]
        public async Task GetOptionSettingTests_OptionSettingExists(string jsonPath, string targetId)
        {
            var projectPath = SystemIOUtilities.ResolvePath("WebAppNoDockerFile");
            var engine = new RecommendationEngine(new[] { RecipeLocator.FindRecipeDefinitionsPath() }, new OrchestratorSession());
            var recommendations = await engine.ComputeRecommendations(projectPath, new Dictionary<string, string>());
            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID);

            var optionSetting = beanstalkRecommendation.GetOptionSetting(jsonPath);

            Assert.NotNull(optionSetting);
            Assert.Equal(optionSetting.Id, targetId);
        }

        [Theory]
        [InlineData("ApplicationIAMRole.Foo")]
        public async Task GetOptionSettingTests_OptionSettingDoesNotExist(string jsonPath)
        {
            var projectPath = SystemIOUtilities.ResolvePath("WebAppNoDockerFile");
            var engine = new RecommendationEngine(new[] { RecipeLocator.FindRecipeDefinitionsPath() }, new OrchestratorSession());
            var recommendations = await engine.ComputeRecommendations(projectPath, new Dictionary<string, string>());
            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID);

            var optionSetting = beanstalkRecommendation.GetOptionSetting(jsonPath);

            Assert.Null(optionSetting);
        }
    }
}
