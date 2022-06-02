// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.Common.DeploymentManifest;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Recipes.Validation;
using AWS.Deploy.Orchestration.UnitTests.Utilities;
using AWS.Deploy.Recipes;
using Moq;
using Xunit;

namespace AWS.Deploy.Orchestration.UnitTests
{
    public class RecipeHandlerTests
    {
        private readonly Mock<IDeploymentManifestEngine> _deploymentManifestEngine;
        private readonly IOrchestratorInteractiveService _orchestratorInteractiveService;
        private readonly TestDirectoryManager _directoryManager;
        private readonly TestFileManager _fileManager;
        private readonly Mock<IServiceProvider> _serviceProvider;
        private readonly IValidatorFactory _validatorFactory;
        private readonly IOptionSettingHandler _optionSettingHandler;
        private readonly IRecipeHandler _recipeHandler;

        public RecipeHandlerTests()
        {
            _deploymentManifestEngine = new Mock<IDeploymentManifestEngine>();
            _orchestratorInteractiveService = new TestToolOrchestratorInteractiveService();
            _directoryManager = new TestDirectoryManager();
            _fileManager = new TestFileManager();
            _serviceProvider = new Mock<IServiceProvider>();
            _validatorFactory = new ValidatorFactory(_serviceProvider.Object);
            _optionSettingHandler = new OptionSettingHandler(_validatorFactory);
            _recipeHandler = new RecipeHandler(_deploymentManifestEngine.Object, _orchestratorInteractiveService, _directoryManager, _fileManager, _optionSettingHandler);
        }

        [Fact]
        public async Task DependencyTree_HappyPath()
        {
            _directoryManager.AddedFiles.Add(RecipeLocator.FindRecipeDefinitionsPath(), new HashSet<string> { "path1" });
            _fileManager.InMemoryStore.Add("path1", File.ReadAllText("./Recipes/OptionSettingCyclicDependency.recipe"));
            var recipeDefinitions = await _recipeHandler.GetRecipeDefinitions(null);

            var recipe = Assert.Single(recipeDefinitions);
            Assert.Equal("AspNetAppEcsFargate", recipe.Id);
            var ecsCluster = recipe.OptionSettings.First(x => x.Id.Equals("ECSCluster"));
            Assert.NotNull(ecsCluster);
            Assert.Empty(ecsCluster.Dependents);
            var ecsClusterCreateNew = ecsCluster.ChildOptionSettings.First(x => x.Id.Equals("CreateNew"));
            Assert.NotNull(ecsClusterCreateNew);
            Assert.Equal(2, ecsClusterCreateNew.Dependents.Count);
            Assert.NotNull(ecsClusterCreateNew.Dependents.First(x => x.Equals("ECSCluster.ClusterArn")));
            Assert.NotNull(ecsClusterCreateNew.Dependents.First(x => x.Equals("ECSCluster.NewClusterName")));
        }

        [Fact]
        public async Task DependencyTree_CyclicDependency()
        {
            _directoryManager.AddedFiles.Add(RecipeLocator.FindRecipeDefinitionsPath(), new HashSet<string> { "path1" });
            _fileManager.InMemoryStore.Add("path1", File.ReadAllText("./Recipes/OptionSettingCyclicDependency.recipe"));
            var recipeDefinitions = await _recipeHandler.GetRecipeDefinitions(null);

            var recipe = Assert.Single(recipeDefinitions);
            Assert.Equal("AspNetAppEcsFargate", recipe.Id);
            var iamRole = recipe.OptionSettings.First(x => x.Id.Equals("ApplicationIAMRole"));
            Assert.NotNull(iamRole);
            Assert.Empty(iamRole.Dependents);
            var iamRoleCreateNew = iamRole.ChildOptionSettings.First(x => x.Id.Equals("CreateNew"));
            Assert.NotNull(iamRoleCreateNew);
            Assert.Single(iamRoleCreateNew.Dependents);
            Assert.NotNull(iamRoleCreateNew.Dependents.First(x => x.Equals("ApplicationIAMRole.RoleArn")));
            var iamRoleRoleArn = iamRole.ChildOptionSettings.First(x => x.Id.Equals("RoleArn"));
            Assert.NotNull(iamRoleRoleArn);
            Assert.Single(iamRoleRoleArn.Dependents);
            Assert.NotNull(iamRoleRoleArn.Dependents.First(x => x.Equals("ApplicationIAMRole.CreateNew")));
        }
    }
}
