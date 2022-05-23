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
    public class ElasticBeanStalkOptionSettingItemValidationTests
    {
        private readonly IOptionSettingHandler _optionSettingHandler;
        private readonly IServiceProvider _serviceProvider;

        public ElasticBeanStalkOptionSettingItemValidationTests()
        {
            _serviceProvider = new Mock<IServiceProvider>().Object;
            _optionSettingHandler = new OptionSettingHandler(new ValidatorFactory(_serviceProvider));
        }

        [Theory]
        [InlineData("12345sas", true)]
        [InlineData("435&*abc@3123", true)]
        [InlineData("abc/123/#", false)] // invalid character forward slash(/)
        public void ApplicationNameValidationTest(string value, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "name", "description");
            //can contain up to 100 Unicode characters, not including forward slash (/).
            optionSettingItem.Validators.Add(GetRegexValidatorConfig("^[^/]{1,100}$"));
            Validate(optionSettingItem, value, isValid);
        }

        [Theory]
        [InlineData("abc-123", true)]
        [InlineData("abc-ABC-123-xyz", true)]
        [InlineData("abc", false)] // invalid length less than 4 characters.
        [InlineData("-12-abc", false)] // invalid character leading hyphen (-)
        public void EnvironmentNameValidationTest(string value, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "name", "description");
            // Must be from 4 to 40 characters in length. The name can contain only letters, numbers, and hyphens.
            // It can't start or end with a hyphen.
            optionSettingItem.Validators.Add(GetRegexValidatorConfig("^[a-zA-Z0-9][a-zA-Z0-9-]{2,38}[a-zA-Z0-9]$"));
            Validate(optionSettingItem, value, isValid);
        }

        [Theory]
        [InlineData("arn:aws:iam::123456789012:user/JohnDoe", true)]
        [InlineData("arn:aws:iam::123456789012:user/division_abc/subdivision_xyz/JaneDoe", true)]
        [InlineData("arn:aws:iam::123456789012:group/Developers", true)]
        [InlineData("arn:aws:iam::123456789012:role/S3Access", true)]
        [InlineData("arn:aws:IAM::123456789012:role/S3Access", false)] //invalid uppercase IAM
        [InlineData("arn:aws:iam::1234567890124354:role/S3Access", false)] //invalid account ID
        public void IAMRoleArnValidationTest(string value, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "name", "description");
            optionSettingItem.Validators.Add(GetRegexValidatorConfig("arn:.+:iam::[0-9]{12}:.+"));
            Validate(optionSettingItem, value, isValid);
        }

        [Theory]
        [InlineData("abcd1234", true)]
        [InlineData("abc 1234 xyz", true)]
        [InlineData(" abc 123-xyz", false)] //leading space
        [InlineData(" 123 abc-456 ", false)] //leading and trailing space
        public void EC2KeyPairValidationTest(string value, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "name", "description");
            // It allows all ASCII characters but without leading and trailing spaces
            optionSettingItem.Validators.Add(GetRegexValidatorConfig("^(?! ).+(?<! )$"));
            Validate(optionSettingItem, value, isValid);
        }

        [Theory]
        [InlineData("arn:aws:elasticbeanstalk:us-east-1:123456789012:platform/MyPlatform", true)]
        [InlineData("arn:aws-cn:elasticbeanstalk:us-west-1:123456789012:platform/MyPlatform", true)]
        [InlineData("arn:aws:elasticbeanstalk:eu-west-1:123456789012:platform/MyPlatform/v1.0", true)]
        [InlineData("arn:aws:elasticbeanstalk:us-west-2::platform/MyPlatform/v1.0", true)]
        [InlineData("arn:aws:elasticbeanstalk:us-east-1:123456789012:platform/", false)] //no resource path
        [InlineData("arn:aws:elasticbeanstack:eu-west-1:123456789012:platform/MyPlatform", false)] //Typo elasticbeanstack instead of elasticbeanstalk
        public void ElasticBeanstalkPlatformArnValidationTest(string value, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "name", "description");
            optionSettingItem.Validators.Add(GetRegexValidatorConfig("arn:[^:]+:elasticbeanstalk:[^:]+:[^:]*:platform/.+"));
            Validate(optionSettingItem, value, isValid);
        }

        [Theory]
        [InlineData("PT10M", true)]
        [InlineData("PT1H", true)]
        [InlineData("PT25S", true)]
        [InlineData("PT1H20M30S", true)]
        [InlineData("invalid", false)]
        [InlineData("PTB1H20M30S", false)]
        public void ElasticBeanstalkRollingUpdatesPauseTime(string value, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "name", "description");
            optionSettingItem.Validators.Add(GetRegexValidatorConfig("^P([0-9]+(?:[,\\.][0-9]+)?Y)?([0-9]+(?:[,\\.][0-9]+)?M)?([0-9]+(?:[,\\.][0-9]+)?D)?(?:T([0-9]+(?:[,\\.][0-9]+)?H)?([0-9]+(?:[,\\.][0-9]+)?M)?([0-9]+(?:[,\\.][0-9]+)?S)?)?$"));
            Validate(optionSettingItem, value, isValid);
        }

        [Theory]
        [InlineData("", true)]
        [InlineData("--no-restore --nologo --framework net5.0", true)]
        [InlineData("-o dir", false)]                   // -o or --output is reserved by the deploy tool
        [InlineData("--output dir", false)]
        [InlineData("-c Release", false)]               // -c or --configuration is controlled by DotnetPublishBuildConfiguration instead
        [InlineData("--configuration Release", false)]
        [InlineData("--self-contained true", false)]    // --self-contained is controlled by SelfContainedBuild instead
        [InlineData("--no-self-contained", false)]
        public void DotnetPublishArgsValidationTest(string value, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "name", "description");
            optionSettingItem.Validators.Add(new OptionSettingItemValidatorConfig
            {
                ValidatorType = OptionSettingItemValidatorList.DotnetPublishArgs
            });

            Validate(optionSettingItem, value, isValid);
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

        private OptionSettingItemValidatorConfig GetRangeValidatorConfig(int min, int max)
        {
            var rangeValidatorConfig = new OptionSettingItemValidatorConfig
            {
                ValidatorType = OptionSettingItemValidatorList.Range,
                Configuration = new RangeValidator
                {
                    Min = min,
                    Max = max
                }
            };
            return rangeValidatorConfig;
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

    }
}
