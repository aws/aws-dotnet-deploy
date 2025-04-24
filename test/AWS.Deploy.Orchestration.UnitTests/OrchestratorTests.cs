// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Runtime;
using AWS.Deploy.Common;
using AWS.Deploy.Common.DeploymentManifest;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Recipes.Validation;
using AWS.Deploy.Orchestration.UnitTests.Utilities;
using Moq;
using Xunit;

namespace AWS.Deploy.Orchestration.UnitTests;

public class OrchestratorTests
{
    private readonly IRecipeHandler _recipeHandler;
    private OrchestratorSession _session;
    private readonly IDeploymentManifestEngine _deploymentManifestEngine;
    private readonly Mock<IOrchestratorInteractiveService> _orchestratorInteractiveService;
    private readonly IDirectoryManager _directoryManager;
    private readonly IFileManager _fileManager;

    public OrchestratorTests()
    {
        _directoryManager = new DirectoryManager();
        _fileManager = new FileManager();
        _deploymentManifestEngine = new DeploymentManifestEngine(_directoryManager, _fileManager);
        _orchestratorInteractiveService = new Mock<IOrchestratorInteractiveService>();
        var serviceProvider = new Mock<IServiceProvider>();
        var validatorFactory = new ValidatorFactory(serviceProvider.Object);
        var optionSettingHandler = new OptionSettingHandler(validatorFactory);
        _recipeHandler = new RecipeHandler(_deploymentManifestEngine, _orchestratorInteractiveService.Object, _directoryManager, _fileManager, optionSettingHandler, validatorFactory);
    }

    private async Task<RecommendationEngine.RecommendationEngine> BuildRecommendationEngine(string testProjectName)
    {
        var fullPath = SystemIOUtilities.ResolvePath(testProjectName);

        var parser = new ProjectDefinitionParser(new FileManager(), new DirectoryManager());
        var awsCredentials = new Mock<AWSCredentials>();
        _session = new OrchestratorSession(
            await parser.Parse(fullPath),
            awsCredentials.Object,
            "us-west-2",
            "123456789012")
        {
            AWSProfileName = "default"
        };

        return new RecommendationEngine.RecommendationEngine(_session, _recipeHandler);
    }

    [Fact]
    public async Task ApplyAllReplacementTokensTest()
    {
        var engine = await BuildRecommendationEngine("WebAppNoDockerFile");
        var recommendations = await engine.ComputeRecommendations();
        var recommendation = recommendations.First(r => r.Recipe.Id.Equals("AspNetAppElasticBeanstalkLinux"));
        var orchestrator = new Orchestrator(_session, _recipeHandler);

        recommendation.ReplacementTokens.Clear();
        recommendation.ReplacementTokens.Add(Constants.RecipeIdentifier.REPLACE_TOKEN_DEFAULT_ENVIRONMENT_ARCHITECTURE, true);

        await orchestrator.ApplyAllReplacementTokens(recommendation, "WebAppNoDockerFile");

        Assert.Equal(Constants.Recipe.DefaultSupportedArchitecture, recommendation.ReplacementTokens[Constants.RecipeIdentifier.REPLACE_TOKEN_DEFAULT_ENVIRONMENT_ARCHITECTURE]);
    }
}
