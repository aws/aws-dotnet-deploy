// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Runtime;
using AWS.Deploy.CLI.TypeHintResponses;
using AWS.Deploy.CLI.UnitTests.Utilities;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Recipes;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.RecommendationEngine;
using Moq;
using Newtonsoft.Json;
using Xunit;
using Assert = Should.Core.Assertions.Assert;

namespace AWS.Deploy.CLI.UnitTests
{
    public class ApplyPreviousSettingsTests
    {
        private async Task<RecommendationEngine> BuildRecommendationEngine(string testProjectName)
        {
            var fullPath = SystemIOUtilities.ResolvePath(testProjectName);

            var parser = new ProjectDefinitionParser(new FileManager(), new DirectoryManager());
            var awsCredentials = new Mock<AWSCredentials>();
            var systemCapabilities = new Mock<SystemCapabilities>(
                It.IsAny<bool>(),
                It.IsAny<DockerInfo>());
            var session =  new OrchestratorSession(
                await parser.Parse(fullPath),
                "default",
                awsCredentials.Object,
                "us-west-2",
                Task.FromResult(systemCapabilities.Object),
                "123456789012");

            return new RecommendationEngine(new[] { RecipeLocator.FindRecipeDefinitionsPath() }, session);
        }

        [Theory]
        [InlineData(true, null)]
        [InlineData(false, "role_arn")]
        public async Task ApplyApplicationIAMRolePreviousSettings(bool createNew, string roleArn)
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

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
        [InlineData(true, false, "")]
        [InlineData(false, true, "")]
        [InlineData(false, false, "vpc_id")]
        public async Task ApplyVpcPreviousSettings(bool isDefault, bool createNew, string vpcId)
        {
            var engine = await BuildRecommendationEngine("WebAppWithDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var fargateRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_ASPNET_CORE_FARGATE_RECIPE_ID);

            var vpcIdValue = string.IsNullOrEmpty(vpcId) ? "\"\"" : $"\"{vpcId}\"";

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
