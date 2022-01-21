// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Runtime;
using AWS.Deploy.CLI.UnitTests.Utilities;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
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

        public SetOptionSettingTests()
        {
            var projectPath = SystemIOUtilities.ResolvePath("WebAppNoDockerFile");

            var parser = new ProjectDefinitionParser(new FileManager(), new DirectoryManager());
            var awsCredentials = new Mock<AWSCredentials>();
            var session =  new OrchestratorSession(
                parser.Parse(projectPath).Result,
                awsCredentials.Object,
                "us-west-2",
                "123456789012")
            {
                AWSProfileName = "default"
            };

            var engine = new RecommendationEngine(new[] { RecipeLocator.FindRecipeDefinitionsPath() }, session);
            _recommendations = engine.ComputeRecommendations().GetAwaiter().GetResult();
        }

        /// <summary>
        /// This test is to make sure no exception is throw when we set a valid value.
        /// The values in AllowedValues are the only values allowed to be set.
        /// </summary>
        [Fact]
        public void SetOptionSettingTests_AllowedValues()
        {
            var recommendation = _recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID);

            var optionSetting = recommendation.Recipe.OptionSettings.First(x => x.Id.Equals("EnvironmentType"));
            optionSetting.SetValueOverride(optionSetting.AllowedValues.First());

            Assert.Equal(optionSetting.AllowedValues.First(), recommendation.GetOptionSettingValue<string>(optionSetting));
        }

        /// <summary>
        /// This test asserts that an exception will be thrown if we set an invalid value.
        /// _optionSetting.ValueMapping.Values contain display values and are not
        /// considered valid values to be set for an option setting. Only values
        /// in AllowedValues can be set. Any other value will throw an exception.
        /// </summary>
        [Fact]
        public void SetOptionSettingTests_MappedValues()
        {
            var recommendation = _recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID);

            var optionSetting = recommendation.Recipe.OptionSettings.First(x => x.Id.Equals("EnvironmentType"));
            Assert.Throws<InvalidOverrideValueException>(() => optionSetting.SetValueOverride(optionSetting.ValueMapping.Values.First()));
        }

        [Fact]
        public void SetOptionSettingTests_KeyValueType()
        {
            var recommendation = _recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID);

            var optionSetting = recommendation.Recipe.OptionSettings.First(x => x.Id.Equals("ElasticBeanstalkEnvironmentVariables"));
            var values = new Dictionary<string, string>() { { "key", "value" } };
            optionSetting.SetValueOverride(values);

            Assert.Equal(values, recommendation.GetOptionSettingValue<Dictionary<string, string>>(optionSetting));
        }

        [Fact]
        public void SetOptionSettingTests_KeyValueType_String()
        {
            var recommendation = _recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID);

            var optionSetting = recommendation.Recipe.OptionSettings.First(x => x.Id.Equals("ElasticBeanstalkEnvironmentVariables"));
            var dictionary = new Dictionary<string, string>() { { "key", "value" } };
            var dictionaryString = JsonConvert.SerializeObject(dictionary);
            optionSetting.SetValueOverride(dictionaryString);

            Assert.Equal(dictionary, recommendation.GetOptionSettingValue<Dictionary<string, string>>(optionSetting));
        }

        [Fact]
        public void SetOptionSettingTests_KeyValueType_Error()
        {
            var recommendation = _recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID);

            var optionSetting = recommendation.Recipe.OptionSettings.First(x => x.Id.Equals("ElasticBeanstalkEnvironmentVariables"));
            Assert.Throws<JsonReaderException>(() => optionSetting.SetValueOverride("string"));
        }
    }
}
