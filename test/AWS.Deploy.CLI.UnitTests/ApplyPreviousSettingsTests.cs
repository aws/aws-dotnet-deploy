// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using AWS.Deploy.CLI.TypeHintResponses;
using AWS.Deploy.CLI.UnitTests.Utilities;
using AWS.Deploy.Recipes;
using Newtonsoft.Json;
using Xunit;
using Assert = Should.Core.Assertions.Assert;

namespace AWS.Deploy.CLI.UnitTests
{
    public class ApplyPreviousSettingsTests
    {
        [Theory]
        [InlineData(true, null)]
        [InlineData(false, "role_arn")]
        public void ApplyApplicationIAMRolePreviousSettings(bool createNew, string roleArn)
        {
            var projectPath = SystemIOUtilities.ResolvePath("WebAppNoDockerFile");
            var engine = new RecommendationEngine.RecommendationEngine(new[] { RecipeLocator.FindRecipeDefinitionsPath() });
            var recommendations = engine.ComputeRecommendations(projectPath);
            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID);

            var roleArnValue = roleArn == null ? "null" : $"\"{roleArn}\"";

            var serializedSettings = @$"
            {{
                ""ApplicationIAMRole"": {{
                    ""RoleArn"": {roleArnValue},
                    ""CreateNew"": {createNew.ToString().ToLower()}
                }}
            }}";

            var settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(serializedSettings);

            beanstalkRecommendation.ApplyPreviousSettings(settings);

            var applicationIAMRoleOptionSetting = beanstalkRecommendation.Recipe.OptionSettings.First(optionSetting => optionSetting.Id.Equals("ApplicationIAMRole"));
            var typeHintResponse = beanstalkRecommendation.GetOptionSettingValue<IAMRoleTypeHintResponse>(applicationIAMRoleOptionSetting);

            Assert.Equal(roleArn, typeHintResponse.RoleArn);
            Assert.Equal(createNew, typeHintResponse.CreateNew);
        }

        [Theory]
        [InlineData(true, false, null)]
        [InlineData(false, true, null)]
        [InlineData(false, false, "vpc_id")]
        public void ApplyVpcPreviousSettings(bool isDefault, bool createNew, string vpcId)
        {
            var projectPath = SystemIOUtilities.ResolvePath("WebAppWithDockerFile");
            var engine = new RecommendationEngine.RecommendationEngine(new[] { RecipeLocator.FindRecipeDefinitionsPath() });
            var recommendations = engine.ComputeRecommendations(projectPath);
            var fargateRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_ASPNET_CORE_FARGATE_RECIPE_ID);

            var vpcIdValue = vpcId == null ? "null" : $"\"{vpcId}\"";

            var serializedSettings = @$"
            {{
                ""Vpc"": {{
                    ""IsDefault"": {isDefault.ToString().ToLower()},
                    ""CreateNew"": {createNew.ToString().ToLower()},
                    ""VpcId"": {vpcIdValue}
                }}
            }}";

            var settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(serializedSettings);

            fargateRecommendation.ApplyPreviousSettings(settings);

            var vpcOptionSetting = fargateRecommendation.Recipe.OptionSettings.First(optionSetting => optionSetting.Id.Equals("Vpc"));

            Assert.Equal(isDefault, fargateRecommendation.GetOptionSettingValue(vpcOptionSetting.ChildOptionSettings.First(optionSetting => optionSetting.Id.Equals("IsDefault"))));
            Assert.Equal(createNew, fargateRecommendation.GetOptionSettingValue(vpcOptionSetting.ChildOptionSettings.First(optionSetting => optionSetting.Id.Equals("CreateNew"))));
            Assert.Equal(vpcId, fargateRecommendation.GetOptionSettingValue(vpcOptionSetting.ChildOptionSettings.First(optionSetting => optionSetting.Id.Equals("VpcId"))));
        }
    }
}
