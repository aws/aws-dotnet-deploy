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
using AWS.Deploy.Orchestration.Data;
using AWS.Deploy.Orchestration.Utilities;
using AWS.Deploy.Recipes.CDK.Common;
using Moq;
using Xunit;

namespace AWS.Deploy.Orchestration.UnitTests
{
    public class DeployedApplicationQueryerTests
    {
        private readonly Mock<IAWSResourceQueryer> _mockAWSResourceQueryer;

        public DeployedApplicationQueryerTests()
        {
            _mockAWSResourceQueryer = new Mock<IAWSResourceQueryer>();
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

            var deployedApplicationQueryer = new DeployedApplicationQueryer(_mockAWSResourceQueryer.Object);

            var result = await deployedApplicationQueryer.GetExistingDeployedApplications();
            Assert.Single(result);

            var expectedStack = result.First();
            Assert.Equal("Stack1", expectedStack.StackName);
        }

        [Fact]
        public async Task GetExistingDeployedApplications_DeployCall()
        {
            var stack = new Stack
            {
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

            var deployedApplicationQueryer = new DeployedApplicationQueryer(_mockAWSResourceQueryer.Object);

            var result = await deployedApplicationQueryer.GetExistingDeployedApplications(new List<Recommendation>());
            Assert.Empty(result);
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

            var deployedApplicationQueryer = new DeployedApplicationQueryer(_mockAWSResourceQueryer.Object);

            var result = await deployedApplicationQueryer.GetExistingDeployedApplications();
            Assert.Empty(result);
        }
    }
}
