// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
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
using Newtonsoft.Json;
using Xunit;

namespace AWS.Deploy.CLI.UnitTests
{
    public class SetOptionSettingTests
    {
        private readonly List<Recommendation> _recommendations;
        private readonly IOptionSettingHandler _optionSettingHandler;
        private readonly Mock<IAWSResourceQueryer> _awsResourceQueryer;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDeploymentManifestEngine _deploymentManifestEngine;
        private readonly IOrchestratorInteractiveService _orchestratorInteractiveService;
        private readonly IDirectoryManager _directoryManager;
        private readonly IFileManager _fileManager;
        private readonly IRecipeHandler _recipeHandler;

        public SetOptionSettingTests()
        {
            var projectPath = SystemIOUtilities.ResolvePath("WebAppNoDockerFile");
            _directoryManager = new DirectoryManager();
            _fileManager = new FileManager();
            _deploymentManifestEngine = new DeploymentManifestEngine(_directoryManager, _fileManager);
            _orchestratorInteractiveService = new TestToolOrchestratorInteractiveService();
            var serviceProvider = new Mock<IServiceProvider>();
            var validatorFactory = new ValidatorFactory(serviceProvider.Object);
            var optionSettingHandler = new OptionSettingHandler(validatorFactory);
            _recipeHandler = new RecipeHandler(_deploymentManifestEngine, _orchestratorInteractiveService, _directoryManager, _fileManager, optionSettingHandler, validatorFactory);

            var parser = new ProjectDefinitionParser(new FileManager(), _directoryManager);
            var awsCredentials = new Mock<AWSCredentials>();
            var session =  new OrchestratorSession(
                parser.Parse(projectPath).Result,
                awsCredentials.Object,
                "us-west-2",
                "123456789012")
            {
                AWSProfileName = "default"
            };

            var engine = new RecommendationEngine(session, _recipeHandler);
            _recommendations = engine.ComputeRecommendations().GetAwaiter().GetResult();
            _awsResourceQueryer = new Mock<IAWSResourceQueryer>();
            var mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider.Setup(x => x.GetService(typeof(IDirectoryManager))).Returns(_directoryManager);
            mockServiceProvider
                .Setup(x => x.GetService(typeof(IAWSResourceQueryer)))
                .Returns(_awsResourceQueryer.Object);
            _serviceProvider = mockServiceProvider.Object;

            _optionSettingHandler = new OptionSettingHandler(new ValidatorFactory(_serviceProvider));
        }

        [Fact]
        public async Task SetOptionSettingTests_DisallowedValues()
        {
            var beanstalkApplication = new List<Amazon.ElasticBeanstalk.Model.ApplicationDescription> { new Amazon.ElasticBeanstalk.Model.ApplicationDescription { ApplicationName = "WebApp1"} };
            _awsResourceQueryer.Setup(x => x.ListOfElasticBeanstalkApplications(It.IsAny<string>())).ReturnsAsync(beanstalkApplication);
            var recommendation = _recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_LINUX_RECIPE_ID);

            var optionSetting = _optionSettingHandler.GetOptionSetting(recommendation, "BeanstalkApplication.ApplicationName");
            await Assert.ThrowsAsync<ValidationFailedException>(() => _optionSettingHandler.SetOptionSettingValue(recommendation, optionSetting, "WebApp1"));
        }

        /// <summary>
        /// This test is to make sure no exception is throw when we set a valid value.
        /// The values in AllowedValues are the only values allowed to be set.
        /// </summary>
        [Fact]
        public async Task SetOptionSettingTests_AllowedValues()
        {
            var recommendation = _recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_LINUX_RECIPE_ID);

            var optionSetting = recommendation.Recipe.OptionSettings.First(x => x.Id.Equals("EnvironmentType"));
            await _optionSettingHandler.SetOptionSettingValue(recommendation, optionSetting, optionSetting.AllowedValues.First());

            Assert.Equal(optionSetting.AllowedValues.First(), _optionSettingHandler.GetOptionSettingValue<string>(recommendation, optionSetting));
        }

        /// <summary>
        /// This test asserts that an exception will be thrown if we set an invalid value.
        /// _optionSetting.ValueMapping.Values contain display values and are not
        /// considered valid values to be set for an option setting. Only values
        /// in AllowedValues can be set. Any other value will throw an exception.
        /// </summary>
        [Fact]
        public async Task SetOptionSettingTests_MappedValues()
        {
            var recommendation = _recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_LINUX_RECIPE_ID);

            var optionSetting = recommendation.Recipe.OptionSettings.First(x => x.Id.Equals("EnvironmentType"));
            await Assert.ThrowsAsync<InvalidOverrideValueException>(async () => await _optionSettingHandler.SetOptionSettingValue(recommendation, optionSetting, optionSetting.ValueMapping.Values.First()));
        }

        [Fact]
        public async Task SetOptionSettingTests_KeyValueType()
        {
            var recommendation = _recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_LINUX_RECIPE_ID);

            var optionSetting = recommendation.Recipe.OptionSettings.First(x => x.Id.Equals("ElasticBeanstalkEnvironmentVariables"));
            var values = new Dictionary<string, string>() { { "key", "value" } };
            await _optionSettingHandler.SetOptionSettingValue(recommendation, optionSetting, values);

            Assert.Equal(values, _optionSettingHandler.GetOptionSettingValue<Dictionary<string, string>>(recommendation, optionSetting));
        }

        [Fact]
        public async Task SetOptionSettingTests_KeyValueType_String()
        {
            var recommendation = _recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_LINUX_RECIPE_ID);

            var optionSetting = recommendation.Recipe.OptionSettings.First(x => x.Id.Equals("ElasticBeanstalkEnvironmentVariables"));
            var dictionary = new Dictionary<string, string>() { { "key", "value" } };
            var dictionaryString = JsonConvert.SerializeObject(dictionary);
            await _optionSettingHandler.SetOptionSettingValue(recommendation, optionSetting, dictionaryString);

            Assert.Equal(dictionary, _optionSettingHandler.GetOptionSettingValue<Dictionary<string, string>>(recommendation, optionSetting));
        }

        [Fact]
        public async Task SetOptionSettingTests_KeyValueType_Error()
        {
            var recommendation = _recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_LINUX_RECIPE_ID);

            var optionSetting = recommendation.Recipe.OptionSettings.First(x => x.Id.Equals("ElasticBeanstalkEnvironmentVariables"));
            await Assert.ThrowsAsync<JsonReaderException>(async () => await _optionSettingHandler.SetOptionSettingValue(recommendation, optionSetting, "string"));
        }

        /// <summary>
        /// Verifies that calling SetOptionSettingValue for Docker-related settings
        /// also sets the corresponding value in recommendation.DeploymentBundle
        /// </summary>
        [Fact]
        public async Task DeploymentBundleWriteThrough_Docker()
        {
            var recommendation = _recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_APPRUNNER_ID);

            var dockerExecutionDirectory = SystemIOUtilities.ResolvePath("WebAppNoDockerFile");
            var dockerBuildArgs = "arg1=val1, arg2=val2";
            await _optionSettingHandler.SetOptionSettingValue(recommendation, _optionSettingHandler.GetOptionSetting(recommendation, "DockerExecutionDirectory"), dockerExecutionDirectory);
            await _optionSettingHandler.SetOptionSettingValue(recommendation, _optionSettingHandler.GetOptionSetting(recommendation, "DockerBuildArgs"), dockerBuildArgs);

            Assert.Equal(dockerExecutionDirectory, recommendation.DeploymentBundle.DockerExecutionDirectory);
            Assert.Equal(dockerBuildArgs, recommendation.DeploymentBundle.DockerBuildArgs);
        }

        /// <summary>
        /// Verifies that calling SetOptionSettingValue for dotnet publish settings
        /// also sets the corresponding value in recommendation.DeploymentBundle
        /// </summary>
        [Fact]
        public async Task DeploymentBundleWriteThrough_Dotnet()
        {
            var recommendation = _recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_LINUX_RECIPE_ID);

            var dotnetBuildConfiguration = "Debug";
            var dotnetPublishArgs = "--force --nologo";
            var selfContainedBuild = true;
            await _optionSettingHandler.SetOptionSettingValue(recommendation, _optionSettingHandler.GetOptionSetting(recommendation, "DotnetBuildConfiguration"), dotnetBuildConfiguration);
            await _optionSettingHandler.SetOptionSettingValue(recommendation, _optionSettingHandler.GetOptionSetting(recommendation, "DotnetPublishArgs"), dotnetPublishArgs);
            await _optionSettingHandler.SetOptionSettingValue(recommendation, _optionSettingHandler.GetOptionSetting(recommendation, "SelfContainedBuild"), selfContainedBuild);

            Assert.Equal(dotnetBuildConfiguration, recommendation.DeploymentBundle.DotnetPublishBuildConfiguration);
            Assert.Equal(dotnetPublishArgs, recommendation.DeploymentBundle.DotnetPublishAdditionalBuildArguments);
            Assert.True(recommendation.DeploymentBundle.DotnetPublishSelfContainedBuild);
        }
    }
}
