// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.ElasticBeanstalk.Model;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Data;
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
        private readonly Mock<IAWSResourceQueryer> _awsResourceQueryer;
        private readonly Mock<IServiceProvider> _serviceProvider;

        public ElasticBeanStalkOptionSettingItemValidationTests()
        {
            _awsResourceQueryer = new Mock<IAWSResourceQueryer>();
            _serviceProvider = new Mock<IServiceProvider>();
            _serviceProvider
                .Setup(x => x.GetService(typeof(IAWSResourceQueryer)))
                .Returns(_awsResourceQueryer.Object);
            _optionSettingHandler = new OptionSettingHandler(new ValidatorFactory(_serviceProvider.Object));
        }

        [Theory]
        [InlineData("12345sas", true)]
        [InlineData("435&*abc@3123", true)]
        [InlineData("abc/123/#", false)] // invalid character forward slash(/)
        public async Task ApplicationNameValidationTest(string value, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "fullyQualifiedId", "name", "description");
            //can contain up to 100 Unicode characters, not including forward slash (/).
            optionSettingItem.Validators.Add(GetRegexValidatorConfig("^[^/]{1,100}$"));
            await Validate(optionSettingItem, value, isValid);
        }

        [Theory]
        [InlineData("Sun:00:00", true)]
        [InlineData("sun:00:00", false)]
        [InlineData("Suns:00:00", false)]
        [InlineData("Mon:23:59", true)]
        [InlineData("Mon:24:00", false)]
        [InlineData("Mon:00:60", false)]
        [InlineData("", false)]
        [InlineData("test", false)]
        public async Task PreferredStartTimeValidationTest(string value, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "fullyQualifiedId", "name", "description");
            // valid examples are in the 'day:hour:minute' pattern such as 'Sun:00:00'
            optionSettingItem.Validators.Add(GetRegexValidatorConfig("^(Mon|Tue|Wed|Thu|Fri|Sat|Sun):(0[0-9]|1\\d|2[0-3]):(0[0-9]|1\\d|2\\d|3\\d|4\\d|5\\d)$"));
            await Validate(optionSettingItem, value, isValid);
        }

        [Theory]
        [InlineData("abc-123", true)]
        [InlineData("abc-ABC-123-xyz", true)]
        [InlineData("abc", false)] // invalid length less than 4 characters.
        [InlineData("-12-abc", false)] // invalid character leading hyphen (-)
        public async Task EnvironmentNameValidationTest(string value, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "fullyQualifiedId", "name", "description");
            // Must be from 4 to 40 characters in length. The name can contain only letters, numbers, and hyphens.
            // It can't start or end with a hyphen.
            optionSettingItem.Validators.Add(GetRegexValidatorConfig("^[a-zA-Z0-9][a-zA-Z0-9-]{2,38}[a-zA-Z0-9]$"));
            await Validate(optionSettingItem, value, isValid);
        }

        [Theory]
        [InlineData("arn:aws:iam::123456789012:user/JohnDoe", true)]
        [InlineData("arn:aws:iam::123456789012:user/division_abc/subdivision_xyz/JaneDoe", true)]
        [InlineData("arn:aws:iam::123456789012:group/Developers", true)]
        [InlineData("arn:aws:iam::123456789012:role/S3Access", true)]
        [InlineData("arn:aws:IAM::123456789012:role/S3Access", false)] //invalid uppercase IAM
        [InlineData("arn:aws:iam::1234567890124354:role/S3Access", false)] //invalid account ID
        public async Task IAMRoleArnValidationTest(string value, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "fullyQualifiedId", "name", "description");
            optionSettingItem.Validators.Add(GetRegexValidatorConfig("arn:.+:iam::[0-9]{12}:.+"));
            await Validate(optionSettingItem, value, isValid);
        }

        [Theory]
        [InlineData("abcd1234", true)]
        [InlineData("abc 1234 xyz", true)]
        [InlineData(" abc 123-xyz", false)] //leading space
        [InlineData(" 123 abc-456 ", false)] //leading and trailing space
        public async Task EC2KeyPairValidationTest(string value, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "fullyQualifiedId", "name", "description");
            // It allows all ASCII characters but without leading and trailing spaces
            optionSettingItem.Validators.Add(GetRegexValidatorConfig("^(?! ).+(?<! )$"));
            await Validate(optionSettingItem, value, isValid);
        }

        [Theory]
        [InlineData("arn:aws:elasticbeanstalk:us-east-1:123456789012:platform/MyPlatform", true)]
        [InlineData("arn:aws-cn:elasticbeanstalk:us-west-1:123456789012:platform/MyPlatform", true)]
        [InlineData("arn:aws:elasticbeanstalk:eu-west-1:123456789012:platform/MyPlatform/v1.0", true)]
        [InlineData("arn:aws:elasticbeanstalk:us-west-2::platform/MyPlatform/v1.0", true)]
        [InlineData("arn:aws:elasticbeanstalk:us-east-1:123456789012:platform/", false)] //no resource path
        [InlineData("arn:aws:elasticbeanstack:eu-west-1:123456789012:platform/MyPlatform", false)] //Typo elasticbeanstack instead of elasticbeanstalk
        public async Task ElasticBeanstalkPlatformArnValidationTest(string value, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "fullyQualifiedId", "name", "description");
            optionSettingItem.Validators.Add(GetRegexValidatorConfig("arn:[^:]+:elasticbeanstalk:[^:]+:[^:]*:platform/.+"));
            await Validate(optionSettingItem, value, isValid);
        }

        [Theory]
        [InlineData("PT10M", true)]
        [InlineData("PT1H", true)]
        [InlineData("PT25S", true)]
        [InlineData("PT1H20M30S", true)]
        [InlineData("invalid", false)]
        [InlineData("PTB1H20M30S", false)]
        public async Task ElasticBeanstalkRollingUpdatesPauseTime(string value, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "fullyQualifiedId", "name", "description");
            optionSettingItem.Validators.Add(GetRegexValidatorConfig("^P([0-9]+(?:[,\\.][0-9]+)?Y)?([0-9]+(?:[,\\.][0-9]+)?M)?([0-9]+(?:[,\\.][0-9]+)?D)?(?:T([0-9]+(?:[,\\.][0-9]+)?H)?([0-9]+(?:[,\\.][0-9]+)?M)?([0-9]+(?:[,\\.][0-9]+)?S)?)?$"));
            await Validate(optionSettingItem, value, isValid);
        }

        [Fact]
        public async Task ExistingApplicationNameValidationTest_Valid()
        {
            _awsResourceQueryer.Setup(x => x.ListOfElasticBeanstalkApplications(It.IsAny<string>())).ReturnsAsync(new List<ApplicationDescription> { });
            var optionSettingItem = new OptionSettingItem("id", "fullyQualifiedId", "name", "description");
            optionSettingItem.Validators.Add(GetExistingResourceValidatorConfig("AWS::ElasticBeanstalk::Application"));
            await Validate(optionSettingItem, "WebApp1", true);
        }

        [Fact]
        public async Task ExistingApplicationNameValidationTest_Invalid()
        {
            _awsResourceQueryer.Setup(x => x.ListOfElasticBeanstalkApplications(It.IsAny<string>())).ReturnsAsync(new List<ApplicationDescription> { new ApplicationDescription { ApplicationName = "WebApp1" } });
            var optionSettingItem = new OptionSettingItem("id", "fullyQualifiedId", "name", "description");
            optionSettingItem.Validators.Add(GetExistingResourceValidatorConfig("AWS::ElasticBeanstalk::Application"));
            await Validate(optionSettingItem, "WebApp1", false);
        }

        [Fact]
        public async Task ExistingEnvironmentNameValidationTest_Valid()
        {
            _awsResourceQueryer.Setup(x => x.ListOfElasticBeanstalkEnvironments(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new List<EnvironmentDescription> { });
            var optionSettingItem = new OptionSettingItem("id", "fullyQualifiedId", "name", "description");
            optionSettingItem.Validators.Add(GetExistingResourceValidatorConfig("AWS::ElasticBeanstalk::Environment"));
            await Validate(optionSettingItem, "WebApp1", true);
        }

        [Fact]
        public async Task ExistingEnvironmentNameValidationTest_Invalid()
        {
            _awsResourceQueryer.Setup(x => x.ListOfElasticBeanstalkEnvironments(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new List<EnvironmentDescription> { new EnvironmentDescription { EnvironmentName = "WebApp1" } });
            var optionSettingItem = new OptionSettingItem("id", "fullyQualifiedId", "name", "description");
            optionSettingItem.Validators.Add(GetExistingResourceValidatorConfig("AWS::ElasticBeanstalk::Environment"));
            await Validate(optionSettingItem, "WebApp1", false);
        }

        /// <summary>
        /// Validates that <see cref="InstanceTypeValidator"/> treats an instance type as
        /// invalid when DescribeInstanceType is mocked to return an InstanceTypeInfo instance
        /// </summary>
        [Fact]
        public async Task EC2InstanceType_Valid()
        {
            var validInstanceType = "m5.large";
            _awsResourceQueryer.Setup(x => x.DescribeInstanceType(
                It.Is<string>(rawInstanceType => rawInstanceType.Equals(validInstanceType))))
                .ReturnsAsync(new InstanceTypeInfo());

            var optionSettingItem = new OptionSettingItem("id", "fullyQualifiedId", "name", "description");
            optionSettingItem.Validators.Add(new OptionSettingItemValidatorConfig
            {
                ValidatorType = OptionSettingItemValidatorList.InstanceType
            });

            await Validate(optionSettingItem, validInstanceType, true);
        }

        /// <summary>
        /// Validates that <see cref="InstanceTypeValidator"/> treats an instance type as
        /// invalid when DescribeInstanceType is mocked to return null
        /// </summary>
        [Fact]
        public async Task EC2InstanceType_Invalid()
        {
            var invalidInstanceType = "m5.superlarge";
            _awsResourceQueryer.Setup(x => x.DescribeInstanceType(
                It.Is<string>(rawInstanceType => rawInstanceType.Equals(invalidInstanceType))))
                .ReturnsAsync(() => null);

            var optionSettingItem = new OptionSettingItem("id", "fullyQualifiedId", "name", "description");
            optionSettingItem.Validators.Add(new OptionSettingItemValidatorConfig
            {
                ValidatorType = OptionSettingItemValidatorList.InstanceType
            });

            await Validate(optionSettingItem, invalidInstanceType, false);
        }

        private OptionSettingItemValidatorConfig GetExistingResourceValidatorConfig(string type)
        {
            var existingResourceValidatorConfig = new OptionSettingItemValidatorConfig
            {
                ValidatorType = OptionSettingItemValidatorList.ExistingResource,
                Configuration = new ExistingResourceValidator(_awsResourceQueryer.Object)
                {
                    ResourceType = type
                }
            };
            return existingResourceValidatorConfig;
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
        public async Task DotnetPublishArgsValidationTest(string value, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "fullyQualifiedId", "name", "description");
            optionSettingItem.Validators.Add(new OptionSettingItemValidatorConfig
            {
                ValidatorType = OptionSettingItemValidatorList.DotnetPublishArgs
            });

            await Validate(optionSettingItem, value, isValid);
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
