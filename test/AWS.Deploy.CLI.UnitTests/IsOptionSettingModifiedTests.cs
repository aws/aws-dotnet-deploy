// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Runtime;
using AWS.Deploy.CLI.Common.UnitTests.Utilities;
using AWS.Deploy.Common;
using AWS.Deploy.Common.DeploymentManifest;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Recipes.Validation;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.RecommendationEngine;
using AWS.Deploy.Constants;
using Moq;
using Xunit;

namespace AWS.Deploy.CLI.UnitTests
{
    public class IsOptionSettingModifiedTests
    {
        private readonly IOptionSettingHandler _optionSettingHandler;
        private readonly RecommendationEngine _recommendationEngine;

        public IsOptionSettingModifiedTests()
        {
            var projectPath = SystemIOUtilities.ResolvePath("WebAppWithDockerFile");
            var directoryManager = new DirectoryManager();
            var fileManager = new FileManager();
            var deploymentManifestEngine = new DeploymentManifestEngine(directoryManager, fileManager);
            var orchestratorInteractiveService = new Mock<IOrchestratorInteractiveService>().Object;

            var parser = new ProjectDefinitionParser(fileManager, directoryManager);
            var awsCredentials = new Mock<AWSCredentials>();
            var orchestratorSession = new OrchestratorSession(
                parser.Parse(projectPath).Result,
                awsCredentials.Object,
                "us-west-2",
                "123456789012")
            {
                AWSProfileName = "default"
            };

            var validatorFactory = new TestValidatorFactory();
            _optionSettingHandler = new OptionSettingHandler(validatorFactory);
            var recipeHandler = new RecipeHandler(deploymentManifestEngine, orchestratorInteractiveService, directoryManager, fileManager, _optionSettingHandler, validatorFactory);
            _recommendationEngine = new RecommendationEngine(orchestratorSession, recipeHandler);
        }

        [Fact]
        public async Task IsOptionSettingModified_ElasticBeanstalk()
        {
            // ARRANGE - select recommendation
            var recommendations = _recommendationEngine.ComputeRecommendations().GetAwaiter().GetResult();
            var selectedRecommendation = recommendations.FirstOrDefault(x => string.Equals(x.Recipe.Id, "AspNetAppElasticBeanstalkLinux"));

            // ARRANGE - add replacement tokens
            selectedRecommendation.AddReplacementToken(RecipeIdentifier.REPLACE_TOKEN_LATEST_DOTNET_BEANSTALK_PLATFORM_ARN, "Latest-ARN");
            selectedRecommendation.AddReplacementToken(RecipeIdentifier.REPLACE_TOKEN_STACK_NAME, "MyAppStack");
            selectedRecommendation.AddReplacementToken(RecipeIdentifier.REPLACE_TOKEN_DEFAULT_VPC_ID, "vpc-12345678");

            // ARRANGE - modify settings so that they are different from their default values
            await _optionSettingHandler.SetOptionSettingValue(selectedRecommendation, "BeanstalkEnvironment.EnvironmentName", "MyEnvironment", skipValidation: true);
            await _optionSettingHandler.SetOptionSettingValue(selectedRecommendation, "EnvironmentType", "LoadBalanced", skipValidation: true);
            await _optionSettingHandler.SetOptionSettingValue(selectedRecommendation, "ApplicationIAMRole.CreateNew", false, skipValidation: true);
            await _optionSettingHandler.SetOptionSettingValue(selectedRecommendation, "ApplicationIAMRole.RoleArn", "MyRoleArn", skipValidation: true);
            await _optionSettingHandler.SetOptionSettingValue(selectedRecommendation, "XRayTracingSupportEnabled", true, skipValidation: true);
            var modifiedSettingsId = new HashSet<string>
            {
                "BeanstalkEnvironment", "EnvironmentType", "ApplicationIAMRole", "XRayTracingSupportEnabled"
            };

            // ACT and ASSERT
            foreach (var optionSetting in selectedRecommendation.GetConfigurableOptionSettingItems())
            {
                if (modifiedSettingsId.Contains(optionSetting.FullyQualifiedId))
                {
                    Assert.True(_optionSettingHandler.IsOptionSettingModified(selectedRecommendation, optionSetting));
                }
                else
                {
                    Assert.False(_optionSettingHandler.IsOptionSettingModified(selectedRecommendation, optionSetting));
                }
            }
        }

        [Fact]
        public async Task IsOptionSettingModified_ECSFargate()
        {
            // ARRANGE - select recommendation
            var recommendations = _recommendationEngine.ComputeRecommendations().GetAwaiter().GetResult();
            var selectedRecommendation = recommendations.FirstOrDefault(x => string.Equals(x.Recipe.Id, "AspNetAppEcsFargate"));

            // ARRANGE - add replacement tokens
            selectedRecommendation.AddReplacementToken(RecipeIdentifier.REPLACE_TOKEN_STACK_NAME, "MyAppStack");
            selectedRecommendation.AddReplacementToken(RecipeIdentifier.REPLACE_TOKEN_DEFAULT_VPC_ID, "vpc-12345678");

            // ARRANGE - modify settings so that they are different from their default values
            await _optionSettingHandler.SetOptionSettingValue(selectedRecommendation, "ECSServiceName", "MyECSService", skipValidation: true);
            await _optionSettingHandler.SetOptionSettingValue(selectedRecommendation, "DesiredCount", 10, skipValidation: true);
            await _optionSettingHandler.SetOptionSettingValue(selectedRecommendation, "ECSCluster.CreateNew", false, skipValidation: true);
            await _optionSettingHandler.SetOptionSettingValue(selectedRecommendation, "ECSCluster.ClusterArn", "MyClusterArn", skipValidation: true);
            await _optionSettingHandler.SetOptionSettingValue(selectedRecommendation, "DockerfilePath", "Path/To/DockerFile", skipValidation: true);
            await _optionSettingHandler.SetOptionSettingValue(selectedRecommendation, "DockerExecutionDirectory", "Path/To/ExecutionDirectory", skipValidation: true);
            var modifiedSettingsId = new HashSet<string>
            {
                "ECSServiceName", "DesiredCount", "ECSCluster", "DockerfilePath", "DockerExecutionDirectory"
            };

            // ACT and ASSERT
            foreach (var optionSetting in selectedRecommendation.GetConfigurableOptionSettingItems())
            {
                if (modifiedSettingsId.Contains(optionSetting.FullyQualifiedId))
                {
                    Assert.True(_optionSettingHandler.IsOptionSettingModified(selectedRecommendation, optionSetting));
                }
                else
                {
                    Assert.False(_optionSettingHandler.IsOptionSettingModified(selectedRecommendation, optionSetting));
                }
            }
        }
    }

    public class TestValidatorFactory : IValidatorFactory
    {
        public IOptionSettingItemValidator[] BuildValidators(OptionSettingItem optionSettingItem, Func<OptionSettingItemValidatorConfig, bool> filter = null) => new IOptionSettingItemValidator[0];
        public IRecipeValidator[] BuildValidators(RecipeDefinition recipeDefinition) => new IRecipeValidator[0];
    }
}
