// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Recipes.Validation;
using AWS.Deploy.Orchestration;
using Moq;
using Should;
using Xunit;

namespace AWS.Deploy.CLI.Common.UnitTests.Recipes.Validation
{
    public class AppRunnerOptionSettingItemValidationTests
    {
        private readonly IOptionSettingHandler _optionSettingHandler;
        private readonly IServiceProvider _serviceProvider;

        public AppRunnerOptionSettingItemValidationTests()
        {
            _serviceProvider = new Mock<IServiceProvider>().Object;
            _optionSettingHandler = new OptionSettingHandler(new ValidatorFactory(_serviceProvider));
        }

        [Theory]
        [InlineData("abcdef1234", true)]
        [InlineData("abc123def45", true)]
        [InlineData("abc12-34-56_XZ", true)]
        [InlineData("abc_@1323", false)] //invalid character "@"
        [InlineData("123*&$_abc_", false)] //invalid characters
        [InlineData("-abc123def45", false)] // does not start with a letter or a number
        public void AppRunnerServiceNameValidationTests(string value, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "name", "description");
            // 4 to 40 letters (uppercase and lowercase), numbers, hyphens, and underscores are allowed.
            optionSettingItem.Validators.Add(GetRegexValidatorConfig("^([A-Za-z0-9][A-Za-z0-9_-]{3,39})$"));
            Validate(optionSettingItem, value, isValid);
        }

        [Theory]
        [InlineData("arn:aws:iam::123456789012:role/S3Access", true)]
        [InlineData("arn:aws-cn:iam::123456789012:role/service-role/MyServiceRole", true)]
        [InlineData("arn:aws:IAM::123456789012:role/S3Access", false)] //invalid uppercase IAM
        [InlineData("arn:aws:iam::1234567890124354:role/S3Access", false)] //invalid account ID
        [InlineData("arn:aws-new:iam::123456789012:role/S3Access", false)] // invalid aws partition
        [InlineData("arn:aws:iam::123456789012:role", false)] // missing resorce path
        public void RoleArnValidationTests(string value, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "name", "description");
            optionSettingItem.Validators.Add(GetRegexValidatorConfig("arn:(aws|aws-us-gov|aws-cn|aws-iso|aws-iso-b):iam::[0-9]{12}:(role|role/service-role)/[\\w+=,.@\\-/]{1,1000}"));
            Validate(optionSettingItem, value, isValid);
        }

        [Theory]
        [InlineData("arn:aws:kms:us-west-2:111122223333:key/1234abcd-12ab-34cd-56ef-1234567890ab", true)]
        [InlineData("arn:aws-us-gov:kms:us-west-2:111122223333:key/1234abcd-12ab-34cd-56ef-1234567890ab", true)]
        [InlineData("arn:aws:kms:111122223333:key/1234abcd-12ab-34cd-56ef-1234567890ab", false)] // missing region
        [InlineData("arn:aws:kms:us-east-1:11112222:key/1234abcd-12ab-34cd-56ef-1234567890ab", false)] // invalid account ID
        [InlineData("arn:aws:kms:us-west-2:111122223333:key", false)] // missing resource path
        [InlineData("arn:aws:kms:us-west-2:111122223333:key/1234abcd-12ab", false)] // invalid key-id structure
        public void KmsKeyArnValidationTests(string value, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "name", "description");
            optionSettingItem.Validators.Add(GetRegexValidatorConfig("arn:aws(-[\\w]+)*:kms:[a-z\\-]+-[0-9]{1}:[0-9]{12}:key/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}"));
            Validate(optionSettingItem, value, isValid);
        }

        private void Validate<T>(OptionSettingItem optionSettingItem, T value, bool isValid)
        {
            ValidationFailedException exception = null;
            try
            {
                _optionSettingHandler.SetOptionSettingValue(null, optionSettingItem, value);
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
    }
}
