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
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Recipes.Validation;
using AWS.Deploy.Common.Data;
using Amazon.CloudControlApi.Model;
using Amazon.ElasticBeanstalk.Model;

namespace AWS.Deploy.CLI.UnitTests
{
    public class ApplyPreviousSettingsTests
    {
        private readonly Mock<IAWSResourceQueryer> _awsResourceQueryer;
        private readonly IOptionSettingHandler _optionSettingHandler;
        private readonly Orchestrator _orchestrator;
        private readonly Mock<IServiceProvider> _serviceProvider;

        public ApplyPreviousSettingsTests()
        {
            _awsResourceQueryer = new Mock<IAWSResourceQueryer>();
            _serviceProvider = new Mock<IServiceProvider>();
            _serviceProvider
                .Setup(x => x.GetService(typeof(IAWSResourceQueryer)))
                .Returns(_awsResourceQueryer.Object);
            _optionSettingHandler = new OptionSettingHandler(new ValidatorFactory(_serviceProvider.Object));
            _orchestrator = new Orchestrator(null, null, null, null, null, null, null, null, null, null, null, null, null, null, _optionSettingHandler);
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
        [InlineData(true, null)]
        [InlineData(false, "arn:aws:iam::123456789012:group/Developers")]
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

            beanstalkRecommendation = await _orchestrator.ApplyRecommendationPreviousSettings(beanstalkRecommendation, settings);

            var applicationIAMRoleOptionSetting = beanstalkRecommendation.Recipe.OptionSettings.First(optionSetting => optionSetting.Id.Equals("ApplicationIAMRole"));
            var typeHintResponse = _optionSettingHandler.GetOptionSettingValue<IAMRoleTypeHintResponse>(beanstalkRecommendation, applicationIAMRoleOptionSetting);

            Assert.Equal(roleArn, typeHintResponse.RoleArn);
            Assert.Equal(createNew, typeHintResponse.CreateNew);
        }

        [Theory]
        [InlineData(true, false, "")]
        [InlineData(false, true, "")]
        [InlineData(false, false, "vpc-88888888")]
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

            fargateRecommendation = await _orchestrator.ApplyRecommendationPreviousSettings(fargateRecommendation, settings);

            var vpcOptionSetting = fargateRecommendation.Recipe.OptionSettings.First(optionSetting => optionSetting.Id.Equals("Vpc"));

            Assert.Equal(isDefault, _optionSettingHandler.GetOptionSettingValue(fargateRecommendation, vpcOptionSetting.ChildOptionSettings.First(optionSetting => optionSetting.Id.Equals("IsDefault"))));
            Assert.Equal(createNew, _optionSettingHandler.GetOptionSettingValue(fargateRecommendation, vpcOptionSetting.ChildOptionSettings.First(optionSetting => optionSetting.Id.Equals("CreateNew"))));
            Assert.Equal(vpcId, _optionSettingHandler.GetOptionSettingValue(fargateRecommendation, vpcOptionSetting.ChildOptionSettings.First(optionSetting => optionSetting.Id.Equals("VpcId"))));
        }

        [Fact]
        public async Task ApplyECSClusterNamePreviousSettings()
        {
            _awsResourceQueryer.Setup(x => x.GetCloudControlApiResource(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new ResourceDescription { Identifier = "WebApp" });
            var engine = await BuildRecommendationEngine("WebAppWithDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var fargateRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_ASPNET_CORE_FARGATE_RECIPE_ID);

            var serializedSettings = @$"
            {{
                ""ECSCluster"": {{
                    ""CreateNew"": true,
                    ""NewClusterName"": ""WebApp""
                }}
            }}";

            var settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(serializedSettings);

            fargateRecommendation = await _orchestrator.ApplyRecommendationPreviousSettings(fargateRecommendation, settings);

            var ecsClusterSetting = fargateRecommendation.Recipe.OptionSettings.First(optionSetting => optionSetting.Id.Equals("ECSCluster"));

            Assert.Equal(true, _optionSettingHandler.GetOptionSettingValue(fargateRecommendation, ecsClusterSetting.ChildOptionSettings.First(optionSetting => optionSetting.Id.Equals("CreateNew"))));
            Assert.Equal("WebApp", _optionSettingHandler.GetOptionSettingValue(fargateRecommendation, ecsClusterSetting.ChildOptionSettings.First(optionSetting => optionSetting.Id.Equals("NewClusterName"))));
        }

        [Fact]
        public async Task ApplyBeanstalkApplicationNamePreviousSettings()
        {
            _awsResourceQueryer.Setup(x => x.ListOfElasticBeanstalkApplications(It.IsAny<string>())).ReturnsAsync(new List<ApplicationDescription> { new ApplicationDescription { ApplicationName = "WebApp" } });
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID);

            var serializedSettings = @$"
            {{
                ""BeanstalkApplication"": {{
                    ""CreateNew"": true,
                    ""ApplicationName"": ""WebApp""
                }}
            }}";

            var settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(serializedSettings);

            beanstalkRecommendation = await _orchestrator.ApplyRecommendationPreviousSettings(beanstalkRecommendation, settings);

            var applicationSetting = beanstalkRecommendation.Recipe.OptionSettings.First(optionSetting => optionSetting.Id.Equals("BeanstalkApplication"));

            Assert.Equal(true, _optionSettingHandler.GetOptionSettingValue(beanstalkRecommendation, applicationSetting.ChildOptionSettings.First(optionSetting => optionSetting.Id.Equals("CreateNew"))));
            Assert.Equal("WebApp", _optionSettingHandler.GetOptionSettingValue(beanstalkRecommendation, applicationSetting.ChildOptionSettings.First(optionSetting => optionSetting.Id.Equals("ApplicationName"))));
        }

        [Fact]
        public async Task ApplyBeanstalkEnvironmentNamePreviousSettings()
        {
            _awsResourceQueryer.Setup(x => x.ListOfElasticBeanstalkEnvironments(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new List<EnvironmentDescription> { new EnvironmentDescription { EnvironmentName = "WebApp" } });
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID);

            var serializedSettings = @$"
            {{
                ""BeanstalkEnvironment"": {{
                    ""EnvironmentName"": ""WebApp""
                }}
            }}";

            var settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(serializedSettings);

            beanstalkRecommendation = await _orchestrator.ApplyRecommendationPreviousSettings(beanstalkRecommendation, settings);

            var environmentSetting = beanstalkRecommendation.Recipe.OptionSettings.First(optionSetting => optionSetting.Id.Equals("BeanstalkEnvironment"));

            Assert.Equal("WebApp", _optionSettingHandler.GetOptionSettingValue(beanstalkRecommendation, environmentSetting.ChildOptionSettings.First(optionSetting => optionSetting.Id.Equals("EnvironmentName"))));
        }
    }
}
