// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;
using Amazon.Runtime;
using AWS.Deploy.Common;
using AWS.Deploy.Common.DeploymentManifest;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes.Validation;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.RecommendationEngine;
using Moq;

namespace AWS.Deploy.CLI.UnitTests.Utilities
{
    public class HelperFunctions
    {
        public static async Task<RecommendationEngine> BuildRecommendationEngine(
            string testProjectName,
            IFileManager fileManager,
            IDirectoryManager directoryManager,
            string awsRegion,
            string awsAccountId,
            string awsProfile)
        {
            return await BuildRecommendationEngine(
                () => SystemIOUtilities.ResolvePath(testProjectName),
                fileManager,
                directoryManager,
                awsRegion,
                awsAccountId,
                awsProfile);
        }

        public static async Task<RecommendationEngine> BuildRecommendationEngine(
            Func<string> ResolvePath,
            IFileManager fileManager,
            IDirectoryManager directoryManager,
            string awsRegion,
            string awsAccountId,
            string awsProfile)
        {
            var fullPath = ResolvePath();

            var deploymentManifestEngine = new DeploymentManifestEngine(directoryManager, fileManager);
            var orchestratorInteractiveService = new TestToolOrchestratorInteractiveService();
            var serviceProvider = new Mock<IServiceProvider>();
            var validatorFactory = new ValidatorFactory(serviceProvider.Object);
            var optionSettingHandler = new OptionSettingHandler(validatorFactory);
            var recipeHandler = new RecipeHandler(deploymentManifestEngine, orchestratorInteractiveService, directoryManager, fileManager, optionSettingHandler, validatorFactory);

            var parser = new ProjectDefinitionParser(fileManager, directoryManager);
            var awsCredentials = new Mock<AWSCredentials>();
            var session = new OrchestratorSession(
                await parser.Parse(fullPath),
                awsCredentials.Object,
                awsRegion,
                awsAccountId)
            {
                AWSProfileName = awsProfile
            };

            return new RecommendationEngine(session, recipeHandler);
        }
    }
}
