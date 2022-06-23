// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.EC2.Model;
using AWS.Deploy.CLI.Commands.TypeHints;
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.CLI.UnitTests.Utilities;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Recipes.Validation;
using AWS.Deploy.Constants;
using AWS.Deploy.Orchestration;
using Moq;
using Xunit;

namespace AWS.Deploy.CLI.UnitTests.TypeHintCommands
{
    public class InstanceTypeCommandTest
    {
        private readonly Mock<IAWSResourceQueryer> _mockAWSResourceQueryer;
        private readonly IDirectoryManager _directoryManager;
        private readonly IOptionSettingHandler _optionSettingHandler;
        private readonly Mock<IAWSResourceQueryer> _awsResourceQueryer;
        private readonly Mock<IServiceProvider> _serviceProvider;

        public InstanceTypeCommandTest()
        {
            _mockAWSResourceQueryer = new Mock<IAWSResourceQueryer>();
            _directoryManager = new TestDirectoryManager();
            _awsResourceQueryer = new Mock<IAWSResourceQueryer>();
            _serviceProvider = new Mock<IServiceProvider>();
            _serviceProvider
                .Setup(x => x.GetService(typeof(IAWSResourceQueryer)))
                .Returns(_awsResourceQueryer.Object);
            _optionSettingHandler = new OptionSettingHandler(new ValidatorFactory(_serviceProvider.Object));
        }

        [Fact]
        public async Task WindowsGetResources()
        {
            var engine = await HelperFunctions.BuildRecommendationEngine(
                "WebAppWithDockerFile",
                new FileManager(),
                new DirectoryManager(),
                "us-west-2",
                "123456789012",
                "default"
                );

            var recommendations = await engine.ComputeRecommendations();

            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_WINDOWS_RECIPE_ID);

            var instanceTypeSetting = _optionSettingHandler.GetOptionSetting(beanstalkRecommendation, "InstanceType");

            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string>());
            var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager, _optionSettingHandler);
            var command = new WindowsInstanceTypeCommand(_mockAWSResourceQueryer.Object, consoleUtilities, _optionSettingHandler);

            _mockAWSResourceQueryer
                .Setup(x => x.ListOfAvailableInstanceTypes())
                .ReturnsAsync(new List<InstanceTypeInfo>()
                {
                    new InstanceTypeInfo()
                    {
                        InstanceType = "t1.any",
                        ProcessorInfo = new ProcessorInfo()
                        {
                            SupportedArchitectures = new List<string>{ EC2.FILTER_ARCHITECTURE_X86_64, EC2.FILTER_ARCHITECTURE_ARM64 }
                        }
                    },
                    new InstanceTypeInfo()
                    {
                        InstanceType = "t1.x86_64",
                        ProcessorInfo = new ProcessorInfo()
                        {
                            SupportedArchitectures = new List<string>{ EC2.FILTER_ARCHITECTURE_X86_64 }
                        }
                    },
                    new InstanceTypeInfo()
                    {
                        InstanceType = "t1.arm64",
                        ProcessorInfo = new ProcessorInfo()
                        {
                            SupportedArchitectures = new List<string>{ EC2.FILTER_ARCHITECTURE_ARM64 }
                        }
                    },
                });

            var resources = await command.GetResources(beanstalkRecommendation, instanceTypeSetting);

            Assert.Contains(resources.Rows, x => string.Equals("t1.any", x.SystemName, StringComparison.OrdinalIgnoreCase));
            Assert.Contains(resources.Rows, x => string.Equals("t1.x86_64", x.SystemName, StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(resources.Rows, x => string.Equals("t1.arm64", x.SystemName, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task LinuxGetResources()
        {
            var engine = await HelperFunctions.BuildRecommendationEngine(
                "WebAppWithDockerFile",
                new FileManager(),
                new DirectoryManager(),
                "us-west-2",
                "123456789012",
                "default"
                );

            var recommendations = await engine.ComputeRecommendations();

            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_LINUX_RECIPE_ID);

            var instanceTypeSetting = _optionSettingHandler.GetOptionSetting(beanstalkRecommendation, "InstanceType");

            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string>());
            var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager, _optionSettingHandler);
            var command = new LinuxInstanceTypeCommand(_mockAWSResourceQueryer.Object, consoleUtilities, _optionSettingHandler);

            _mockAWSResourceQueryer
                .Setup(x => x.ListOfAvailableInstanceTypes())
                .ReturnsAsync(new List<InstanceTypeInfo>()
                {
                    new InstanceTypeInfo()
                    {
                        InstanceType = "t1.any",
                        ProcessorInfo = new ProcessorInfo()
                        {
                            SupportedArchitectures = new List<string>{ EC2.FILTER_ARCHITECTURE_X86_64, EC2.FILTER_ARCHITECTURE_ARM64 }
                        }
                    },
                    new InstanceTypeInfo()
                    {
                        InstanceType = "t1.x86_64",
                        ProcessorInfo = new ProcessorInfo()
                        {
                            SupportedArchitectures = new List<string>{ EC2.FILTER_ARCHITECTURE_X86_64 }
                        }
                    },
                    new InstanceTypeInfo()
                    {
                        InstanceType = "t1.arm64",
                        ProcessorInfo = new ProcessorInfo()
                        {
                            SupportedArchitectures = new List<string>{ EC2.FILTER_ARCHITECTURE_ARM64 }
                        }
                    },
                });

            var resources = await command.GetResources(beanstalkRecommendation, instanceTypeSetting);

            Assert.Contains(resources.Rows, x => string.Equals("t1.any", x.SystemName, StringComparison.OrdinalIgnoreCase));
            Assert.Contains(resources.Rows, x => string.Equals("t1.x86_64", x.SystemName, StringComparison.OrdinalIgnoreCase));
            Assert.Contains(resources.Rows, x => string.Equals("t1.arm64", x.SystemName, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task WindowsExecute()
        {
            var engine = await HelperFunctions.BuildRecommendationEngine(
                "WebAppWithDockerFile",
                new FileManager(),
                new DirectoryManager(),
                "us-west-2",
                "123456789012",
                "default"
            );

            var recommendations = await engine.ComputeRecommendations();

            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_WINDOWS_RECIPE_ID);

            var instanceTypeSetting = _optionSettingHandler.GetOptionSetting(beanstalkRecommendation, "InstanceType");


            _mockAWSResourceQueryer
                .Setup(x => x.ListOfAvailableInstanceTypes())
                .ReturnsAsync(new List<InstanceTypeInfo>()
                {
                    new InstanceTypeInfo()
                    {
                        InstanceType = "t1.any",
                        FreeTierEligible = true,
                        VCpuInfo = new VCpuInfo
                        {
                            DefaultCores = 1,
                        },
                        MemoryInfo = new MemoryInfo
                        {
                            SizeInMiB = 1000
                        },
                        ProcessorInfo = new ProcessorInfo()
                        {
                            SupportedArchitectures = new List<string>{ EC2.FILTER_ARCHITECTURE_X86_64, EC2.FILTER_ARCHITECTURE_ARM64 }
                        }
                    },
                    new InstanceTypeInfo()
                    {
                        InstanceType = "t1.x86_64",
                        FreeTierEligible = true,
                        VCpuInfo = new VCpuInfo
                        {
                            DefaultCores = 2,
                        },
                        MemoryInfo = new MemoryInfo
                        {
                            SizeInMiB = 2000
                        },
                        ProcessorInfo = new ProcessorInfo()
                        {
                            SupportedArchitectures = new List<string>{ EC2.FILTER_ARCHITECTURE_X86_64 }
                        }
                    },
                    new InstanceTypeInfo()
                    {
                        InstanceType = "t1.x86_64v2",
                        FreeTierEligible = true,
                        VCpuInfo = new VCpuInfo
                        {
                            DefaultCores = 2,
                        },
                        MemoryInfo = new MemoryInfo
                        {
                            SizeInMiB = 3000
                        },
                        ProcessorInfo = new ProcessorInfo()
                        {
                            SupportedArchitectures = new List<string>{ EC2.FILTER_ARCHITECTURE_X86_64 }
                        }
                    },
                    new InstanceTypeInfo()
                    {
                        InstanceType = "t1.arm64",
                        FreeTierEligible = false,
                        VCpuInfo = new VCpuInfo
                        {
                            DefaultCores = 3,
                        },
                        MemoryInfo = new MemoryInfo
                        {
                            SizeInMiB = 2000
                        },
                        ProcessorInfo = new ProcessorInfo()
                        {
                            SupportedArchitectures = new List<string>{ EC2.FILTER_ARCHITECTURE_ARM64 }
                        }
                    },
                });

            // Default options
            {
                var interactiveServices = new TestToolInteractiveServiceImpl(new List<string>
                {
                    "y", // Free tier
                    "1", // CPU 
                    "1", // Memory
                    "1"  // Instance type
                });
                var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager, _optionSettingHandler);
                var command = new WindowsInstanceTypeCommand(_mockAWSResourceQueryer.Object, consoleUtilities, _optionSettingHandler);

                var typeHintResponse = await command.Execute(beanstalkRecommendation, instanceTypeSetting);

                Assert.Contains("t1.any", typeHintResponse.ToString());
            }

            // Select instance type with 2 cores and 3000 of memory
            {
                var interactiveServices = new TestToolInteractiveServiceImpl(new List<string>
                {
                    "y", // Free tier
                    "2", // CPU 
                    "2", // Memory
                    "1"  // Instance type
                });
                var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager, _optionSettingHandler);
                var command = new WindowsInstanceTypeCommand(_mockAWSResourceQueryer.Object, consoleUtilities, _optionSettingHandler);

                var typeHintResponse = await command.Execute(beanstalkRecommendation, instanceTypeSetting);

                Assert.Contains("t1.x86_64v2", typeHintResponse.ToString());
            }
        }

        [Fact]
        public async Task LinuxExecute()
        {
            var engine = await HelperFunctions.BuildRecommendationEngine(
                "WebAppWithDockerFile",
                new FileManager(),
                new DirectoryManager(),
                "us-west-2",
                "123456789012",
                "default"
            );

            var recommendations = await engine.ComputeRecommendations();

            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_LINUX_RECIPE_ID);

            var instanceTypeSetting = _optionSettingHandler.GetOptionSetting(beanstalkRecommendation, "InstanceType");


            _mockAWSResourceQueryer
                .Setup(x => x.ListOfAvailableInstanceTypes())
                .ReturnsAsync(new List<InstanceTypeInfo>()
                {
                    new InstanceTypeInfo()
                    {
                        InstanceType = "t1.any",
                        FreeTierEligible = true,
                        VCpuInfo = new VCpuInfo
                        {
                            DefaultCores = 1,
                        },
                        MemoryInfo = new MemoryInfo
                        {
                            SizeInMiB = 1000
                        },
                        ProcessorInfo = new ProcessorInfo()
                        {
                            SupportedArchitectures = new List<string>{ EC2.FILTER_ARCHITECTURE_X86_64, EC2.FILTER_ARCHITECTURE_ARM64 }
                        }
                    },
                    new InstanceTypeInfo()
                    {
                        InstanceType = "t1.x86_64",
                        FreeTierEligible = true,
                        VCpuInfo = new VCpuInfo
                        {
                            DefaultCores = 2,
                        },
                        MemoryInfo = new MemoryInfo
                        {
                            SizeInMiB = 2000
                        },
                        ProcessorInfo = new ProcessorInfo()
                        {
                            SupportedArchitectures = new List<string>{ EC2.FILTER_ARCHITECTURE_X86_64 }
                        }
                    },
                    new InstanceTypeInfo()
                    {
                        InstanceType = "t1.x86_64v2",
                        FreeTierEligible = true,
                        VCpuInfo = new VCpuInfo
                        {
                            DefaultCores = 2,
                        },
                        MemoryInfo = new MemoryInfo
                        {
                            SizeInMiB = 3000
                        },
                        ProcessorInfo = new ProcessorInfo()
                        {
                            SupportedArchitectures = new List<string>{ EC2.FILTER_ARCHITECTURE_X86_64 }
                        }
                    },
                    new InstanceTypeInfo()
                    {
                        InstanceType = "t1.arm64",
                        FreeTierEligible = true,
                        VCpuInfo = new VCpuInfo
                        {
                            DefaultCores = 3,
                        },
                        MemoryInfo = new MemoryInfo
                        {
                            SizeInMiB = 2000
                        },
                        ProcessorInfo = new ProcessorInfo()
                        {
                            SupportedArchitectures = new List<string>{ EC2.FILTER_ARCHITECTURE_ARM64 }
                        }
                    },
                });

            // Default options
            {
                var interactiveServices = new TestToolInteractiveServiceImpl(new List<string>
                {
                    "y", // Free tier
                    "1", // Architecture x64_86
                    "1", // CPU 
                    "1", // Memory
                    "1"  // Instance type
                });
                var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager, _optionSettingHandler);
                var command = new LinuxInstanceTypeCommand(_mockAWSResourceQueryer.Object, consoleUtilities, _optionSettingHandler);

                var typeHintResponse = await command.Execute(beanstalkRecommendation, instanceTypeSetting);

                Assert.Contains("t1.any", typeHintResponse.ToString());
            }

            // Select instance type with ARM CPU 3 cores and 2000 of memory
            {
                var interactiveServices = new TestToolInteractiveServiceImpl(new List<string>
                {
                    "y", // Free tier
                    "2", // Architecture arm64
                    "2", // CPU 
                    "1", // Memory
                    "1"  // Instance type
                });
                var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager, _optionSettingHandler);
                var command = new LinuxInstanceTypeCommand(_mockAWSResourceQueryer.Object, consoleUtilities, _optionSettingHandler);

                var typeHintResponse = await command.Execute(beanstalkRecommendation, instanceTypeSetting);

                Assert.Contains("t1.arm64", typeHintResponse.ToString());
            }
        }

        [Fact]
        public async Task WindowsValidate()
        {
            var engine = await HelperFunctions.BuildRecommendationEngine(
                "WebAppWithDockerFile",
                new FileManager(),
                new DirectoryManager(),
                "us-west-2",
                "123456789012",
                "default"
            );

            var recommendations = await engine.ComputeRecommendations();

            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_WINDOWS_RECIPE_ID);

            var instanceTypeSetting = _optionSettingHandler.GetOptionSetting(beanstalkRecommendation, "InstanceType");

            _mockAWSResourceQueryer
                .Setup(x => x.DescribeInstanceType(It.IsAny<string>()))
                .ReturnsAsync((string type) =>
                {
                    if (type == "t1.x64_86")
                    {
                        return new InstanceTypeInfo
                        {
                            ProcessorInfo = new ProcessorInfo
                            {
                                SupportedArchitectures = new List<string> { "x64_86" }
                            }
                        };
                    }
                    if (type == "t1.arm64")
                    {
                        return new InstanceTypeInfo
                        {
                            ProcessorInfo = new ProcessorInfo
                            {
                                SupportedArchitectures = new List<string> { EC2.FILTER_ARCHITECTURE_ARM64 }
                            }
                        };
                    }
                    if (type == "t1.both")
                    {
                        return new InstanceTypeInfo
                        {
                            ProcessorInfo = new ProcessorInfo
                            {
                                SupportedArchitectures = new List<string> { "x64_86", EC2.FILTER_ARCHITECTURE_ARM64 }
                            }
                        };
                    }

                    return null;
                });

            var validator = new WindowsInstanceTypeValidator(_mockAWSResourceQueryer.Object);

            Assert.True(validator.Validate("t1.x64_86", beanstalkRecommendation, instanceTypeSetting).Result.IsValid);
            Assert.False(validator.Validate("t1.arm64", beanstalkRecommendation, instanceTypeSetting).Result.IsValid);
            Assert.True(validator.Validate("t1.both", beanstalkRecommendation, instanceTypeSetting).Result.IsValid);
            Assert.False(validator.Validate("t1.fake", beanstalkRecommendation, instanceTypeSetting).Result.IsValid);
        }

        [Fact]
        public async Task LinuxValidate()
        {
            var engine = await HelperFunctions.BuildRecommendationEngine(
                "WebAppWithDockerFile",
                new FileManager(),
                new DirectoryManager(),
                "us-west-2",
                "123456789012",
                "default"
            );

            var recommendations = await engine.ComputeRecommendations();

            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_LINUX_RECIPE_ID);

            var instanceTypeSetting = _optionSettingHandler.GetOptionSetting(beanstalkRecommendation, "InstanceType");

            _mockAWSResourceQueryer
                .Setup(x => x.DescribeInstanceType(It.IsAny<string>()))
                .ReturnsAsync((string type) =>
                {
                    if (type == "t1.x64_86")
                    {
                        return new InstanceTypeInfo
                        {
                            ProcessorInfo = new ProcessorInfo
                            {
                                SupportedArchitectures = new List<string> { "x64_86" }
                            }
                        };
                    }
                    if (type == "t1.arm64")
                    {
                        return new InstanceTypeInfo
                        {
                            ProcessorInfo = new ProcessorInfo
                            {
                                SupportedArchitectures = new List<string> { EC2.FILTER_ARCHITECTURE_ARM64 }
                            }
                        };
                    }
                    if (type == "t1.both")
                    {
                        return new InstanceTypeInfo
                        {
                            ProcessorInfo = new ProcessorInfo
                            {
                                SupportedArchitectures = new List<string> { "x64_86", EC2.FILTER_ARCHITECTURE_ARM64 }
                            }
                        };
                    }

                    return null;
                });

            var validator = new LinuxInstanceTypeValidator(_mockAWSResourceQueryer.Object);

            Assert.True(validator.Validate("t1.x64_86", beanstalkRecommendation, instanceTypeSetting).Result.IsValid);
            Assert.True(validator.Validate("t1.arm64", beanstalkRecommendation, instanceTypeSetting).Result.IsValid);
            Assert.True(validator.Validate("t1.both", beanstalkRecommendation, instanceTypeSetting).Result.IsValid);
            Assert.False(validator.Validate("t1.fake", beanstalkRecommendation, instanceTypeSetting).Result.IsValid);
        }
    }
}
