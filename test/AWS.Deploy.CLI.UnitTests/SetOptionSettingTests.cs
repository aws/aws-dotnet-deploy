// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using AWS.Deploy.CLI.UnitTests.Utilities;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Recipes;
using Xunit;

namespace AWS.Deploy.CLI.UnitTests
{
    public class SetOptionSettingTests
    {
        private readonly OptionSettingItem _optionSetting;

        public SetOptionSettingTests()
        {
            var projectPath = SystemIOUtilities.ResolvePath("WebAppNoDockerFile");
            var engine = new RecommendationEngine.RecommendationEngine(new[] { RecipeLocator.FindRecipeDefinitionsPath() });
            var recommendations = engine.ComputeRecommendations(projectPath, new Dictionary<string, string>());
            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID);

            _optionSetting = beanstalkRecommendation.Recipe.OptionSettings.First(x => x.Id.Equals("EnvironmentType"));
        }

        /// <summary>
        /// This test is to make sure no exception is throw when we set a valid value.
        /// The values in AllowedValues are the only values allowed to be set.
        /// </summary>
        [Fact]
        public void SetOptionSettingTests_AllowedValues()
        {
            _optionSetting.SetValueOverride(_optionSetting.AllowedValues.First());
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
            Assert.Throws<InvalidOverrideValueException>(() => _optionSetting.SetValueOverride(_optionSetting.ValueMapping.Values.First()));
        }
    }
}
