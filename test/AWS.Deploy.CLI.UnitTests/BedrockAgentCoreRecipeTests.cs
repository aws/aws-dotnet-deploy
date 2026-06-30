// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.CLI.UnitTests.Utilities;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Recipes.Validation;
using AWS.Deploy.Orchestration;
using Moq;
using Xunit;

namespace AWS.Deploy.CLI.UnitTests
{
    public class BedrockAgentCoreRecipeTests
    {
        private const string RECIPE_ID = "AspNetAppBedrockAgentCore";

        private readonly IOptionSettingHandler _optionSettingHandler;
        private readonly IDirectoryManager _directoryManager;
        private readonly IFileManager _fileManager;
        private readonly IProjectDefinitionParser _projectDefinitionParser;

        public BedrockAgentCoreRecipeTests()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            _optionSettingHandler = new OptionSettingHandler(new ValidatorFactory(serviceProvider.Object));
            _directoryManager = new DirectoryManager();
            _fileManager = new FileManager();
            _projectDefinitionParser = new ProjectDefinitionParser(_fileManager, _directoryManager);
        }

        [Fact]
        public async Task RecipeIsRecommended_WhenProjectReferencesAgentCoreHosting()
        {
            var engine = await HelperFunctions.BuildRecommendationEngine(
                "AgentCoreWebApp",
                _fileManager,
                _directoryManager,
                "us-west-2",
                "123456789012",
                "default");

            var recommendations = await engine.ComputeRecommendations();

            var agentCoreRecommendation = recommendations.FirstOrDefault(x => x.Recipe.Id == RECIPE_ID);
            Assert.NotNull(agentCoreRecommendation);

            // Should be the top recommendation due to PriorityAdjustment: 200
            Assert.Equal(RECIPE_ID, recommendations.First().Recipe.Id);
        }

        [Fact]
        public async Task RecipeHasCorrectSettings()
        {
            var engine = await HelperFunctions.BuildRecommendationEngine(
                "AgentCoreWebApp",
                _fileManager,
                _directoryManager,
                "us-west-2",
                "123456789012",
                "default");

            var recommendations = await engine.ComputeRecommendations();
            var recommendation = recommendations.First(x => x.Recipe.Id == RECIPE_ID);

            var settingIds = recommendation.Recipe.OptionSettings.Select(s => s.Id).ToList();
            Assert.Contains("RuntimeName", settingIds);
            Assert.Contains("RequestHeaders", settingIds);
            Assert.Contains("RuntimeIAMRole", settingIds);
            Assert.Contains("AgentCoreEnvironmentVariables", settingIds);
        }

        [Fact]
        public async Task RecipeOnlySupportsArm64()
        {
            var engine = await HelperFunctions.BuildRecommendationEngine(
                "AgentCoreWebApp",
                _fileManager,
                _directoryManager,
                "us-west-2",
                "123456789012",
                "default");

            var recommendations = await engine.ComputeRecommendations();
            var recommendation = recommendations.First(x => x.Recipe.Id == RECIPE_ID);

            Assert.Single(recommendation.Recipe.SupportedArchitectures!);
            Assert.Equal(SupportedArchitecture.Arm64, recommendation.Recipe.SupportedArchitectures![0]);
        }

        [Fact]
        public async Task RecipeDeploymentBundleIsContainer()
        {
            var engine = await HelperFunctions.BuildRecommendationEngine(
                "AgentCoreWebApp",
                _fileManager,
                _directoryManager,
                "us-west-2",
                "123456789012",
                "default");

            var recommendations = await engine.ComputeRecommendations();
            var recommendation = recommendations.First(x => x.Recipe.Id == RECIPE_ID);

            Assert.Equal(DeploymentBundleTypes.Container, recommendation.Recipe.DeploymentBundle);
        }

        [Fact]
        public async Task RuntimeName_DefaultsToStackName()
        {
            var engine = await HelperFunctions.BuildRecommendationEngine(
                "AgentCoreWebApp",
                _fileManager,
                _directoryManager,
                "us-west-2",
                "123456789012",
                "default");

            var recommendations = await engine.ComputeRecommendations();
            var recommendation = recommendations.First(x => x.Recipe.Id == RECIPE_ID);

            var setting = recommendation.Recipe.OptionSettings.First(s => s.Id == "RuntimeName");
            Assert.Equal("{StackName}", setting.DefaultValue?.ToString());
        }

        [Fact]
        public async Task RuntimeName_ValidationRejectsInvalidNames()
        {
            var engine = await HelperFunctions.BuildRecommendationEngine(
                "AgentCoreWebApp",
                _fileManager,
                _directoryManager,
                "us-west-2",
                "123456789012",
                "default");

            var recommendations = await engine.ComputeRecommendations();
            var recommendation = recommendations.First(x => x.Recipe.Id == RECIPE_ID);

            // Name starting with number should fail validation
            await Assert.ThrowsAsync<ValidationFailedException>(
                () => _optionSettingHandler.SetOptionSettingValue(recommendation, "RuntimeName", "1InvalidName"));

            // Valid name should pass without throwing
            await _optionSettingHandler.SetOptionSettingValue(recommendation, "RuntimeName", "MyValidAgent");
        }

        [Fact]
        public async Task RuntimeIAMRole_DefaultsToCreateNew()
        {
            var engine = await HelperFunctions.BuildRecommendationEngine(
                "AgentCoreWebApp",
                _fileManager,
                _directoryManager,
                "us-west-2",
                "123456789012",
                "default");

            var recommendations = await engine.ComputeRecommendations();
            var recommendation = recommendations.First(x => x.Recipe.Id == RECIPE_ID);

            var roleSetting = recommendation.Recipe.OptionSettings.First(s => s.Id == "RuntimeIAMRole");
            var createNewSetting = roleSetting.ChildOptionSettings.First(s => s.Id == "CreateNew");
            Assert.Equal(true, createNewSetting.DefaultValue);
        }

        [Fact]
        public async Task RecipeIsNotRecommended_WhenProjectDoesNotReferenceAgentCore()
        {
            // WebAppWithDockerFile doesn't reference AWS.AgentCore.Hosting
            var engine = await HelperFunctions.BuildRecommendationEngine(
                "WebAppWithDockerFile",
                _fileManager,
                _directoryManager,
                "us-west-2",
                "123456789012",
                "default");

            var recommendations = await engine.ComputeRecommendations();

            // Recipe should still be in the list (Include: true on Fail) but NOT first
            var agentCoreRecommendation = recommendations.FirstOrDefault(x => x.Recipe.Id == RECIPE_ID);
            if (agentCoreRecommendation != null)
            {
                Assert.NotEqual(RECIPE_ID, recommendations.First().Recipe.Id);
            }
        }
    }
}
