// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Orchestration.Data;
using AWS.Deploy.Orchestration.LocalUserSettings;
using AWS.Deploy.Orchestration.Utilities;
using AWS.Deploy.Recipes.CDK.Common;
using Moq;
using Xunit;

namespace AWS.Deploy.Orchestration.UnitTests
{
    public class DeployedApplicationQueryerTests
    {
        private readonly Mock<IAWSResourceQueryer> _mockAWSResourceQueryer;
        private readonly IDirectoryManager _directoryManager;
        private readonly TestFileManager _fileManager;
        private readonly Mock<ILocalUserSettingsEngine> _mockLocalUserSettingsEngine;
        private readonly Mock<IOrchestratorInteractiveService> _mockOrchestratorInteractiveService;

        public DeployedApplicationQueryerTests()
        {
            _mockAWSResourceQueryer = new Mock<IAWSResourceQueryer>();
            _directoryManager = new TestDirectoryManager();
            _fileManager = new TestFileManager();
            _mockLocalUserSettingsEngine = new Mock<ILocalUserSettingsEngine>();
            _mockOrchestratorInteractiveService = new Mock<IOrchestratorInteractiveService>();
        }

        [Fact]
        public async Task GetExistingDeployedApplications_ListDeploymentsCall()
        {
            var stack = new Stack {
                Tags = new List<Amazon.CloudFormation.Model.Tag>() { new Amazon.CloudFormation.Model.Tag {
                    Key = Constants.CloudFormationIdentifier.STACK_TAG,
                    Value = "AspNetAppEcsFargate"
                } },
                Description = Constants.CloudFormationIdentifier.STACK_DESCRIPTION_PREFIX,
                StackStatus = StackStatus.CREATE_COMPLETE,
                StackName = "Stack1"
            };

            _mockAWSResourceQueryer
                .Setup(x => x.GetCloudFormationStacks())
                .Returns(Task.FromResult(new List<Stack>() { stack }));

            _mockAWSResourceQueryer
                .Setup(x => x.ListOfElasticBeanstalkEnvironments(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(new List<EnvironmentDescription>()));

            var deployedApplicationQueryer = new DeployedApplicationQueryer(
                _mockAWSResourceQueryer.Object,
                _mockLocalUserSettingsEngine.Object,
                _mockOrchestratorInteractiveService.Object,
                _fileManager);

            var deploymentTypes = new List<DeploymentTypes>() { DeploymentTypes.CdkProject, DeploymentTypes.BeanstalkEnvironment };
            var result = await deployedApplicationQueryer.GetExistingDeployedApplications(deploymentTypes);
            Assert.Single(result);

            var expectedStack = result.First();
            Assert.Equal("Stack1", expectedStack.Name);
        }

        [Fact]
        public async Task GetExistingDeployedApplications_CompatibleSystemRecipes()
        {
            var stacks = new List<Stack> {
                new Stack{
                    Tags = new List<Amazon.CloudFormation.Model.Tag>() { new Amazon.CloudFormation.Model.Tag {
                        Key = Constants.CloudFormationIdentifier.STACK_TAG,
                        Value = "AspNetAppEcsFargate"
                    } },
                    Description = Constants.CloudFormationIdentifier.STACK_DESCRIPTION_PREFIX,
                    StackStatus = StackStatus.CREATE_COMPLETE,
                    StackName = "WebApp"
                },
                new Stack{
                    Tags = new List<Amazon.CloudFormation.Model.Tag>() { new Amazon.CloudFormation.Model.Tag {
                        Key = Constants.CloudFormationIdentifier.STACK_TAG,
                        Value = "ConsoleAppEcsFargateService"
                    } },
                    Description = Constants.CloudFormationIdentifier.STACK_DESCRIPTION_PREFIX,
                    StackStatus = StackStatus.CREATE_COMPLETE,
                    StackName = "ServiceProcessor"
                }
            };

            _mockAWSResourceQueryer
                .Setup(x => x.GetCloudFormationStacks())
                .Returns(Task.FromResult(stacks));

            _mockAWSResourceQueryer
                .Setup(x => x.ListOfElasticBeanstalkEnvironments(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(new List<EnvironmentDescription>()));

            var deployedApplicationQueryer = new DeployedApplicationQueryer(
                _mockAWSResourceQueryer.Object,
                _mockLocalUserSettingsEngine.Object,
                _mockOrchestratorInteractiveService.Object,
                _fileManager);

            var recommendations = new List<Recommendation>
            {
                new Recommendation(new RecipeDefinition("AspNetAppEcsFargate", "0.2.0",  "ASP.NET Core ECS", DeploymentTypes.CdkProject, DeploymentBundleTypes.Container, "", "", "", "", "" ), null!, 100, new Dictionary<string, object>())
                {

                }
            };


            var result = await deployedApplicationQueryer.GetCompatibleApplications(recommendations);
            Assert.Single(result);
            Assert.Equal("AspNetAppEcsFargate", result[0].RecipeId);
        }

        [Fact]
        public async Task GetExistingDeployedApplications_WithDeploymentProjects()
        {
            var stacks = new List<Stack> {
                // Existing stack from the base recipe which should be valid
                new Stack{
                    Tags = new List<Amazon.CloudFormation.Model.Tag>() { new Amazon.CloudFormation.Model.Tag {
                        Key = Constants.CloudFormationIdentifier.STACK_TAG,
                        Value = "AspNetAppEcsFargate"
                    } },
                    Description = Constants.CloudFormationIdentifier.STACK_DESCRIPTION_PREFIX,
                    StackStatus = StackStatus.CREATE_COMPLETE,
                    StackName = "WebApp"
                },
                // Existing stack that was deployed custom deployment project. Should be valid.
                new Stack{
                    Tags = new List<Amazon.CloudFormation.Model.Tag>() { new Amazon.CloudFormation.Model.Tag {
                        Key = Constants.CloudFormationIdentifier.STACK_TAG,
                        Value = "AspNetAppEcsFargate-Custom"
                    } },
                    Description = Constants.CloudFormationIdentifier.STACK_DESCRIPTION_PREFIX,
                    StackStatus = StackStatus.CREATE_COMPLETE,
                    StackName = "WebApp-Custom"
                },
                // Stack created from a different recipe and should not be valid.
                new Stack{
                    Tags = new List<Amazon.CloudFormation.Model.Tag>() { new Amazon.CloudFormation.Model.Tag {
                        Key = Constants.CloudFormationIdentifier.STACK_TAG,
                        Value = "ConsoleAppEcsFargateService"
                    } },
                    Description = Constants.CloudFormationIdentifier.STACK_DESCRIPTION_PREFIX,
                    StackStatus = StackStatus.CREATE_COMPLETE,
                    StackName = "ServiceProcessor"
                }
            };

            _mockAWSResourceQueryer
                .Setup(x => x.GetCloudFormationStacks())
                .Returns(Task.FromResult(stacks));

            _mockAWSResourceQueryer
                .Setup(x => x.ListOfElasticBeanstalkEnvironments(It.IsAny<string>(), It.IsAny<string>()))
               .Returns(Task.FromResult(new List<EnvironmentDescription>()));

            var deployedApplicationQueryer = new DeployedApplicationQueryer(
                _mockAWSResourceQueryer.Object,
                _mockLocalUserSettingsEngine.Object,
                _mockOrchestratorInteractiveService.Object,
                _fileManager);

            var recommendations = new List<Recommendation>
            {
                new Recommendation(new RecipeDefinition("AspNetAppEcsFargate-Custom", "0.2.0",  "Saved Deployment Project",
                                                            DeploymentTypes.CdkProject, DeploymentBundleTypes.Container, "", "", "", "", "" )
                                                            {
                                                                PersistedDeploymentProject = true,
                                                                BaseRecipeId = "AspNetAppEcsFargate"
                                                            },
                                                            null!, 100, new Dictionary<string, object>())
                {

                }
            };


            var result = await deployedApplicationQueryer.GetCompatibleApplications(recommendations);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, x => string.Equals("AspNetAppEcsFargate", x.RecipeId));
            Assert.Contains(result, x => string.Equals("AspNetAppEcsFargate-Custom", x.RecipeId));
        }

        [Theory]
        [InlineData("", Constants.CloudFormationIdentifier.STACK_DESCRIPTION_PREFIX, "CREATE_COMPLETE")]
        [InlineData("AspNetAppEcsFargate", "", "CREATE_COMPLETE")]
        [InlineData("AspNetAppEcsFargate", Constants.CloudFormationIdentifier.STACK_DESCRIPTION_PREFIX, "DELETE_IN_PROGRESS")]
        [InlineData("AspNetAppEcsFargate", Constants.CloudFormationIdentifier.STACK_DESCRIPTION_PREFIX, "ROLLBACK_COMPLETE")]
        public async Task GetExistingDeployedApplications_InvalidConfigurations(string recipeId, string stackDecription, string deploymentStatus)
        {
            var tags = new List<Amazon.CloudFormation.Model.Tag>();
            if (!string.IsNullOrEmpty(recipeId))
                tags.Add(new Amazon.CloudFormation.Model.Tag
                {
                    Key = Constants.CloudFormationIdentifier.STACK_TAG,
                    Value = "AspNetAppEcsFargate"
                });

            var stack = new Stack
            {
                Tags = tags,
                Description = stackDecription,
                StackStatus = deploymentStatus,
                StackName = "Stack1"
            };

            _mockAWSResourceQueryer
                .Setup(x => x.GetCloudFormationStacks())
                .Returns(Task.FromResult(new List<Stack>() { stack }));

            _mockAWSResourceQueryer
                .Setup(x => x.ListOfElasticBeanstalkEnvironments(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(new List<EnvironmentDescription>()));

            var deployedApplicationQueryer = new DeployedApplicationQueryer(
                _mockAWSResourceQueryer.Object,
                _mockLocalUserSettingsEngine.Object,
                _mockOrchestratorInteractiveService.Object,
                _fileManager);

            var deploymentTypes = new List<DeploymentTypes>() { DeploymentTypes.CdkProject, DeploymentTypes.BeanstalkEnvironment };
            var result = await deployedApplicationQueryer.GetExistingDeployedApplications(deploymentTypes);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetExistingDeployedApplications_ContainsValidBeanstalkEnvironments()
        {
            var environments = new List<EnvironmentDescription>
            {
                new EnvironmentDescription
                {
                    EnvironmentName = "env-1",
                    PlatformArn = "dotnet-platform-arn1",
                    EnvironmentArn = "env-arn-1",
                    Status = EnvironmentStatus.Ready
                },
                new EnvironmentDescription
                {
                    EnvironmentName = "env-2",
                    PlatformArn = "dotnet-platform-arn1",
                    EnvironmentArn = "env-arn-2",
                    Status = EnvironmentStatus.Ready
                }
            };

            var platforms = new List<PlatformSummary>
            {
                new PlatformSummary
                {
                    PlatformArn = "dotnet-platform-arn1"
                },
                new PlatformSummary
                {
                    PlatformArn = "dotnet-platform-arn2"
                }
            };

            _mockAWSResourceQueryer
                .Setup(x => x.GetCloudFormationStacks())
                .Returns(Task.FromResult(new List<Stack>()));

            _mockAWSResourceQueryer
                .Setup(x => x.ListOfElasticBeanstalkEnvironments(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(environments));

            _mockAWSResourceQueryer
                .Setup(x => x.GetElasticBeanstalkPlatformArns(It.IsAny<string>()))
                .Returns(Task.FromResult(platforms));

            _mockAWSResourceQueryer
                .Setup(x => x.ListElasticBeanstalkResourceTags(It.IsAny<string>()))
                .Returns(Task.FromResult(new List<Amazon.ElasticBeanstalk.Model.Tag>()));

            var deployedApplicationQueryer = new DeployedApplicationQueryer(
                _mockAWSResourceQueryer.Object,
                _mockLocalUserSettingsEngine.Object,
                _mockOrchestratorInteractiveService.Object,
                _fileManager);

            var deploymentTypes = new List<DeploymentTypes>() { DeploymentTypes.CdkProject, DeploymentTypes.BeanstalkEnvironment };
            var result = await deployedApplicationQueryer.GetExistingDeployedApplications(deploymentTypes);
            Assert.Contains(result, x => string.Equals("env-1", x.Name));
            Assert.Contains(result, x => string.Equals("env-2", x.Name));
        }

        [Fact]
        public async Task GetExistingDeployedApplication_SkipsEnvironmentsWithIncompatiblePlatformArns()
        {
            var environments = new List<EnvironmentDescription>
            {
                new EnvironmentDescription
                {
                    EnvironmentName = "env",
                    PlatformArn = "incompatible-platform-arn",
                    EnvironmentArn = "env-arn",
                    Status = EnvironmentStatus.Ready
                }
            };

            var platforms = new List<PlatformSummary>
            {
                new PlatformSummary
                {
                    PlatformArn = "dotnet-platform-arn1"
                },
                new PlatformSummary
                {
                    PlatformArn = "dotnet-platform-arn2"
                }
            };

            _mockAWSResourceQueryer
                .Setup(x => x.GetCloudFormationStacks())
                .Returns(Task.FromResult(new List<Stack>()));

            _mockAWSResourceQueryer
                .Setup(x => x.ListOfElasticBeanstalkEnvironments(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(environments));

            _mockAWSResourceQueryer
                .Setup(x => x.GetElasticBeanstalkPlatformArns(It.IsAny<string>()))
                .Returns(Task.FromResult(platforms));

            _mockAWSResourceQueryer
                .Setup(x => x.ListElasticBeanstalkResourceTags(It.IsAny<string>()))
                .Returns(Task.FromResult(new List<Amazon.ElasticBeanstalk.Model.Tag>()));

            var deployedApplicationQueryer = new DeployedApplicationQueryer(
                _mockAWSResourceQueryer.Object,
                _mockLocalUserSettingsEngine.Object,
                _mockOrchestratorInteractiveService.Object,
                _fileManager);

            var deploymentTypes = new List<DeploymentTypes>() { DeploymentTypes.CdkProject, DeploymentTypes.BeanstalkEnvironment };
            var result = await deployedApplicationQueryer.GetExistingDeployedApplications(deploymentTypes);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetExistingDeployedApplication_SkipsEnvironmentsCreatedFromTheDeployTool()
        {
            var environments = new List<EnvironmentDescription>
            {
                new EnvironmentDescription
                {
                    EnvironmentName = "env",
                    PlatformArn = "dotnet-platform-arn1",
                    EnvironmentArn = "env-arn",
                    Status = EnvironmentStatus.Ready
                }
            };

            var platforms = new List<PlatformSummary>
            {
                new PlatformSummary
                {
                    PlatformArn = "dotnet-platform-arn1"
                },
                new PlatformSummary
                {
                    PlatformArn = "dotnet-platform-arn2"
                }
            };

            var tags = new List<Amazon.ElasticBeanstalk.Model.Tag>
            {
                new Amazon.ElasticBeanstalk.Model.Tag
                {
                    Key = Constants.CloudFormationIdentifier.STACK_TAG,
                    Value = "RecipeId"
                }
            };

            _mockAWSResourceQueryer
                .Setup(x => x.GetCloudFormationStacks())
                .Returns(Task.FromResult(new List<Stack>()));

            _mockAWSResourceQueryer
                .Setup(x => x.ListOfElasticBeanstalkEnvironments(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(environments));

            _mockAWSResourceQueryer
                .Setup(x => x.GetElasticBeanstalkPlatformArns(It.IsAny<string>()))
                .Returns(Task.FromResult(platforms));

            _mockAWSResourceQueryer
                .Setup(x => x.ListElasticBeanstalkResourceTags("env-arn"))
                .Returns(Task.FromResult(tags));

            var deployedApplicationQueryer = new DeployedApplicationQueryer(
                _mockAWSResourceQueryer.Object,
                _mockLocalUserSettingsEngine.Object,
                _mockOrchestratorInteractiveService.Object,
                _fileManager);

            var deploymentTypes = new List<DeploymentTypes>() { DeploymentTypes.CdkProject, DeploymentTypes.BeanstalkEnvironment };
            var result = await deployedApplicationQueryer.GetExistingDeployedApplications(deploymentTypes);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetPreviousSettings_BeanstalkEnvironment()
        {
            var application = new CloudApplication("name", "Id", CloudApplicationResourceType.BeanstalkEnvironment, "recipe");
            var configurationSettings = new List<ConfigurationOptionSetting>
            {
                new ConfigurationOptionSetting
                {
                    Namespace = Constants.ElasticBeanstalk.EnhancedHealthReportingOptionNameSpace,
                    OptionName = Constants.ElasticBeanstalk.EnhancedHealthReportingOptionName,
                    Value = "enhanced"
                },
                new ConfigurationOptionSetting
                {
                    OptionName = Constants.ElasticBeanstalk.HealthCheckURLOptionName,
                    Namespace = Constants.ElasticBeanstalk.HealthCheckURLOptionNameSpace,
                    Value = "/"
                },
                new ConfigurationOptionSetting
                {
                    OptionName = Constants.ElasticBeanstalk.ProxyOptionName,
                    Namespace = Constants.ElasticBeanstalk.ProxyOptionNameSpace,
                    Value = "nginx"
                },
                new ConfigurationOptionSetting
                {
                    OptionName = Constants.ElasticBeanstalk.XRayTracingOptionName,
                    Namespace = Constants.ElasticBeanstalk.XRayTracingOptionNameSpace,
                    Value = "false"
                }
            };

            _mockAWSResourceQueryer
                .Setup(x => x.GetBeanstalkEnvironmentConfigurationSettings(It.IsAny<string>()))
                .Returns(Task.FromResult(configurationSettings));

            var deployedApplicationQueryer = new DeployedApplicationQueryer(
                _mockAWSResourceQueryer.Object,
                _mockLocalUserSettingsEngine.Object,
                _mockOrchestratorInteractiveService.Object,
                _fileManager);

            var projectDefinition = new ProjectDefinition(null!, "testPath", "", "net6.0");
            var recipeDefinitiion = new RecipeDefinition("AspNetAppExistingBeanstalkEnvironment", "", "", DeploymentTypes.BeanstalkEnvironment, DeploymentBundleTypes.DotnetPublishZipFile, "", "", "", "", "");
            var recommendation =  new Recommendation(recipeDefinitiion, projectDefinition, 100, new Dictionary<string, object>());

            var optionSettings = await deployedApplicationQueryer.GetPreviousSettings(application, recommendation);

            Assert.Equal("enhanced", optionSettings[Constants.ElasticBeanstalk.EnhancedHealthReportingOptionId]);
            Assert.Equal("/", optionSettings[Constants.ElasticBeanstalk.HealthCheckURLOptionId]);
            Assert.Equal("nginx", optionSettings[Constants.ElasticBeanstalk.ProxyOptionId]);
            Assert.Equal("false", optionSettings[Constants.ElasticBeanstalk.XRayTracingOptionId]);
        }

        [Theory]

        [InlineData(@"{
              ""manifestVersion"": 1,
              ""deployments"": {
                ""aspNetCoreWeb"": [
                  {
                    ""name"": ""app"",
                    ""parameters"": {
                      ""iisPath"": ""/path"",
                      ""iisWebSite"": ""Default Web Site Custom""
                    }
                  }
                ]
              }
            }")]
        [InlineData(@"{
              ""manifestVersion"": 1,
              // comments
              ""deployments"": {
                ""aspNetCoreWeb"": [
                  {
                    ""name"": ""app"",
                    ""parameters"": {
                      ""iisPath"": ""/path"",
                      ""iisWebSite"": ""Default Web Site Custom""
                    }
                  }
                ]
              }
            }")]
        public async Task GetPreviousSettings_BeanstalkWindowsEnvironment(string manifestJson)
        {
            var application = new CloudApplication("name", "Id", CloudApplicationResourceType.BeanstalkEnvironment, "recipe");
            var configurationSettings = new List<ConfigurationOptionSetting>
            {
                new ConfigurationOptionSetting
                {
                    Namespace = Constants.ElasticBeanstalk.EnhancedHealthReportingOptionNameSpace,
                    OptionName = Constants.ElasticBeanstalk.EnhancedHealthReportingOptionName,
                    Value = "enhanced"
                },
                new ConfigurationOptionSetting
                {
                    OptionName = Constants.ElasticBeanstalk.HealthCheckURLOptionName,
                    Namespace = Constants.ElasticBeanstalk.HealthCheckURLOptionNameSpace,
                    Value = "/"
                },
                new ConfigurationOptionSetting
                {
                    OptionName = Constants.ElasticBeanstalk.XRayTracingOptionName,
                    Namespace = Constants.ElasticBeanstalk.XRayTracingOptionNameSpace,
                    Value = "false"
                }
            };

            _mockAWSResourceQueryer
                .Setup(x => x.GetBeanstalkEnvironmentConfigurationSettings(It.IsAny<string>()))
                .Returns(Task.FromResult(configurationSettings));

            var deployedApplicationQueryer = new DeployedApplicationQueryer(
                _mockAWSResourceQueryer.Object,
                _mockLocalUserSettingsEngine.Object,
                _mockOrchestratorInteractiveService.Object,
                _fileManager);

            _fileManager.InMemoryStore.Add(Path.Combine("testPath", "aws-windows-deployment-manifest.json"), manifestJson);
            var projectDefinition = new ProjectDefinition(null!, Path.Combine("testPath", "project.csproj"), "", "net6.0");
            var recipeDefinitiion = new RecipeDefinition("AspNetAppExistingBeanstalkWindowsEnvironment", "", "", DeploymentTypes.BeanstalkEnvironment, DeploymentBundleTypes.DotnetPublishZipFile, "", "", "", "", "");
            var recommendation = new Recommendation(recipeDefinitiion, projectDefinition, 100, new Dictionary<string, object>());

            var optionSettings = await deployedApplicationQueryer.GetPreviousSettings(application, recommendation);

            Assert.Equal("enhanced", optionSettings[Constants.ElasticBeanstalk.EnhancedHealthReportingOptionId]);
            Assert.Equal("/", optionSettings[Constants.ElasticBeanstalk.HealthCheckURLOptionId]);
            Assert.Equal("false", optionSettings[Constants.ElasticBeanstalk.XRayTracingOptionId]);
            Assert.Equal("false", optionSettings[Constants.ElasticBeanstalk.XRayTracingOptionId]);
            Assert.Equal("/path", optionSettings[Constants.ElasticBeanstalk.IISAppPathOptionId]);
            Assert.Equal("Default Web Site Custom", optionSettings[Constants.ElasticBeanstalk.IISWebSiteOptionId]);
        }
    }
}
