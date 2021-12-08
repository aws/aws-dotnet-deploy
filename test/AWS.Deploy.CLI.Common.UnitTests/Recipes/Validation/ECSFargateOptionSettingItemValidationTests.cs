// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Recipes.Validation;
using Should;
using Xunit;
using Xunit.Abstractions;

namespace AWS.Deploy.CLI.Common.UnitTests.Recipes.Validation
{
    public class ECSFargateOptionSettingItemValidationTests
    {
        [Theory]
        [InlineData("arn:aws:ecs:us-east-1:012345678910:cluster/test", true)]
        [InlineData("arn:aws-cn:ecs:us-east-1:012345678910:cluster/test", true)]
        [InlineData("arb:aws:ecs:us-east-1:012345678910:cluster/test", false)] //typo arb instean of arn
        [InlineData("arn:aws:ecs:us-east-1:01234567891:cluster/test", false)] //invalid account ID
        [InlineData("arn:aws:ecs:us-east-1:012345678910:cluster", false)] //no cluster name
        [InlineData("arn:aws:ecs:us-east-1:012345678910:fluster/test", false)] //fluster instead of cluster
        public void ClusterArnValidationTests(string value, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "name", "description");
            optionSettingItem.Validators.Add(GetRegexValidatorConfig("arn:[^:]+:ecs:[^:]*:[0-9]{12}:cluster/.+"));
            Validate(optionSettingItem, value, isValid);
        }

        [Theory]
        [InlineData("abcdef1234", true)]
        [InlineData("abc123def45", true)]
        [InlineData("abc12-34-56-XZ", true)] 
        [InlineData("abc_@1323", false)] //invalid characters
        [InlineData("123*&$abc", false)] //invalid characters
        public void NewClusterNameValidationTests(string value, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "name", "description");
            //up to 255 letters(uppercase and lowercase), numbers, underscores, and hyphens are allowed.
            optionSettingItem.Validators.Add(GetRegexValidatorConfig("^([A-Za-z0-9-]{1,255})$"));
            Validate(optionSettingItem, value, isValid);
        }

        [Theory]
        [InlineData("abcdef1234", true)]
        [InlineData("abc123def45", true)]
        [InlineData("abc12-34-56_XZ", true)]
        [InlineData("abc_@1323", false)] //invalid character "@"
        [InlineData("123*&$_abc_", false)] //invalid characters
        public void ECSServiceNameValidationTests(string value, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "name", "description");
            // Up to 255 letters (uppercase and lowercase), numbers, hyphens, and underscores are allowed.
            optionSettingItem.Validators.Add(GetRegexValidatorConfig("^([A-Za-z0-9_-]{1,255})$"));
            Validate(optionSettingItem, value, isValid);
        }

        [Theory]
        [InlineData(5, true)]
        [InlineData(10, true)]
        [InlineData(-1, false)]
        [InlineData(6000, false)]
        [InlineData(1000, true)]
        public void DesiredCountValidationTests(int value, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "name", "description");
            optionSettingItem.Validators.Add(GetRangeValidatorConfig(1, 5000));
            Validate(optionSettingItem, value, isValid);
        }

        [Theory]
        [InlineData("arn:aws:iam::123456789012:user/JohnDoe", true)]
        [InlineData("arn:aws:iam::123456789012:user/division_abc/subdivision_xyz/JaneDoe", true)]
        [InlineData("arn:aws:iam::123456789012:group/Developers", true)]
        [InlineData("arn:aws:iam::123456789012:role/S3Access", true)]
        [InlineData("arn:aws:IAM::123456789012:role/S3Access", false)] //invalid uppercase IAM
        [InlineData("arn:aws:iam::1234567890124354:role/S3Access", false)] //invalid account ID
        public void RoleArnValidationTests(string value, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "name", "description");
            optionSettingItem.Validators.Add(GetRegexValidatorConfig("arn:.+:iam::[0-9]{12}:.+"));
            Validate(optionSettingItem, value, isValid);
        }

        [Theory]
        [InlineData("vpc-0123abcd", true)]
        [InlineData("vpc-ab12bf49", true)]
        [InlineData("vpc-ffffffffaaaabbbb1", true)]
        [InlineData("vpc-12345678", true)]
        [InlineData("ipc-456678", false)] //invalid prefix
        [InlineData("vpc-zzzzzzzz", false)] //invalid character z
        [InlineData("vpc-ffffffffaaaabbbb12", false)] //suffix length greater than 17
        public void VpcIdValidationTests(string value, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "name", "description");
            //must start with the \"vpc-\" prefix,
            //followed by either 8 or 17 characters consisting of digits and letters(lower-case) from a to f.
            optionSettingItem.Validators.Add(GetRegexValidatorConfig("^vpc-([0-9a-f]{8}|[0-9a-f]{17})$"));
            Validate(optionSettingItem, value, isValid);
        }

        [Theory]
        [InlineData("arn:aws:elasticloadbalancing:us-east-1:012345678910:loadbalancer/my-load-balancer", true)]
        [InlineData("arn:aws:elasticloadbalancing:us-east-1:012345678910:loadbalancer/app/my-load-balancer", true)]
        [InlineData("arn:aws:elasticloadbalancing:012345678910:elasticloadbalancing:loadbalancer/my-load-balancer", false)] //missing region
        [InlineData("arn:aws:elasticloadbalancing:012345678910:elasticloadbalancing:loadbalancer", false)] //missing resource path
        [InlineData("arn:aws:elasticloadbalancing:01234567891:elasticloadbalancing:loadbalancer", false)] //11 digit account ID
        public void LoadBalancerArnValidationTest(string value, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "name", "description");
            optionSettingItem.Validators.Add(GetRegexValidatorConfig("arn:[^:]+:elasticloadbalancing:[^:]*:[0-9]{12}:loadbalancer/.+"));
            Validate(optionSettingItem, value, isValid);
        }

        [Theory]
        [InlineData("/", true)]
        [InlineData("/Api/*", true)]
        [InlineData("/Api/Path/&*$-/@", true)]
        [InlineData("Api/Path", false)] // does not start with '/'
        [InlineData("/Api/Path/<dsd<>", false)] // contains invalid character '<' and '>'
        public void ListenerConditionPathPatternValidationTest(string value, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "name", "description");
            optionSettingItem.Validators.Add(GetRegexValidatorConfig("^/[a-zA-Z0-9*?&_\\-.$/~\"'@:+]{0,127}$"));
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
                optionSettingItem.SetValueOverride(value);
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
