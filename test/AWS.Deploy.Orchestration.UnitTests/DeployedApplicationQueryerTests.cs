// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using AWS.Deploy.Common;
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
        private readonly Mock<ILocalUserSettingsEngine> _mockLocalUserSettingsEngine;
        private readonly Mock<IOrchestratorInteractiveService> _mockOrchestratorInteractiveService;

        public DeployedApplicationQueryerTests()
        {
            _mockAWSResourceQueryer = new Mock<IAWSResourceQueryer>();
            _directoryManager = new TestDirectoryManager();
            _mockLocalUserSettingsEngine = new Mock<ILocalUserSettingsEngine>();
            _mockOrchestratorInteractiveService = new Mock<IOrchestratorInteractiveService>();
        }

        [Fact]
        public async Task GetExistingDeployedApplications_ListDeploymentsCall()
        {
            var stack = new Stack {
                Tags = new List<Tag>() { new Tag {
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

            var deployedApplicationQueryer = new DeployedApplicationQueryer(
                _mockAWSResourceQueryer.Object,
                _mockLocalUserSettingsEngine.Object,
                _mockOrchestratorInteractiveService.Object);

            var result = await deployedApplicationQueryer.GetExistingDeployedApplications();
            Assert.Single(result);

            var expectedStack = result.First();
            Assert.Equal("Stack1", expectedStack.StackName);
        }

        [Fact]
        public async Task GetExistingDeployedApplications_CompatibleSystemRecipes()
        {
            var stacks = new List<Stack> {
                new Stack{
                    Tags = new List<Tag>() { new Tag {
                        Key = Constants.CloudFormationIdentifier.STACK_TAG,
                        Value = "AspNetAppEcsFargate"
                    } },
                    Description = Constants.CloudFormationIdentifier.STACK_DESCRIPTION_PREFIX,
                    StackStatus = StackStatus.CREATE_COMPLETE,
                    StackName = "WebApp"
                },
                new Stack{
                    Tags = new List<Tag>() { new Tag {
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

            var deployedApplicationQueryer = new DeployedApplicationQueryer(
                _mockAWSResourceQueryer.Object,
                _mockLocalUserSettingsEngine.Object,
                _mockOrchestratorInteractiveService.Object);

            var recommendations = new List<Recommendation>
            {
                new Recommendation(new RecipeDefinition("AspNetAppEcsFargate", "0.2.0",  "ASP.NET Core ECS", DeploymentTypes.CdkProject, DeploymentBundleTypes.Container, "", "", "", "", "" ), null, null, 100, new Dictionary<string, string>())
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
                    Tags = new List<Tag>() { new Tag {
                        Key = Constants.CloudFormationIdentifier.STACK_TAG,
                        Value = "AspNetAppEcsFargate"
                    } },
                    Description = Constants.CloudFormationIdentifier.STACK_DESCRIPTION_PREFIX,
                    StackStatus = StackStatus.CREATE_COMPLETE,
                    StackName = "WebApp"
                },
                // Existing stack that was deployed custom deployment project. Should be valid.
                new Stack{
                    Tags = new List<Tag>() { new Tag {
                        Key = Constants.CloudFormationIdentifier.STACK_TAG,
                        Value = "AspNetAppEcsFargate-Custom"
                    } },
                    Description = Constants.CloudFormationIdentifier.STACK_DESCRIPTION_PREFIX,
                    StackStatus = StackStatus.CREATE_COMPLETE,
                    StackName = "WebApp-Custom"
                },
                // Stack created from a different recipe and should not be valid.
                new Stack{
                    Tags = new List<Tag>() { new Tag {
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

            var deployedApplicationQueryer = new DeployedApplicationQueryer(
                _mockAWSResourceQueryer.Object,
                _mockLocalUserSettingsEngine.Object,
                _mockOrchestratorInteractiveService.Object);

            var recommendations = new List<Recommendation>
            {
                new Recommendation(new RecipeDefinition("AspNetAppEcsFargate-Custom", "0.2.0",  "Saved Deployment Project",
                                                            DeploymentTypes.CdkProject, DeploymentBundleTypes.Container, "", "", "", "", "" )
                                                            {
                                                                PersistedDeploymentProject = true,
                                                                BaseRecipeId = "AspNetAppEcsFargate"
                                                            },
                                                            null, null, 100, new Dictionary<string, string>())
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
            var tags = new List<Tag>();
            if (!string.IsNullOrEmpty(recipeId))
                tags.Add(new Tag
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

            var deployedApplicationQueryer = new DeployedApplicationQueryer(
                _mockAWSResourceQueryer.Object,
                _mockLocalUserSettingsEngine.Object,
                _mockOrchestratorInteractiveService.Object);

            var result = await deployedApplicationQueryer.GetExistingDeployedApplications();
            Assert.Empty(result);
        }
    }
}
