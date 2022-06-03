// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Runtime;
using AWS.Deploy.CLI.UnitTests.Utilities;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Common.DeploymentManifest;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Recipes.Validation;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.RecommendationEngine;
using AWS.Deploy.Recipes;
using Moq;
using Xunit;

namespace AWS.Deploy.CLI.UnitTests
{
    public class GetOptionSettingTests
    {
        private readonly IOptionSettingHandler _optionSettingHandler;
        private readonly Mock<IAWSResourceQueryer> _awsResourceQueryer;
        private readonly Mock<IServiceProvider> _serviceProvider;
        private readonly IDeploymentManifestEngine _deploymentManifestEngine;
        private readonly IOrchestratorInteractiveService _orchestratorInteractiveService;
        private readonly IDirectoryManager _directoryManager;
        private readonly IFileManager _fileManager;
        private readonly IRecipeHandler _recipeHandler;

        public GetOptionSettingTests()
        {
            _awsResourceQueryer = new Mock<IAWSResourceQueryer>();
            _serviceProvider = new Mock<IServiceProvider>();
            _serviceProvider
                .Setup(x => x.GetService(typeof(IAWSResourceQueryer)))
                .Returns(_awsResourceQueryer.Object);
            _optionSettingHandler = new OptionSettingHandler(new ValidatorFactory(_serviceProvider.Object));
            _directoryManager = new DirectoryManager();
            _fileManager = new FileManager();
            _deploymentManifestEngine = new DeploymentManifestEngine(_directoryManager, _fileManager);
            _orchestratorInteractiveService = new TestToolOrchestratorInteractiveService();
            var serviceProvider = new Mock<IServiceProvider>();
            var validatorFactory = new ValidatorFactory(serviceProvider.Object);
            var optionSettingHandler = new OptionSettingHandler(validatorFactory);
            _recipeHandler = new RecipeHandler(_deploymentManifestEngine, _orchestratorInteractiveService, _directoryManager, _fileManager, optionSettingHandler);
            _optionSettingHandler = new OptionSettingHandler(new ValidatorFactory(_serviceProvider.Object));
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

            return new RecommendationEngine(session, _recipeHandler);
        }

        [Theory]
        [InlineData("ApplicationIAMRole.RoleArn", "RoleArn")]
        public async Task GetOptionSettingTests_OptionSettingExists(string jsonPath, string targetId)
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID);

            var optionSetting = _optionSettingHandler.GetOptionSetting(beanstalkRecommendation, jsonPath);

            Assert.NotNull(optionSetting);
            Assert.Equal(optionSetting.Id, targetId);
        }

        [Theory]
        [InlineData("ApplicationIAMRole.Foo")]
        public async Task GetOptionSettingTests_OptionSettingDoesNotExist(string jsonPath)
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID);

            Assert.Throws<OptionSettingItemDoesNotExistException>(() => _optionSettingHandler.GetOptionSetting(beanstalkRecommendation, jsonPath));
        }

        [Theory]
        [InlineData("ElasticBeanstalkManagedPlatformUpdates", "ManagedActionsEnabled", true, 3)]
        [InlineData("ElasticBeanstalkManagedPlatformUpdates", "ManagedActionsEnabled", false, 1)]
        public async Task GetOptionSettingTests_GetDisplayableChildren(string optionSetting, string childSetting, bool childValue, int displayableCount)
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID);

            var managedActionsEnabled = _optionSettingHandler.GetOptionSetting(beanstalkRecommendation, $"{optionSetting}.{childSetting}");
            await _optionSettingHandler.SetOptionSettingValue(beanstalkRecommendation, managedActionsEnabled, childValue);

            var elasticBeanstalkManagedPlatformUpdates = _optionSettingHandler.GetOptionSetting(beanstalkRecommendation, optionSetting);
            var elasticBeanstalkManagedPlatformUpdatesValue = _optionSettingHandler.GetOptionSettingValue<Dictionary<string, object>>(beanstalkRecommendation, elasticBeanstalkManagedPlatformUpdates);

            Assert.Equal(displayableCount, elasticBeanstalkManagedPlatformUpdatesValue.Count);
        }

        [Fact]
        public async Task GetOptionSettingTests_ListType_InvalidValue()
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var appRunnerRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_APPRUNNER_ID);

            var useVpcConnector = _optionSettingHandler.GetOptionSetting(appRunnerRecommendation, "VPCConnector.UseVPCConnector");
            var createNew = _optionSettingHandler.GetOptionSetting(appRunnerRecommendation, "VPCConnector.CreateNew");
            var vpcId = _optionSettingHandler.GetOptionSetting(appRunnerRecommendation, "VPCConnector.VpcId");
            var subnets = _optionSettingHandler.GetOptionSetting(appRunnerRecommendation, "VPCConnector.Subnets");
            var securityGroups = _optionSettingHandler.GetOptionSetting(appRunnerRecommendation, "VPCConnector.SecurityGroups");

            await _optionSettingHandler.SetOptionSettingValue(appRunnerRecommendation, useVpcConnector, true);
            await _optionSettingHandler.SetOptionSettingValue(appRunnerRecommendation, createNew, true);
            await _optionSettingHandler.SetOptionSettingValue(appRunnerRecommendation, vpcId, "vpc-1234abcd");
            await Assert.ThrowsAsync<ValidationFailedException>(async () => await _optionSettingHandler.SetOptionSettingValue(appRunnerRecommendation, subnets, new SortedSet<string>(){ "subnet1" }));
            await Assert.ThrowsAsync<ValidationFailedException>(async () => await _optionSettingHandler.SetOptionSettingValue(appRunnerRecommendation, securityGroups, new SortedSet<string>(){ "securityGroup1" }));
        }

        [Fact]
        public async Task GetOptionSettingTests_VPCConnector_DisplayableItems()
        {
            var SETTING_ID_VPCCONNECTOR = "VPCConnector";
            var SETTING_ID_USEVPCCONNECTOR = "UseVPCConnector";
            var SETTING_ID_CREATENEW = "CreateNew";
            var SETTING_ID_VPCCONNECTORID = "VpcConnectorId";
            var SETTING_ID_VPCID = "VpcId";
            var SETTING_ID_SUBNETS = "Subnets";
            var SETTING_ID_SECURITYGROUPS = "SecurityGroups";

            var engine = await BuildRecommendationEngine("WebAppWithDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var appRunnerRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_APPRUNNER_ID);

            var vpcConnector = _optionSettingHandler.GetOptionSetting(appRunnerRecommendation, SETTING_ID_VPCCONNECTOR);
            var vpcConnectorChildren = vpcConnector.ChildOptionSettings.Where(x => _optionSettingHandler.IsOptionSettingDisplayable(appRunnerRecommendation, x)).ToList();
            Assert.Single(vpcConnectorChildren);
            Assert.NotNull(vpcConnectorChildren.First(x => x.Id.Equals(SETTING_ID_USEVPCCONNECTOR)));

            var useVpcConnector = _optionSettingHandler.GetOptionSetting(appRunnerRecommendation, $"{SETTING_ID_VPCCONNECTOR}.{SETTING_ID_USEVPCCONNECTOR}");
            await _optionSettingHandler.SetOptionSettingValue(appRunnerRecommendation, useVpcConnector, true);
            vpcConnectorChildren = vpcConnector.ChildOptionSettings.Where(x => _optionSettingHandler.IsOptionSettingDisplayable(appRunnerRecommendation, x)).ToList();
            Assert.Equal(3, vpcConnectorChildren.Count);
            Assert.NotNull(vpcConnectorChildren.First(x => x.Id.Equals(SETTING_ID_USEVPCCONNECTOR)));
            Assert.NotNull(vpcConnectorChildren.First(x => x.Id.Equals(SETTING_ID_CREATENEW)));
            Assert.NotNull(vpcConnectorChildren.First(x => x.Id.Equals(SETTING_ID_VPCCONNECTORID)));

            var createNew = _optionSettingHandler.GetOptionSetting(appRunnerRecommendation, $"{SETTING_ID_VPCCONNECTOR}.{SETTING_ID_CREATENEW}");
            await _optionSettingHandler.SetOptionSettingValue(appRunnerRecommendation, createNew, true);
            vpcConnectorChildren = vpcConnector.ChildOptionSettings.Where(x => _optionSettingHandler.IsOptionSettingDisplayable(appRunnerRecommendation, x)).ToList();
            Assert.Equal(3, vpcConnectorChildren.Count);
            Assert.NotNull(vpcConnectorChildren.First(x => x.Id.Equals(SETTING_ID_USEVPCCONNECTOR)));
            Assert.NotNull(vpcConnectorChildren.First(x => x.Id.Equals(SETTING_ID_CREATENEW)));
            Assert.NotNull(vpcConnectorChildren.First(x => x.Id.Equals(SETTING_ID_VPCID)));

            var vpcId = _optionSettingHandler.GetOptionSetting(appRunnerRecommendation, $"{SETTING_ID_VPCCONNECTOR}.{SETTING_ID_VPCID}");
            await _optionSettingHandler.SetOptionSettingValue(appRunnerRecommendation, vpcId, "vpc-abcd1234");
            vpcConnectorChildren = vpcConnector.ChildOptionSettings.Where(x => _optionSettingHandler.IsOptionSettingDisplayable(appRunnerRecommendation, x)).ToList();
            Assert.Equal(5, vpcConnectorChildren.Count);
            Assert.NotNull(vpcConnectorChildren.First(x => x.Id.Equals(SETTING_ID_USEVPCCONNECTOR)));
            Assert.NotNull(vpcConnectorChildren.First(x => x.Id.Equals(SETTING_ID_CREATENEW)));
            Assert.NotNull(vpcConnectorChildren.First(x => x.Id.Equals(SETTING_ID_VPCID)));
            Assert.NotNull(vpcConnectorChildren.First(x => x.Id.Equals(SETTING_ID_SUBNETS)));
            Assert.NotNull(vpcConnectorChildren.First(x => x.Id.Equals(SETTING_ID_SECURITYGROUPS)));

            await _optionSettingHandler.SetOptionSettingValue(appRunnerRecommendation, useVpcConnector, false);
            vpcConnectorChildren = vpcConnector.ChildOptionSettings.Where(x => _optionSettingHandler.IsOptionSettingDisplayable(appRunnerRecommendation, x)).ToList();
            Assert.Single(vpcConnectorChildren);
            Assert.NotNull(vpcConnectorChildren.First(x => x.Id.Equals(SETTING_ID_USEVPCCONNECTOR)));
        }

        [Fact]
        public async Task GetOptionSettingTests_ListType()
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var appRunnerRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_APPRUNNER_ID);

            var subnets = _optionSettingHandler.GetOptionSetting(appRunnerRecommendation, "VPCConnector.Subnets");
            var emptySubnetsValue = _optionSettingHandler.GetOptionSettingValue(appRunnerRecommendation, subnets);

            await _optionSettingHandler.SetOptionSettingValue(appRunnerRecommendation, subnets, new SortedSet<string>(){ "subnet-1234abcd" });
            var subnetsValue = _optionSettingHandler.GetOptionSettingValue(appRunnerRecommendation, subnets);

            var emptySubnetsString = Assert.IsType<string>(emptySubnetsValue);
            Assert.True(string.IsNullOrEmpty(emptySubnetsString));

            var subnetsList = Assert.IsType<SortedSet<string>>(subnetsValue);
            Assert.Single(subnetsList);
            Assert.Contains("subnet-1234abcd", subnetsList);
        }
    }
}
