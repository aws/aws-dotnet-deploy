// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Recipes.Validation;
using AWS.Deploy.Orchestration;
using Moq;
using Should;
using Xunit;

namespace AWS.Deploy.CLI.Common.UnitTests.Recipes.Validation
{
    public class PushToECROptionSettingItemValidationTests
    {
        private readonly IOptionSettingHandler _optionSettingHandler;
        private readonly IServiceProvider _serviceProvider;

        public PushToECROptionSettingItemValidationTests()
        {
            var mockServiceProvider = new Mock<IServiceProvider>();
            _serviceProvider = mockServiceProvider.Object;
            _optionSettingHandler = new OptionSettingHandler(new ValidatorFactory(_serviceProvider));
        }

        [Theory]
        [InlineData("abc123")]
        [InlineData("abc.123")]
        [InlineData("abc-123")]
        [InlineData("123abc")]
        [InlineData("1abc234")]
        [InlineData("1-234_abc")]
        [InlineData("1.234-abc")]
        [InlineData("267-234.abc_.-")]
        public async Task ImageTagValidationTests_ValidTags(string imageTag)
        {
            var optionSettingItem = new OptionSettingItem("id", "fullyQualifiedId", "name", "description");
            optionSettingItem.Validators.Add(GetRegexValidatorConfig("^[a-zA-Z0-9][a-zA-Z0-9.\\-_]{0,127}$"));
            await Validate(optionSettingItem, imageTag, true);
        }

        [Theory]
        [InlineData("-abc123")] // cannot start with a special character
        [InlineData("abc.$123")] // can only contain dot(.), hyphen(-), and underscore(_) as special characters
        [InlineData("abc@123")]// can only contain dot(.), hyphen(-), and underscore(_) as special characters
        [InlineData("")] // cannot be empty
        [InlineData("imagetagimagetagimagetagimagetagimagetagimagetagimagetagimagetagimagetagimagetagimagetagimagetagimagetagimagetagimagetagimagetagimagetag")] // cannot be greater than 128 characters
        public async Task ImageTagValidationTests_InValidTags(string imageTag)
        {
            var optionSettingItem = new OptionSettingItem("id", "fullyQualifiedId", "name", "description");
            optionSettingItem.Validators.Add(GetRegexValidatorConfig("^[a-zA-Z0-9][a-zA-Z0-9.\\-_]{0,127}$"));
            await Validate(optionSettingItem, imageTag, false);
        }

        private OptionSettingItemValidatorConfig GetRegexValidatorConfig(string regex)
        {
            var regexValidatorConfig = new OptionSettingItemValidatorConfig
            {
                ValidatorType = OptionSettingItemValidatorList.Regex,
                Configuration = new RegexValidator
                {
                    Regex = regex
                }
            };
            return regexValidatorConfig;
        }

        private async Task Validate<T>(OptionSettingItem optionSettingItem, T value, bool isValid)
        {
            ValidationFailedException? exception = null;
            try
            {
                await _optionSettingHandler.SetOptionSettingValue(null!, optionSettingItem, value!);
            }
            catch (ValidationFailedException e)
            {
                exception = e;
            }

            if (isValid)
                exception.ShouldBeNull();
            else
                exception.ShouldNotBeNull();
        }
    }
}


