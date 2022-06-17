// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using Amazon.CloudControlApi.Model;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Recipes.Validation;
using AWS.Deploy.Orchestration;
using Moq;
using Should;
using Xunit;
using ResourceNotFoundException = Amazon.CloudControlApi.Model.ResourceNotFoundException;
using Task = System.Threading.Tasks.Task;
using System.Collections.Generic;

namespace AWS.Deploy.CLI.Common.UnitTests.Recipes.Validation
{
    public class ECSFargateOptionSettingItemValidationTests
    {
        private readonly IOptionSettingHandler _optionSettingHandler;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDirectoryManager _directoryManager;
        private readonly Mock<IAWSResourceQueryer> _awsResourceQueryer;
        private readonly RecipeDefinition _recipe;
        private readonly Recommendation _recommendation;

        public ECSFargateOptionSettingItemValidationTests()
        {
            _awsResourceQueryer = new Mock<IAWSResourceQueryer>();
            _directoryManager = new TestDirectoryManager();
            var mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider.Setup(x => x.GetService(typeof(IDirectoryManager))).Returns(_directoryManager);
            mockServiceProvider
                .Setup(x => x.GetService(typeof(IAWSResourceQueryer)))
                .Returns(_awsResourceQueryer.Object);
            _serviceProvider = mockServiceProvider.Object;
            _optionSettingHandler = new OptionSettingHandler(new ValidatorFactory(_serviceProvider));

            _recipe = new RecipeDefinition("Fargate", "0.1", "Fargate", DeploymentTypes.CdkProject, DeploymentBundleTypes.Container, "", "", "", "", "");
            _recommendation = new Recommendation(_recipe, null, 100, new Dictionary<string, string>());
        }

        [Theory]
        [InlineData("arn:aws:ecs:us-east-1:012345678910:cluster/test", true)]
        [InlineData("arn:aws-cn:ecs:us-east-1:012345678910:cluster/test", true)]
        [InlineData("arb:aws:ecs:us-east-1:012345678910:cluster/test", false)] //typo arb instean of arn
        [InlineData("arn:aws:ecs:us-east-1:01234567891:cluster/test", false)] //invalid account ID
        [InlineData("arn:aws:ecs:us-east-1:012345678910:cluster", false)] //no cluster name
        [InlineData("arn:aws:ecs:us-east-1:012345678910:fluster/test", false)] //fluster instead of cluster
        public async Task ClusterArnValidationTests(string value, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "fullyQualifiedId", "name", "description");
            optionSettingItem.Validators.Add(GetRegexValidatorConfig("arn:[^:]+:ecs:[^:]*:[0-9]{12}:cluster/.+"));
            await Validate(optionSettingItem, value, isValid);
        }

        [Theory]
        [InlineData("abcdef1234", true)]
        [InlineData("abc123def45", true)]
        [InlineData("abc12-34-56-XZ", true)] 
        [InlineData("abc_@1323", false)] //invalid characters
        [InlineData("123*&$abc", false)] //invalid characters
        public async Task NewClusterNameValidationTests(string value, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "fullyQualifiedId", "name", "description");
            //up to 255 letters(uppercase and lowercase), numbers, underscores, and hyphens are allowed.
            optionSettingItem.Validators.Add(GetRegexValidatorConfig("^([A-Za-z0-9-]{1,255})$"));
            await Validate(optionSettingItem, value, isValid);
        }

        [Theory]
        [InlineData("abcdef1234", true)]
        [InlineData("abc123def45", true)]
        [InlineData("abc12-34-56_XZ", true)]
        [InlineData("abc_@1323", false)] //invalid character "@"
        [InlineData("123*&$_abc_", false)] //invalid characters
        public async Task ECSServiceNameValidationTests(string value, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "fullyQualifiedId", "name", "description");
            // Up to 255 letters (uppercase and lowercase), numbers, hyphens, and underscores are allowed.
            optionSettingItem.Validators.Add(GetRegexValidatorConfig("^([A-Za-z0-9_-]{1,255})$"));
            await Validate(optionSettingItem, value, isValid);
        }

        [Theory]
        [InlineData(5, true)]
        [InlineData(10, true)]
        [InlineData(-1, false)]
        [InlineData(6000, false)]
        [InlineData(1000, true)]
        public async Task DesiredCountValidationTests(int value, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "fullyQualifiedId", "name", "description");
            optionSettingItem.Validators.Add(GetRangeValidatorConfig(1, 5000));
            await Validate(optionSettingItem, value, isValid);
        }

        [Theory]
        [InlineData(5, false)]
        [InlineData(6, true)]
        public async Task HealthCheckInterval(int value, bool isValid)
        {
            var healthCheckInterval = new OptionSettingItem("healthCheckInterval", "fullyQualifiedId", "name", "description");
            var healthCheckTimeout = new OptionSettingItem("healthCheckTimeout", "fullyQualifiedId", "name", "description");
            _recipe.OptionSettings.Add(healthCheckInterval);
            _recipe.OptionSettings.Add(healthCheckTimeout);

            await _optionSettingHandler.SetOptionSettingValue(_recommendation, healthCheckTimeout, 5, true);
            healthCheckInterval.Validators.Add(GetComparisonValidatorConfig(ComparisonValidatorOperation.GreaterThan, "healthCheckTimeout"));

            await Validate(healthCheckInterval, value, isValid);
        }

        [Theory]
        [InlineData("arn:aws:iam::123456789012:user/JohnDoe", true)]
        [InlineData("arn:aws:iam::123456789012:user/division_abc/subdivision_xyz/JaneDoe", true)]
        [InlineData("arn:aws:iam::123456789012:group/Developers", true)]
        [InlineData("arn:aws:iam::123456789012:role/S3Access", true)]
        [InlineData("arn:aws:IAM::123456789012:role/S3Access", false)] //invalid uppercase IAM
        [InlineData("arn:aws:iam::1234567890124354:role/S3Access", false)] //invalid account ID
        public async Task RoleArnValidationTests(string value, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "fullyQualifiedId", "name", "description");
            optionSettingItem.Validators.Add(GetRegexValidatorConfig("arn:.+:iam::[0-9]{12}:.+"));
            await Validate(optionSettingItem, value, isValid);
        }

        [Theory]
        [InlineData("vpc-0123abcd", true)]
        [InlineData("vpc-ab12bf49", true)]
        [InlineData("vpc-ffffffffaaaabbbb1", true)]
        [InlineData("vpc-12345678", true)]
        [InlineData("ipc-456678", false)] //invalid prefix
        [InlineData("vpc-zzzzzzzz", false)] //invalid character z
        [InlineData("vpc-ffffffffaaaabbbb12", false)] //suffix length greater than 17
        public async Task VpcIdValidationTests(string value, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "fullyQualifiedId", "name", "description");
            //must start with the \"vpc-\" prefix,
            //followed by either 8 or 17 characters consisting of digits and letters(lower-case) from a to f.
            optionSettingItem.Validators.Add(GetRegexValidatorConfig("^vpc-([0-9a-f]{8}|[0-9a-f]{17})$"));
            await Validate(optionSettingItem, value, isValid);
        }

        [Theory]
        [InlineData("arn:aws:elasticloadbalancing:us-east-1:012345678910:loadbalancer/my-load-balancer", true)]
        [InlineData("arn:aws:elasticloadbalancing:us-east-1:012345678910:loadbalancer/app/my-load-balancer", true)]
        [InlineData("arn:aws:elasticloadbalancing:012345678910:elasticloadbalancing:loadbalancer/my-load-balancer", false)] //missing region
        [InlineData("arn:aws:elasticloadbalancing:012345678910:elasticloadbalancing:loadbalancer", false)] //missing resource path
        [InlineData("arn:aws:elasticloadbalancing:01234567891:elasticloadbalancing:loadbalancer", false)] //11 digit account ID
        public async Task LoadBalancerArnValidationTest(string value, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "fullyQualifiedId", "name", "description");
            optionSettingItem.Validators.Add(GetRegexValidatorConfig("arn:[^:]+:elasticloadbalancing:[^:]*:[0-9]{12}:loadbalancer/.+"));
            await Validate(optionSettingItem, value, isValid);
        }

        [Theory]
        [InlineData("/", true)]
        [InlineData("/Api/*", true)]
        [InlineData("/Api/Path/&*$-/@", true)]
        [InlineData("Api/Path", false)] // does not start with '/'
        [InlineData("/Api/Path/<dsd<>", false)] // contains invalid character '<' and '>'
        public async Task ListenerConditionPathPatternValidationTest(string value, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "fullyQualifiedId", "name", "description");
            optionSettingItem.Validators.Add(GetRegexValidatorConfig("^/[a-zA-Z0-9*?&_\\-.$/~\"'@:+]{0,127}$"));
            await Validate(optionSettingItem, value, isValid);
        }

        [Theory]
        [InlineData("myrepo123", true)]
        [InlineData("myrepo123.a/b", true)]
        [InlineData("MyRepo", false)] // cannot contain uppercase letters
        [InlineData("myrepo123@", false)] // cannot contain @
        [InlineData("myrepo123.a//b", false)] // cannot contain consecutive slashes.
        [InlineData("aa", true)]
        [InlineData("a", false)] //length cannot be less than 2
        [InlineData("", false)] // length cannot be less than 2
        [InlineData("reporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporeporepo", false)] // cannot be greater than 256 characters
        public async Task ECRRepositoryNameValidationTest(string value, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "fullyQualifiedId", "name", "description");
            optionSettingItem.Validators.Add(GetRegexValidatorConfig("^(?:[a-z0-9]+(?:[._-][a-z0-9]+)*/)*[a-z0-9]+(?:[._-][a-z0-9]+)*$"));
            optionSettingItem.Validators.Add(GetStringLengthValidatorConfig(2, 256));
            await Validate(optionSettingItem, value, isValid);
        }

        [Fact]
        public async Task ECSClusterNameValidationTest_Valid()
        {
            _awsResourceQueryer.Setup(x => x.GetCloudControlApiResource(It.IsAny<string>(), It.IsAny<string>())).Throws(new ResourceQueryException(DeployToolErrorCode.ResourceQuery, "", new ResourceNotFoundException("")));
            var optionSettingItem = new OptionSettingItem("id", "fullyQualifiedId", "name", "description");
            optionSettingItem.Validators.Add(GetExistingResourceValidatorConfig("AWS::ECS::Cluster"));
            await Validate(optionSettingItem, "WebApp1", true);
        }

        [Fact]
        public async Task VpcIdHasSubnetsInDifferentAZs_DifferentZones_Valid()
        {
            _awsResourceQueryer.Setup(x => x.DescribeSubnets(It.IsAny<string>())).ReturnsAsync(
                new List<Amazon.EC2.Model.Subnet> {
                    new Amazon.EC2.Model.Subnet { AvailabilityZoneId = "AZ1"},
                new Amazon.EC2.Model.Subnet { AvailabilityZoneId = "AZ2"}
                });
            var optionSettingItem = new OptionSettingItem("id", "fullyQualifiedId", "name", "description");
            optionSettingItem.Validators.Add(GetVPCSubnetsInDifferentAZsValidatorConfig());
            await Validate(optionSettingItem, "vpc-1234abcd", true);
        }

        [Fact]
        public async Task VpcIdHasSubnetsInDifferentAZs_SingleSubnet_Invalid()
        {
            _awsResourceQueryer.Setup(x => x.DescribeSubnets(It.IsAny<string>())).ReturnsAsync(
                new List<Amazon.EC2.Model.Subnet> {
                    new Amazon.EC2.Model.Subnet { AvailabilityZoneId = "AZ1"}
                });
            var optionSettingItem = new OptionSettingItem("id", "fullyQualifiedId", "name", "description");
            optionSettingItem.Validators.Add(GetVPCSubnetsInDifferentAZsValidatorConfig());
            await Validate(optionSettingItem, "vpc-1234abcd", false);
        }

        [Fact]
        public async Task VpcIdHasSubnetsInDifferentAZs_SingleZone_Invalid()
        {
            _awsResourceQueryer.Setup(x => x.DescribeSubnets(It.IsAny<string>())).ReturnsAsync(
                new List<Amazon.EC2.Model.Subnet> {
                    new Amazon.EC2.Model.Subnet { AvailabilityZoneId = "AZ1"},
                    new Amazon.EC2.Model.Subnet { AvailabilityZoneId = "AZ1"}
                });
            var optionSettingItem = new OptionSettingItem("id", "fullyQualifiedId", "name", "description");
            optionSettingItem.Validators.Add(GetVPCSubnetsInDifferentAZsValidatorConfig());
            await Validate(optionSettingItem, "vpc-1234abcd", false);
        }

        [Fact]
        public async Task ECSClusterNameValidationTest_Invalid()
        {
            var resource = new ResourceDescription { Identifier = "WebApp1" };
            _awsResourceQueryer.Setup(x => x.GetCloudControlApiResource(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(resource);
            var optionSettingItem = new OptionSettingItem("id", "fullyQualifiedId", "name", "description");
            optionSettingItem.Validators.Add(GetExistingResourceValidatorConfig("AWS::ECS::Cluster"));
            await Validate(optionSettingItem, "WebApp1", false);
        }

        [Theory]
        [InlineData("", true)]
        [InlineData("--build-arg arg=val --no-cache", true)]
        [InlineData("-t name:tag", false)]
        [InlineData("--tag name:tag", false)]
        [InlineData("-f file", false)]
        [InlineData("--file file", false)]
        public async Task DockerBuildArgsValidationTest(string value, bool isValid)
        {
            var optionSettingItem = new OptionSettingItem("id", "fullyQualifiedId", "name", "description");
            optionSettingItem.Validators.Add(new OptionSettingItemValidatorConfig
            {
                ValidatorType = OptionSettingItemValidatorList.DockerBuildArgs
            });

            await Validate(optionSettingItem, value, isValid);
        }

        [Fact]
        public async Task DockerExecutionDirectory_AbsoluteExists()
        {
            var optionSettingItem = new OptionSettingItem("id", "fullyQualifiedId", "name", "description");
            optionSettingItem.Validators.Add(new OptionSettingItemValidatorConfig
            {
                ValidatorType = OptionSettingItemValidatorList.DirectoryExists,
            });

            _directoryManager.CreateDirectory(Path.Join("C:", "project"));

            await Validate(optionSettingItem, Path.Join("C:", "project"), true);
        }

        [Fact]
        public async Task DockerExecutionDirectory_AbsoluteDoesNotExist()
        {
            var optionSettingItem = new OptionSettingItem("id", "fullyQualifiedId", "name", "description");
            optionSettingItem.Validators.Add(new OptionSettingItemValidatorConfig
            {
                ValidatorType = OptionSettingItemValidatorList.DirectoryExists,
            });

            await Validate(optionSettingItem, Path.Join("C:", "other_project"), false);
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

        private OptionSettingItemValidatorConfig GetVPCSubnetsInDifferentAZsValidatorConfig()
        {
            var vpcSubnetsInDifferentAZsValidatorConfig = new OptionSettingItemValidatorConfig
            {
                ValidatorType = OptionSettingItemValidatorList.VPCSubnetsInDifferentAZs,
                Configuration = new VPCSubnetsInDifferentAZsValidator(_awsResourceQueryer.Object)
            };
            return vpcSubnetsInDifferentAZsValidatorConfig;
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

        private OptionSettingItemValidatorConfig GetComparisonValidatorConfig(ComparisonValidatorOperation operation, string settingId)
        {
            var comparisonValidatorConfig = new OptionSettingItemValidatorConfig
            {
                ValidatorType = OptionSettingItemValidatorList.Comparison,
                Configuration = new ComparisonValidator(_optionSettingHandler)
                {
                    Operation = operation,
                    SettingId = settingId
                }
            };
            return comparisonValidatorConfig;
        }

        private OptionSettingItemValidatorConfig GetStringLengthValidatorConfig(int minLength, int maxLength)
        {
            var stringLengthValidatorConfig = new OptionSettingItemValidatorConfig
            {
                ValidatorType = OptionSettingItemValidatorList.StringLength,
                Configuration = new StringLengthValidator
                {
                    MinLength = minLength,
                    MaxLength = maxLength
                }
            };
            return stringLengthValidatorConfig;
        }

        private async Task Validate<T>(OptionSettingItem optionSettingItem, T value, bool isValid)
        {
            ValidationFailedException exception = null;
            try
            {
                await _optionSettingHandler.SetOptionSettingValue(_recommendation, optionSettingItem, value);
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
