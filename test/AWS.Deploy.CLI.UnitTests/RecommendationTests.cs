// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Runtime;
using AWS.Deploy.CLI.TypeHintResponses;
using AWS.Deploy.CLI.UnitTests.Utilities;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.RecommendationEngine;
using AWS.Deploy.Recipes;
using Moq;
using Should;
using Xunit;

namespace AWS.Deploy.CLI.UnitTests
{
    public class RecommendationTests
    {
        private OrchestratorSession _session;
        private async Task<RecommendationEngine> BuildRecommendationEngine(string testProjectName)
        {
            var fullPath = SystemIOUtilities.ResolvePath(testProjectName);

            var parser = new ProjectDefinitionParser(new FileManager(), new DirectoryManager());
            var awsCredentials = new Mock<AWSCredentials>();
            var systemCapabilities = new Mock<SystemCapabilities>(
                It.IsAny<bool>(),
                It.IsAny<DockerInfo>());
            _session =  new OrchestratorSession(
                await parser.Parse(fullPath),
                awsCredentials.Object,
                "us-west-2",
                "123456789012")
            {
                SystemCapabilities = Task.FromResult(systemCapabilities.Object),
                AWSProfileName = "default"
            };

            return new RecommendationEngine(new[] { RecipeLocator.FindRecipeDefinitionsPath() }, _session);
        }

        [Fact]
        public async Task WebAppNoDockerFileTest()
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            recommendations
                .Any(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID)
                .ShouldBeTrue("Failed to receive Recommendation: " + Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID);

            recommendations
                .Any(r => r.Recipe.Id == Constants.ASPNET_CORE_ASPNET_CORE_FARGATE_RECIPE_ID)
                .ShouldBeTrue("Failed to receive Recommendation: " + Constants.ASPNET_CORE_ASPNET_CORE_FARGATE_RECIPE_ID);
        }

        [Fact]
        public async Task WebAppWithDockerFileTest()
        {

            var engine = await BuildRecommendationEngine("WebAppWithDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            recommendations
                .Any(r => r.Recipe.Id == Constants.ASPNET_CORE_ASPNET_CORE_FARGATE_RECIPE_ID)
                .ShouldBeTrue("Failed to receive Recommendation: " + Constants.ASPNET_CORE_ASPNET_CORE_FARGATE_RECIPE_ID);

            recommendations
                .Any(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID)
                .ShouldBeTrue("Failed to receive Recommendation: " + Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID);
        }

        [Fact]
        public async Task MessageProcessingAppTest()
        {
            var projectPath = SystemIOUtilities.ResolvePath("MessageProcessingApp");

            var engine = await BuildRecommendationEngine("MessageProcessingApp");

            var recommendations = await engine.ComputeRecommendations();

            recommendations
                .Any(r => r.Recipe.Id == Constants.CONSOLE_APP_FARGATE_SERVICE_RECIPE_ID)
                .ShouldBeTrue("Failed to receive Recommendation: " + Constants.CONSOLE_APP_FARGATE_SERVICE_RECIPE_ID);

            recommendations
                .Any(r => r.Recipe.Id == Constants.CONSOLE_APP_FARGATE_SCHEDULE_TASK_RECIPE_ID)
                .ShouldBeTrue("Failed to receive Recommendation: " + Constants.CONSOLE_APP_FARGATE_SCHEDULE_TASK_RECIPE_ID);
        }

        [Theory]
        [InlineData("BlazorWasm31")]
        [InlineData("BlazorWasm50")]
        public async Task BlazorWasmTest(string projectName)
        {
            var engine = await BuildRecommendationEngine(projectName);

            var recommendations = await engine.ComputeRecommendations();

            var blazorRecommendation = recommendations.FirstOrDefault(r => r.Recipe.Id == Constants.BLAZOR_WASM);

            Assert.NotNull(blazorRecommendation);
            Assert.NotNull(blazorRecommendation.Recipe.DeploymentConfirmation.DefaultMessage);
        }


        [Fact]
        public async Task ValueMappingWithDefaultValue()
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID);
            var environmentTypeOptionSetting = beanstalkRecommendation.Recipe.OptionSettings.First(optionSetting => optionSetting.Id.Equals("EnvironmentType"));

            Assert.Equal("SingleInstance", beanstalkRecommendation.GetOptionSettingValue(environmentTypeOptionSetting));
        }

        [Fact]
        public async Task ResetOptionSettingValue_Int()
        {
            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string>
            {
                "<reset>"
            });

            var consoleUtilities = new ConsoleUtilities(interactiveServices);

            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var fargateRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_ASPNET_CORE_FARGATE_RECIPE_ID);
            var desiredCountOptionSetting = fargateRecommendation.Recipe.OptionSettings.First(optionSetting => optionSetting.Id.Equals("DesiredCount"));

            var originalDefaultValue = fargateRecommendation.GetOptionSettingDefaultValue<int>(desiredCountOptionSetting);

            desiredCountOptionSetting.SetValueOverride(2);

            Assert.Equal(2, fargateRecommendation.GetOptionSettingValue<int>(desiredCountOptionSetting));

            desiredCountOptionSetting.SetValueOverride(consoleUtilities.AskUserForValue("Title", "2", true, originalDefaultValue.ToString()));

            Assert.Equal(originalDefaultValue, fargateRecommendation.GetOptionSettingValue<int>(desiredCountOptionSetting));
        }

        [Fact]
        public async Task ResetOptionSettingValue_String()
        {
            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string>
            {
                "<reset>"
            });
            var consoleUtilities = new ConsoleUtilities(interactiveServices);

            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var fargateRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_ASPNET_CORE_FARGATE_RECIPE_ID);
            var ecsServiceNameOptionSetting = fargateRecommendation.Recipe.OptionSettings.First(optionSetting => optionSetting.Id.Equals("ECSServiceName"));

            var originalDefaultValue = fargateRecommendation.GetOptionSettingDefaultValue<string>(ecsServiceNameOptionSetting);

            ecsServiceNameOptionSetting.SetValueOverride("TestService");

            Assert.Equal("TestService", fargateRecommendation.GetOptionSettingValue<string>(ecsServiceNameOptionSetting));

            ecsServiceNameOptionSetting.SetValueOverride(consoleUtilities.AskUserForValue("Title", "TestService", true, originalDefaultValue));

            Assert.Equal(originalDefaultValue, fargateRecommendation.GetOptionSettingValue<string>(ecsServiceNameOptionSetting));
        }

        [Fact]
        public async Task ObjectMappingWithDefaultValue()
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID);
            var applicationIAMRoleOptionSetting = beanstalkRecommendation.Recipe.OptionSettings.First(optionSetting => optionSetting.Id.Equals("ApplicationIAMRole"));

            var iamRoleTypeHintResponse = beanstalkRecommendation.GetOptionSettingValue<IAMRoleTypeHintResponse>(applicationIAMRoleOptionSetting);

            Assert.Null(iamRoleTypeHintResponse.RoleArn);
            Assert.True(iamRoleTypeHintResponse.CreateNew);
        }

        [Fact]
        public async Task ObjectMappingWithoutDefaultValue()
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID);
            var applicationIAMRoleOptionSetting = beanstalkRecommendation.Recipe.OptionSettings.First(optionSetting => optionSetting.Id.Equals("ApplicationIAMRole"));

            Assert.Null(beanstalkRecommendation.GetOptionSettingDefaultValue(applicationIAMRoleOptionSetting));
        }

        [Fact]
        public async Task ValueMappingSetWithValue()
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID);
            var environmentTypeOptionSetting = beanstalkRecommendation.Recipe.OptionSettings.First(optionSetting => optionSetting.Id.Equals("EnvironmentType"));

            environmentTypeOptionSetting.SetValueOverride("LoadBalanced");
            Assert.Equal("LoadBalanced", beanstalkRecommendation.GetOptionSettingValue(environmentTypeOptionSetting));
        }

        [Fact]
        public async Task ObjectMappingSetWithValue()
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID);
            var applicationIAMRoleOptionSetting = beanstalkRecommendation.Recipe.OptionSettings.First(optionSetting => optionSetting.Id.Equals("ApplicationIAMRole"));

            applicationIAMRoleOptionSetting.SetValueOverride(new IAMRoleTypeHintResponse {CreateNew = false, RoleArn = "role_arn"});

            var iamRoleTypeHintResponse = beanstalkRecommendation.GetOptionSettingValue<IAMRoleTypeHintResponse>(applicationIAMRoleOptionSetting);

            Assert.Equal("role_arn", iamRoleTypeHintResponse.RoleArn);
            Assert.False(iamRoleTypeHintResponse.CreateNew);
        }

        [Fact]
        public async Task ApplyProjectNameToSettings()
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var beanstalkRecommendation = recommendations.FirstOrDefault(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID);
            var beanstalEnvNameSetting = beanstalkRecommendation.Recipe.OptionSettings.FirstOrDefault(x => string.Equals("EnvironmentName", x.Id));
            Assert.Equal("WebAppNoDockerFile-dev", beanstalkRecommendation.GetOptionSettingValue<string>(beanstalEnvNameSetting));

            beanstalkRecommendation.OverrideProjectName("CustomName");
            Assert.Equal("CustomName-dev", beanstalkRecommendation.GetOptionSettingValue<string>(beanstalEnvNameSetting));
        }

        [Theory]
        [MemberData(nameof(ShouldIncludeTestCases))]
        public void ShouldIncludeTests(RuleEffect effect, bool testPass, bool expectedResult)
        {
            var awsCredentials = new Mock<AWSCredentials>();
            var session =  new OrchestratorSession(
                null,
                awsCredentials.Object,
                "us-west-2",
                "123456789012")
            {
                AWSProfileName = "default"
            };
            var engine = new RecommendationEngine(new[] { RecipeLocator.FindRecipeDefinitionsPath() }, session);

            Assert.Equal(expectedResult, engine.ShouldInclude(effect, testPass));
        }

        public static IEnumerable<object[]> ShouldIncludeTestCases =>
            new List<object[]>
            {
                // No effect defined
                new object[]{ new RuleEffect { }, true, true },
                new object[]{ new RuleEffect { }, false, false },

                // Negative Rule
                new object[]{ new RuleEffect { Pass = new EffectOptions {Include = false }, Fail = new EffectOptions { Include = true } }, true, false },
                new object[]{ new RuleEffect { Pass = new EffectOptions {Include = false }, Fail = new EffectOptions { Include = true } }, false, true },

                // Explicitly define effects
                new object[]{ new RuleEffect { Pass = new EffectOptions {Include = true }, Fail = new EffectOptions { Include = false} }, true, true },
                new object[]{ new RuleEffect { Pass = new EffectOptions {Include = true }, Fail = new EffectOptions { Include = false} }, false, false },

                // Positive rule to adjust priority
                new object[]{ new RuleEffect { Pass = new EffectOptions {PriorityAdjustment = 55 } }, true, true },
                new object[]{ new RuleEffect { Pass = new EffectOptions { PriorityAdjustment = 55 }, Fail = new EffectOptions { Include = true } }, false, true },

                // Negative rule to adjust priority
                new object[]{ new RuleEffect { Fail = new EffectOptions {PriorityAdjustment = -55 } }, true, true },
                new object[]{ new RuleEffect { Fail = new EffectOptions { PriorityAdjustment = -55 } }, false, true },
            };

        [Fact]
        public async Task IsDisplayable_OneDependency()
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID);
            var environmentTypeOptionSetting = beanstalkRecommendation.Recipe.OptionSettings.First(optionSetting => optionSetting.Id.Equals("EnvironmentType"));

            var loadBalancerTypeOptionSetting = beanstalkRecommendation.Recipe.OptionSettings.First(optionSetting => optionSetting.Id.Equals("LoadBalancerType"));

            Assert.Equal("SingleInstance", beanstalkRecommendation.GetOptionSettingValue(environmentTypeOptionSetting));

            // Before dependency isn't satisfied
            Assert.False(beanstalkRecommendation.IsOptionSettingDisplayable(loadBalancerTypeOptionSetting));

            // Satisfy dependency
            environmentTypeOptionSetting.SetValueOverride("LoadBalanced");
            Assert.Equal("LoadBalanced", beanstalkRecommendation.GetOptionSettingValue(environmentTypeOptionSetting));

            // Verify
            Assert.True(beanstalkRecommendation.IsOptionSettingDisplayable(loadBalancerTypeOptionSetting));
        }

        [Fact]
        public async Task IsDisplayable_ManyDependencies()
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var fargateRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_ASPNET_CORE_FARGATE_RECIPE_ID);
            var isDefaultOptionSetting = fargateRecommendation.GetOptionSetting("Vpc.IsDefault");
            var createNewOptionSetting = fargateRecommendation.GetOptionSetting("Vpc.CreateNew");
            var vpcIdOptionSetting = fargateRecommendation.GetOptionSetting("Vpc.VpcId");

            // Before dependency aren't satisfied
            Assert.False(fargateRecommendation.IsOptionSettingDisplayable(vpcIdOptionSetting));

            // Satisfy dependencies
            isDefaultOptionSetting.SetValueOverride(false);
            Assert.False(fargateRecommendation.GetOptionSettingValue<bool>(isDefaultOptionSetting));

            // Default value for Vpc.CreateNew already false, this is to show explicitly setting an override that satisfies Vpc Id option setting
            createNewOptionSetting.SetValueOverride(false);
            Assert.False(fargateRecommendation.GetOptionSettingValue<bool>(createNewOptionSetting));

            // Verify
            Assert.True(fargateRecommendation.IsOptionSettingDisplayable(vpcIdOptionSetting));
        }

        [Fact]
        public void LoadAvailableRecommendationTests()
        {
            var tests = RecommendationTestFactory.LoadAvailableTests();

            Assert.True(tests.Count > 0);

            // Look to see if the known system test FileExists has been found by LoadAvailableTests.
            Assert.Contains(new FileExistsTest().Name, tests);
        }

        [Fact]
        public async Task PackageReferenceTest()
        {
            var projectPath = SystemIOUtilities.ResolvePath("MessageProcessingApp");

            var projectDefinition = await new ProjectDefinitionParser(new FileManager(), new DirectoryManager()).Parse(projectPath);

            var test = new NuGetPackageReferenceTest();

            Assert.True(await test.Execute(new RecommendationTestInput(
                new RuleTest(
                    test.Name,
                    new RuleCondition
                    {
                        NuGetPackageName = "AWSSDK.SQS"
                    }),
                projectDefinition,
                _session)));

            Assert.False(await test.Execute(new RecommendationTestInput(
                new RuleTest(
                    test.Name,
                    new RuleCondition
                    {
                        NuGetPackageName = "AWSSDK.S3"
                    }),
                projectDefinition,
                _session)));
        }
    }
}
