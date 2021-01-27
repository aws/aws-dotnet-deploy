// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Recipes;
using Should;
using Xunit;

namespace AWS.Deploy.CLI.UnitTests
{
    public class RecommendationTests
    {
        private const string ASPNET_CORE_ASPNET_CORE_FARGATE_RECIPE_ID = "AspNetAppEcsFargate";
        private const string ASPNET_CORE_BEANSTALK_RECIPE_ID = "AspNetAppElasticBeanstalkLinux";

        private const string CONSOLE_APP_FARGATE_SERVICE_RECIPE_ID = "ConsoleAppEcsFargateService";
        private const string CONSOLE_APP_FARGATE_TASK_RECIPE_ID = "ConsoleAppEcsFargateTask";

        [Fact]
        public void WebAppNoDockerFileTest()
        {
            var projectPath = ResolvePath("WebAppNoDockerFile");

            var engine = new RecommendationEngine.RecommendationEngine(new[] { RecipeLocator.FindRecipeDefinitionsPath() });

            var recommendations = engine.ComputeRecommendations(projectPath);

            recommendations
                .Any(r => r.Recipe.Id == ASPNET_CORE_BEANSTALK_RECIPE_ID)
                .ShouldBeTrue("Failed to receive Recommendation: " + ASPNET_CORE_BEANSTALK_RECIPE_ID);

            recommendations
                .Any(r => r.Recipe.Id == ASPNET_CORE_ASPNET_CORE_FARGATE_RECIPE_ID)
                .ShouldBeTrue("Failed to receive Recommendation: " + ASPNET_CORE_ASPNET_CORE_FARGATE_RECIPE_ID);
        }

        [Fact]
        public void WebAppWithDockerFileTest()
        {
            var projectPath = ResolvePath("WebAppWithDockerFile");

            var engine = new RecommendationEngine.RecommendationEngine(new[] { RecipeLocator.FindRecipeDefinitionsPath() });

            var recommendations = engine.ComputeRecommendations(projectPath);

            recommendations
                .Any(r => r.Recipe.Id == ASPNET_CORE_ASPNET_CORE_FARGATE_RECIPE_ID)
                .ShouldBeTrue("Failed to receive Recommendation: " + ASPNET_CORE_ASPNET_CORE_FARGATE_RECIPE_ID);

            recommendations
                .Any(r => r.Recipe.Id == ASPNET_CORE_BEANSTALK_RECIPE_ID)
                .ShouldBeTrue("Failed to receive Recommendation: " + ASPNET_CORE_BEANSTALK_RECIPE_ID);
        }

        [Fact]
        public void MessageProcessingAppTest()
        {
            var projectPath = ResolvePath("MessageProcessingApp");

            var engine = new RecommendationEngine.RecommendationEngine(new[] { RecipeLocator.FindRecipeDefinitionsPath() });

            var recommendations = engine.ComputeRecommendations(projectPath);

            recommendations
                .Any(r => r.Recipe.Id == CONSOLE_APP_FARGATE_SERVICE_RECIPE_ID)
                .ShouldBeTrue("Failed to receive Recommendation: " + CONSOLE_APP_FARGATE_SERVICE_RECIPE_ID);

            recommendations
                .Any(r => r.Recipe.Id == CONSOLE_APP_FARGATE_TASK_RECIPE_ID)
                .ShouldBeTrue("Failed to receive Recommendation: " + CONSOLE_APP_FARGATE_TASK_RECIPE_ID);
        }


        [Fact]
        public void ValueMappingWithDefaultValue()
        {
            var projectPath = ResolvePath("WebAppNoDockerFile");
            var engine = new RecommendationEngine.RecommendationEngine(new[] { RecipeLocator.FindRecipeDefinitionsPath() });
            var recommendations = engine.ComputeRecommendations(projectPath);
            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == ASPNET_CORE_BEANSTALK_RECIPE_ID);

            Assert.Equal("SingleInstance", beanstalkRecommendation.GetOptionSettingValue("EnvironmentType", false));
        }

        [Fact]
        public void ValueMappingSetWithAllowedValues()
        {
            var projectPath = ResolvePath("WebAppNoDockerFile");
            var engine = new RecommendationEngine.RecommendationEngine(new[] { RecipeLocator.FindRecipeDefinitionsPath() });
            var recommendations = engine.ComputeRecommendations(projectPath);
            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == ASPNET_CORE_BEANSTALK_RECIPE_ID);

            beanstalkRecommendation.SetOverrideOptionSettingValue("EnvironmentType", "Load Balanced");
            Assert.Equal("LoadBalanced", beanstalkRecommendation.GetOptionSettingValue("EnvironmentType", false));
        }

        [Fact]
        public void ValueMappingSetWithValue()
        {
            var projectPath = ResolvePath("WebAppNoDockerFile");
            var engine = new RecommendationEngine.RecommendationEngine(new[] { RecipeLocator.FindRecipeDefinitionsPath() });
            var recommendations = engine.ComputeRecommendations(projectPath);
            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == ASPNET_CORE_BEANSTALK_RECIPE_ID);

            beanstalkRecommendation.SetOverrideOptionSettingValue("EnvironmentType", "LoadBalanced");
            Assert.Equal("LoadBalanced", beanstalkRecommendation.GetOptionSettingValue("EnvironmentType", false));
        }


        private string ResolvePath(string projectName)
        {
            var testsPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            while (testsPath != null && !string.Equals(new DirectoryInfo(testsPath).Name, "test", StringComparison.OrdinalIgnoreCase))
            {
                testsPath = Directory.GetParent(testsPath).FullName;
            }

            return Path.Combine(testsPath, "..", "testapps", projectName);
        }


        [Theory]
        [MemberData(nameof(ShouldIncludeTestCases))]
        public void ShouldIncludeTests(RuleEffect effect, bool testPass, bool expectedResult)
        {
            var engine = new RecommendationEngine.RecommendationEngine(new[] { RecipeLocator.FindRecipeDefinitionsPath() });

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

                // Postive rule to adjust priority
                new object[]{ new RuleEffect { Pass = new EffectOptions {PriorityAdjustment = 55 } }, true, true },
                new object[]{ new RuleEffect { Pass = new EffectOptions { PriorityAdjustment = 55 }, Fail = new EffectOptions { Include = true } }, false, true },

                // Negative rule to adjust priority
                new object[]{ new RuleEffect { Fail = new EffectOptions {PriorityAdjustment = -55 } }, true, true },
                new object[]{ new RuleEffect { Fail = new EffectOptions { PriorityAdjustment = -55 } }, false, true },
            };
    }
}
