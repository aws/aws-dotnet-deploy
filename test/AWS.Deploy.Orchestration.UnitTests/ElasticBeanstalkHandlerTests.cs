// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.ElasticBeanstalk.Model;
using Amazon.Runtime;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Common.DeploymentManifest;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Recipes.Validation;
using AWS.Deploy.Orchestration.ServiceHandlers;
using AWS.Deploy.Orchestration.UnitTests.Utilities;
using AWS.Deploy.Recipes;
using Moq;
using Xunit;

namespace AWS.Deploy.Orchestration.UnitTests
{
    public class ElasticBeanstalkHandlerTests
    {
        private readonly IOptionSettingHandler _optionSettingHandler;
        private readonly Mock<IAWSResourceQueryer> _awsResourceQueryer;
        private readonly Mock<IServiceProvider> _serviceProvider;
        private readonly IDeploymentManifestEngine _deploymentManifestEngine;
        private readonly Mock<IOrchestratorInteractiveService> _orchestratorInteractiveService;
        private readonly IDirectoryManager _directoryManager;
        private readonly IFileManager _fileManager;
        private readonly IRecipeHandler _recipeHandler;

        public ElasticBeanstalkHandlerTests()
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
            _orchestratorInteractiveService = new Mock<IOrchestratorInteractiveService>();
            var serviceProvider = new Mock<IServiceProvider>();
            var validatorFactory = new ValidatorFactory(serviceProvider.Object);
            var optionSettingHandler = new OptionSettingHandler(validatorFactory);
            _recipeHandler = new RecipeHandler(_deploymentManifestEngine, _orchestratorInteractiveService.Object, _directoryManager, _fileManager, optionSettingHandler);
            _optionSettingHandler = new OptionSettingHandler(new ValidatorFactory(_serviceProvider.Object));
        }

        [Fact]
        public async Task GetAdditionSettingsTest_DefaultValues()
        {
            // ARRANGE
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");
            var recommendations = await engine.ComputeRecommendations();
            var recommendation = recommendations.First(r => r.Recipe.Id.Equals(Constants.RecipeIdentifier.EXISTING_BEANSTALK_ENVIRONMENT_RECIPE_ID));

            var elasticBeanstalkHandler = new AWSElasticBeanstalkHandler(new Mock<IAWSClientFactory>().Object,
                new Mock<IOrchestratorInteractiveService>().Object,
                new Mock<IFileManager>().Object,
                _optionSettingHandler);

            // ACT
            var optionSettings = elasticBeanstalkHandler.GetEnvironmentConfigurationSettings(recommendation);

            // ASSERT
            Assert.Contains(optionSettings, x => IsEqual(new ConfigurationOptionSetting
            {
                OptionName = Constants.ElasticBeanstalk.EnhancedHealthReportingOptionName,
                Namespace = Constants.ElasticBeanstalk.EnhancedHealthReportingOptionNameSpace,
                Value = "enhanced"
            }, x));

            Assert.Contains(optionSettings, x => IsEqual(new ConfigurationOptionSetting
            {
                OptionName = Constants.ElasticBeanstalk.HealthCheckURLOptionName,
                Namespace = Constants.ElasticBeanstalk.HealthCheckURLOptionNameSpace,
                Value = "/"
            }, x));

            Assert.Contains(optionSettings, x => IsEqual(new ConfigurationOptionSetting
            {
                OptionName = Constants.ElasticBeanstalk.ProxyOptionName,
                Namespace = Constants.ElasticBeanstalk.ProxyOptionNameSpace,
                Value = "nginx"
            }, x));

            Assert.Contains(optionSettings, x => IsEqual(new ConfigurationOptionSetting
            {
                OptionName = Constants.ElasticBeanstalk.XRayTracingOptionName,
                Namespace = Constants.ElasticBeanstalk.XRayTracingOptionNameSpace,
                Value = "false"
            }, x));
        }

        [Fact]
        public async Task GetAdditionSettingsTest_CustomValues()
        {
            // ARRANGE
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");
            var recommendations = await engine.ComputeRecommendations();
            var recommendation = recommendations.First(r => r.Recipe.Id.Equals(Constants.RecipeIdentifier.EXISTING_BEANSTALK_ENVIRONMENT_RECIPE_ID));

            var elasticBeanstalkHandler = new AWSElasticBeanstalkHandler(new Mock<IAWSClientFactory>().Object,
                new Mock<IOrchestratorInteractiveService>().Object,
                new Mock<IFileManager>().Object,
                _optionSettingHandler);

            await _optionSettingHandler.SetOptionSettingValue(recommendation, _optionSettingHandler.GetOptionSetting(recommendation, Constants.ElasticBeanstalk.EnhancedHealthReportingOptionId), "basic");
            await _optionSettingHandler.SetOptionSettingValue(recommendation, _optionSettingHandler.GetOptionSetting(recommendation, Constants.ElasticBeanstalk.HealthCheckURLOptionId), "/url");
            await _optionSettingHandler.SetOptionSettingValue(recommendation, _optionSettingHandler.GetOptionSetting(recommendation, Constants.ElasticBeanstalk.ProxyOptionId), "none");
            await _optionSettingHandler.SetOptionSettingValue(recommendation, _optionSettingHandler.GetOptionSetting(recommendation, Constants.ElasticBeanstalk.XRayTracingOptionId), "true");

            // ACT
            var optionSettings = elasticBeanstalkHandler.GetEnvironmentConfigurationSettings(recommendation);

            // ASSERT
            Assert.Contains(optionSettings, x => IsEqual(new ConfigurationOptionSetting
            {
                OptionName = Constants.ElasticBeanstalk.EnhancedHealthReportingOptionName,
                Namespace = Constants.ElasticBeanstalk.EnhancedHealthReportingOptionNameSpace,
                Value = "basic"
            }, x));

            Assert.Contains(optionSettings, x => IsEqual(new ConfigurationOptionSetting
            {
                OptionName = Constants.ElasticBeanstalk.HealthCheckURLOptionName,
                Namespace = Constants.ElasticBeanstalk.HealthCheckURLOptionNameSpace,
                Value = "/url"
            }, x));

            Assert.Contains(optionSettings, x => IsEqual(new ConfigurationOptionSetting
            {
                OptionName = Constants.ElasticBeanstalk.ProxyOptionName,
                Namespace = Constants.ElasticBeanstalk.ProxyOptionNameSpace,
                Value = "none"
            }, x));

            Assert.Contains(optionSettings, x => IsEqual(new ConfigurationOptionSetting
            {
                OptionName = Constants.ElasticBeanstalk.XRayTracingOptionName,
                Namespace = Constants.ElasticBeanstalk.XRayTracingOptionNameSpace,
                Value = "true"
            }, x));
        }

        private async Task<RecommendationEngine.RecommendationEngine> BuildRecommendationEngine(string testProjectName)
        {
            var fullPath = SystemIOUtilities.ResolvePath(testProjectName);

            var parser = new ProjectDefinitionParser(new FileManager(), new DirectoryManager());
            var awsCredentials = new Mock<AWSCredentials>();
            var session = new OrchestratorSession(
                await parser.Parse(fullPath),
                awsCredentials.Object,
                "us-west-2",
                "123456789012")
            {
                AWSProfileName = "default"
            };

            return new RecommendationEngine.RecommendationEngine(session, _recipeHandler);
        }

        private bool IsEqual(ConfigurationOptionSetting expected, ConfigurationOptionSetting actual)
        {
            return string.Equals(expected.OptionName, actual.OptionName)
                && string.Equals(expected.Namespace, actual.Namespace)
                && string.Equals(expected.Value, actual.Value);
        }
    }
}
