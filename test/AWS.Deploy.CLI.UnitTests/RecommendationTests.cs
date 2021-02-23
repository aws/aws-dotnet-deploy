// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AWS.Deploy.CLI.TypeHintResponses;
using AWS.Deploy.CLI.UnitTests.Utilities;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Recipes;
using AWS.Deploy.Orchestrator.RecommendationEngine;
using Should;
using Xunit;
using System.Threading.Tasks;

namespace AWS.Deploy.CLI.UnitTests
{
    public class RecommendationTests
    {
        [Fact]
        public async Task WebAppNoDockerFileTest()
        {
            var projectPath = SystemIOUtilities.ResolvePath("WebAppNoDockerFile");

            var engine = new RecommendationEngine(new[] { RecipeLocator.FindRecipeDefinitionsPath() }, new Orchestrator.OrchestratorSession());

            var recommendations = await engine.ComputeRecommendations(projectPath, new Dictionary<string, string>());

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
            var projectPath = SystemIOUtilities.ResolvePath("WebAppWithDockerFile");

            var engine = new RecommendationEngine(new[] { RecipeLocator.FindRecipeDefinitionsPath() }, new Orchestrator.OrchestratorSession());

            var recommendations = await engine.ComputeRecommendations(projectPath, new Dictionary<string, string>());

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

            var engine = new RecommendationEngine(new[] { RecipeLocator.FindRecipeDefinitionsPath() }, new Orchestrator.OrchestratorSession());

            var recommendations = await engine.ComputeRecommendations(projectPath, new Dictionary<string, string>());

            recommendations
                .Any(r => r.Recipe.Id == Constants.CONSOLE_APP_FARGATE_SERVICE_RECIPE_ID)
                .ShouldBeTrue("Failed to receive Recommendation: " + Constants.CONSOLE_APP_FARGATE_SERVICE_RECIPE_ID);

            recommendations
                .Any(r => r.Recipe.Id == Constants.CONSOLE_APP_FARGATE_TASK_RECIPE_ID)
                .ShouldBeTrue("Failed to receive Recommendation: " + Constants.CONSOLE_APP_FARGATE_TASK_RECIPE_ID);
        }


        [Fact]
        public async Task ValueMappingWithDefaultValue()
        {
            var projectPath = SystemIOUtilities.ResolvePath("WebAppNoDockerFile");
            var engine = new RecommendationEngine(new[] { RecipeLocator.FindRecipeDefinitionsPath() }, new Orchestrator.OrchestratorSession());
            var recommendations = await engine.ComputeRecommendations(projectPath, new Dictionary<string, string>());
            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID);
            var environmentTypeOptionSetting = beanstalkRecommendation.Recipe.OptionSettings.First(optionSetting => optionSetting.Id.Equals("EnvironmentType"));

            Assert.Equal("SingleInstance", beanstalkRecommendation.GetOptionSettingValue(environmentTypeOptionSetting, false));
        }

        [Fact]
        public async Task ObjectMappingWithDefaultValue()
        {
            var projectPath = SystemIOUtilities.ResolvePath("WebAppNoDockerFile");
            var engine = new RecommendationEngine(new[] { RecipeLocator.FindRecipeDefinitionsPath() }, new Orchestrator.OrchestratorSession());
            var recommendations = await engine.ComputeRecommendations(projectPath, new Dictionary<string, string>());
            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID);
            var applicationIAMRoleOptionSetting = beanstalkRecommendation.Recipe.OptionSettings.First(optionSetting => optionSetting.Id.Equals("ApplicationIAMRole"));

            var iamRoleTypeHintResponse = beanstalkRecommendation.GetOptionSettingValue<IAMRoleTypeHintResponse>(applicationIAMRoleOptionSetting, false);

            Assert.Null(iamRoleTypeHintResponse.RoleArn);
            Assert.True(iamRoleTypeHintResponse.CreateNew);
        }

        [Fact]
        public async Task ObjectMappingWithoutDefaultValue()
        {
            var projectPath = SystemIOUtilities.ResolvePath("WebAppNoDockerFile");
            var engine = new RecommendationEngine(new[] { RecipeLocator.FindRecipeDefinitionsPath() }, new Orchestrator.OrchestratorSession());
            var recommendations = await engine.ComputeRecommendations(projectPath, new Dictionary<string, string>());
            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID);
            var applicationIAMRoleOptionSetting = beanstalkRecommendation.Recipe.OptionSettings.First(optionSetting => optionSetting.Id.Equals("ApplicationIAMRole"));

            Assert.Null(beanstalkRecommendation.GetOptionSettingValue(applicationIAMRoleOptionSetting, true));
        }


        [Fact]
        public async Task ValueMappingSetWithAllowedValues()
        {
            var projectPath = SystemIOUtilities.ResolvePath("WebAppNoDockerFile");
            var engine = new RecommendationEngine(new[] { RecipeLocator.FindRecipeDefinitionsPath() }, new Orchestrator.OrchestratorSession());
            var recommendations = await engine.ComputeRecommendations(projectPath, new Dictionary<string, string>());
            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID);
            var environmentTypeOptionSetting = beanstalkRecommendation.Recipe.OptionSettings.First(optionSetting => optionSetting.Id.Equals("EnvironmentType"));

            environmentTypeOptionSetting.SetValueOverride("Load Balanced");
            Assert.Equal("LoadBalanced", beanstalkRecommendation.GetOptionSettingValue(environmentTypeOptionSetting, false));
        }

        [Fact]
        public async Task ValueMappingSetWithValue()
        {
            var projectPath = SystemIOUtilities.ResolvePath("WebAppNoDockerFile");
            var engine = new RecommendationEngine(new[] { RecipeLocator.FindRecipeDefinitionsPath() }, new Orchestrator.OrchestratorSession());
            var recommendations = await engine.ComputeRecommendations(projectPath, new Dictionary<string, string>());
            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID);
            var environmentTypeOptionSetting = beanstalkRecommendation.Recipe.OptionSettings.First(optionSetting => optionSetting.Id.Equals("EnvironmentType"));

            environmentTypeOptionSetting.SetValueOverride("LoadBalanced");
            Assert.Equal("LoadBalanced", beanstalkRecommendation.GetOptionSettingValue(environmentTypeOptionSetting, false));
        }

        [Fact]
        public async Task ObjectMappingSetWithValue()
        {
            var projectPath = SystemIOUtilities.ResolvePath("WebAppNoDockerFile");
            var engine = new RecommendationEngine(new[] { RecipeLocator.FindRecipeDefinitionsPath() }, new Orchestrator.OrchestratorSession());
            var recommendations = await engine.ComputeRecommendations(projectPath, new Dictionary<string, string>());
            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID);
            var applicationIAMRoleOptionSetting = beanstalkRecommendation.Recipe.OptionSettings.First(optionSetting => optionSetting.Id.Equals("ApplicationIAMRole"));

            applicationIAMRoleOptionSetting.SetValueOverride(new IAMRoleTypeHintResponse {CreateNew = false, RoleArn = "role_arn"});

            var iamRoleTypeHintResponse = beanstalkRecommendation.GetOptionSettingValue<IAMRoleTypeHintResponse>(applicationIAMRoleOptionSetting, false);

            Assert.Equal("role_arn", iamRoleTypeHintResponse.RoleArn);
            Assert.False(iamRoleTypeHintResponse.CreateNew);
        }

        [Fact]
        public void ApplyProjectNameToSettings()
        {
            var projectPath = SystemIOUtilities.ResolvePath("WebAppNoDockerFile");

            var engine = new RecommendationEngine.RecommendationEngine(new[] { RecipeLocator.FindRecipeDefinitionsPath() });

            var recommendations = engine.ComputeRecommendations(projectPath, new Dictionary<string, string>());

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
            var engine = new RecommendationEngine(new[] { RecipeLocator.FindRecipeDefinitionsPath() }, new Orchestrator.OrchestratorSession());

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
            var projectPath = SystemIOUtilities.ResolvePath("WebAppNoDockerFile");
            var engine = new RecommendationEngine(new[] { RecipeLocator.FindRecipeDefinitionsPath() }, new Orchestrator.OrchestratorSession());
            var recommendations = await engine.ComputeRecommendations(projectPath, new());
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
            var projectPath = SystemIOUtilities.ResolvePath("WebAppWithDockerFile");
            var engine = new RecommendationEngine(new[] { RecipeLocator.FindRecipeDefinitionsPath() }, new Orchestrator.OrchestratorSession());
            var recommendations = await engine.ComputeRecommendations(projectPath, new ());
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
    }
}
