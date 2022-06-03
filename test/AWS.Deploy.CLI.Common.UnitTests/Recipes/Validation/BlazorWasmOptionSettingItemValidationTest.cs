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
    public class BlazorWasmOptionSettingItemValidationTest
    {
        private readonly IOptionSettingHandler _optionSettingHandler;
        private readonly IServiceProvider _serviceProvider;

        public BlazorWasmOptionSettingItemValidationTest()
        {
            var mockServiceProvider = new Mock<IServiceProvider>();
            _serviceProvider = mockServiceProvider.Object;
            _optionSettingHandler = new OptionSettingHandler(new ValidatorFactory(_serviceProvider));
        }

        [Theory]
        [InlineData("https://www.abc.com/")]
        [InlineData("http://www.abc.com/")]
        [InlineData("http://abc.com")]
        [InlineData("http://abc.com.xyz")]
        [InlineData("http://abc.com/def")]
        [InlineData("http://abc.com//def")]
        [InlineData("http://api-uri")]
        [InlineData("customScheme://www.abc.com")]
        [InlineData("")] // Special case - It is possible that a URI specific option setting item is optional and can be null or empty.
        public async Task BackendApiUriValidationTests_ValidUri(string uri)
        {
            var optionSettingItem = new OptionSettingItem("id", "fullyQualifiedId", "name", "description");
            optionSettingItem.Validators.Add(GetUriValidatorConfig());
            await Validate(optionSettingItem, uri, true);
        }

        [Theory]
        [InlineData("//")]
        [InlineData("/")]
        [InlineData("abc.com")]
        [InlineData("abc")]
        [InlineData("http:www.abc.com")]
        [InlineData("http:/www.abc.com")]
        [InlineData("http//:www.abc.com")]
        [InlineData("://www.abc.com")]
        [InlineData("www.abc.com")]
        public async Task BackendApiUriValidationTests_InvalidUri(string uri)
        {
            var optionSettingItem = new OptionSettingItem("id", "fullyQualifiedId", "name", "description");
            optionSettingItem.Validators.Add(GetUriValidatorConfig());
            await Validate(optionSettingItem, uri, false);
        }

        [Theory]
        [InlineData("/*", true)]
        [InlineData("/api/*", true)]
        [InlineData("/api/myResource", true)]
        [InlineData("/", false)]
        [InlineData("", false)]
        [InlineData("abc", false)]
        public async Task BackendApiResourcePathValidationTests_InvalidUri(string resourcePath, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "fullyQualifiedId", "name", "description");
            optionSettingItem.Validators.Add(GetRegexValidatorConfig("^/\\S+$"));
            await Validate(optionSettingItem, resourcePath, isValid);
        }

        private OptionSettingItemValidatorConfig GetUriValidatorConfig()
        {
            var rangeValidatorConfig = new OptionSettingItemValidatorConfig
            {
                ValidatorType = OptionSettingItemValidatorList.Uri,
            };
            return rangeValidatorConfig;
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
            ValidationFailedException exception = null;
            try
            {
                await _optionSettingHandler.SetOptionSettingValue(null, optionSettingItem, value);
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
