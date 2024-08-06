// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.ECR;
using Amazon.ECR.Model;
using Amazon.ElasticBeanstalk.Model;
using Amazon.Runtime;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using AWS.Deploy.Common;
using AWS.Deploy.Orchestration.Data;
using Moq;
using Xunit;

namespace AWS.Deploy.Orchestration.UnitTests
{
    public class AWSResourceQueryerTests
    {
        private readonly Mock<IAWSClientFactory> _mockAWSClientFactory;
        private readonly Mock<IAmazonSecurityTokenService> _mockSTSClient;
        private readonly Mock<IAmazonSecurityTokenService> _mockSTSClientDefaultRegion;
        private readonly Mock<IAmazonEC2> _mockEC2Client;
        private readonly Mock<IAmazonECR> _mockECRClient;

        public AWSResourceQueryerTests()
        {
            _mockAWSClientFactory = new Mock<IAWSClientFactory>();
            _mockSTSClient = new Mock<IAmazonSecurityTokenService>();
            _mockSTSClientDefaultRegion = new Mock<IAmazonSecurityTokenService>();
            _mockEC2Client = new Mock<IAmazonEC2>();
            _mockECRClient = new Mock<IAmazonECR>();

            _mockECRClient.Setup(x => x.CreateRepositoryAsync(
                It.IsAny<CreateRepositoryRequest>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new CreateRepositoryResponse()));
        }

        [Fact]
        public async Task GetDefaultVPC_UnauthorizedAccess()
        {
            var awsResourceQueryer = new AWSResourceQueryer(_mockAWSClientFactory.Object);
            var vpcResponse = new DescribeVpcsResponse();
            var unauthorizedException = new AmazonServiceException("You are not authorized to perform this operation.")
            {
                ErrorCode = "UnauthorizedOperation"
            };

            _mockAWSClientFactory.Setup(x => x.GetAWSClient<IAmazonEC2>(It.IsAny<string>())).Returns(_mockEC2Client.Object);
            _mockEC2Client.Setup(x => x.Paginators.DescribeVpcs(It.IsAny<DescribeVpcsRequest>())).Throws(unauthorizedException);

            var exceptionThrown = await Assert.ThrowsAsync<ResourceQueryException>(awsResourceQueryer.GetDefaultVpc);
            Assert.Equal(DeployToolErrorCode.ResourceQuery, exceptionThrown.ErrorCode);
            Assert.Contains("Error attempting to retrieve the default VPC (UnauthorizedOperation).", exceptionThrown.Message);
        }

        [Fact]
        public async Task GetCallerIdentity_HasRegionAccess()
        {
            var awsResourceQueryer = new AWSResourceQueryer(_mockAWSClientFactory.Object);
            var stsResponse = new GetCallerIdentityResponse();

            _mockAWSClientFactory.Setup(x => x.GetAWSClient<IAmazonSecurityTokenService>("ap-southeast-3")).Returns(_mockSTSClient.Object);
            _mockSTSClient.Setup(x => x.GetCallerIdentityAsync(It.IsAny<GetCallerIdentityRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(stsResponse);

            await awsResourceQueryer.GetCallerIdentity("ap-southeast-3");
        }

        [Fact]
        public async Task GetCallerIdentity_OptInRegion()
        {
            var awsResourceQueryer = new AWSResourceQueryer(_mockAWSClientFactory.Object);
            var stsResponse = new GetCallerIdentityResponse();

            _mockAWSClientFactory.Setup(x => x.GetAWSClient<IAmazonSecurityTokenService>("ap-southeast-3")).Returns(_mockSTSClient.Object);
            _mockSTSClient.Setup(x => x.GetCallerIdentityAsync(It.IsAny<GetCallerIdentityRequest>(), It.IsAny<CancellationToken>())).Throws(new Exception("Invalid token"));

            _mockAWSClientFactory.Setup(x => x.GetAWSClient<IAmazonSecurityTokenService>("us-east-1")).Returns(_mockSTSClientDefaultRegion.Object);
            _mockSTSClientDefaultRegion.Setup(x => x.GetCallerIdentityAsync(It.IsAny<GetCallerIdentityRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(stsResponse);

            var exceptionThrown = await Assert.ThrowsAsync<UnableToAccessAWSRegionException>(() => awsResourceQueryer.GetCallerIdentity("ap-southeast-3"));
            Assert.Equal(DeployToolErrorCode.OptInRegionDisabled, exceptionThrown.ErrorCode);
        }

        [Fact]
        public async Task GetCallerIdentity_BadConnection()
        {
            var awsResourceQueryer = new AWSResourceQueryer(_mockAWSClientFactory.Object);
            var stsResponse = new GetCallerIdentityResponse();

            _mockAWSClientFactory.Setup(x => x.GetAWSClient<IAmazonSecurityTokenService>("ap-southeast-3")).Returns(_mockSTSClient.Object);
            _mockSTSClient.Setup(x => x.GetCallerIdentityAsync(It.IsAny<GetCallerIdentityRequest>(), It.IsAny<CancellationToken>())).Throws(new Exception("Invalid token"));

            _mockAWSClientFactory.Setup(x => x.GetAWSClient<IAmazonSecurityTokenService>("us-east-1")).Returns(_mockSTSClientDefaultRegion.Object);
            _mockSTSClientDefaultRegion.Setup(x => x.GetCallerIdentityAsync(It.IsAny<GetCallerIdentityRequest>(), It.IsAny<CancellationToken>())).Throws(new Exception("Invalid token"));

            var exceptionThrown = await Assert.ThrowsAsync<UnableToAccessAWSRegionException>(() => awsResourceQueryer.GetCallerIdentity("ap-southeast-3"));
            Assert.Equal(DeployToolErrorCode.UnableToAccessAWSRegion, exceptionThrown.ErrorCode);
        }

        [Fact]
        public void SortElasticBeanstalkWindowsPlatforms()
        {
            // Use PlatformOwner as a placeholder to store where the summary should be sorted to.
            var platforms = new List<PlatformSummary>()
            {
                new PlatformSummary
                {
                    PlatformBranchName = "IIS 10.0 running on 64bit Windows Server 2016",
                    PlatformVersion = "2.0.0",
                    PlatformOwner = "2"
                },
                new PlatformSummary
                {
                    PlatformBranchName = "IIS 10.0 running on 64bit Windows Server 2019",
                    PlatformVersion = "2.0.0",
                    PlatformOwner = "0"
                },
                new PlatformSummary
                {
                    PlatformBranchName = "IIS 10.0 running on 64bit Windows Server Core 2016",
                    PlatformVersion = "2.0.0",
                    PlatformOwner = "3"
                },
                new PlatformSummary
                {
                    PlatformBranchName = "IIS 10.0 running on 64bit Windows Server Core 2019",
                    PlatformVersion = "2.0.0",
                    PlatformOwner = "1"
                },
                new PlatformSummary
                {
                    PlatformBranchName = "Test Environment",
                    PlatformVersion = "0.5.0",
                    PlatformOwner = "8"
                },
                new PlatformSummary
                {
                    PlatformBranchName = "IIS 10.0 running on 64bit Windows Server 2016",
                    PlatformVersion = "1.0.0",
                    PlatformOwner = "6"
                },
                new PlatformSummary
                {
                    PlatformBranchName = "IIS 10.0 running on 64bit Windows Server 2019",
                    PlatformVersion = "1.0.0",
                    PlatformOwner = "4"
                },
                new PlatformSummary
                {
                    PlatformBranchName = "IIS 10.0 running on 64bit Windows Server Core 2016",
                    PlatformVersion = "1.0.0",
                    PlatformOwner = "7"
                },
                new PlatformSummary
                {
                    PlatformBranchName = "IIS 10.0 running on 64bit Windows Server Core 2019",
                    PlatformVersion = "1.0.0",
                    PlatformOwner = "5"
                }
            };


            AWSResourceQueryer.SortElasticBeanstalkWindowsPlatforms(platforms);

            for (var i = 0; i < platforms.Count; i++)
            {
                Assert.Equal(i.ToString(), platforms[i].PlatformOwner);
            }
        }

        [Fact]
        public void SortElasticBeanstalkLinuxPlatforms()
        {
            // Use PlatformOwner as a placeholder to store where the summary should be sorted to.
            var platforms = new List<PlatformSummary>()
            {
                new PlatformSummary
                {
                    PlatformArn = "arn:aws:elasticbeanstalk:us-west-2::platform/.NET 6 running on 64bit Amazon Linux 2023/3.1.3",
                    PlatformBranchName = ".NET 6 running on 64bit Amazon Linux 2023",
                    PlatformVersion = "3.1.3",
                    PlatformOwner = "1"
                },
                new PlatformSummary
                {
                    PlatformArn = "arn:aws:elasticbeanstalk:us-west-2::platform/.NET 6 running on 64bit Amazon Linux 2023/3.1.2",
                    PlatformBranchName = ".NET 6 running on 64bit Amazon Linux 2023",
                    PlatformVersion = "3.1.2",
                    PlatformOwner = "2"
                },
                new PlatformSummary
                {
                    PlatformArn = "arn:aws:elasticbeanstalk:us-west-2::platform/.NET 6 running on 64bit Amazon Linux 2023/3.0.6",
                    PlatformBranchName = ".NET 6 running on 64bit Amazon Linux 2023",
                    PlatformVersion = "3.0.6",
                    PlatformOwner = "3"
                },
                new PlatformSummary
                {
                    PlatformArn = "arn:aws:elasticbeanstalk:us-west-2::platform/.NET 6 running on 64bit Amazon Linux 2023/3.0.5",
                    PlatformBranchName = ".NET 6 running on 64bit Amazon Linux 2023",
                    PlatformVersion = "3.0.5",
                    PlatformOwner = "4"
                },
                new PlatformSummary
                {
                    PlatformArn = "arn:aws:elasticbeanstalk:us-west-2::platform/.NET 8 running on 64bit Amazon Linux 2023/3.1.3",
                    PlatformBranchName = ".NET 8 running on 64bit Amazon Linux 2023",
                    PlatformVersion = "3.1.3",
                    PlatformOwner = "0"
                },
                new PlatformSummary
                {
                    PlatformArn = "arn:aws:elasticbeanstalk:us-west-2::platform/.NET Core running on 64bit Amazon Linux 2/2.8.0",
                    PlatformBranchName = ".NET Core running on 64bit Amazon Linux 2",
                    PlatformVersion = "2.8.0",
                    PlatformOwner = "5"
                },
                new PlatformSummary
                {
                    PlatformArn = "arn:aws:elasticbeanstalk:us-west-2::platform/.NET Core running on 64bit Amazon Linux 2/2.7.3",
                    PlatformBranchName = ".NET Core running on 64bit Amazon Linux 2",
                    PlatformVersion = "2.7.3",
                    PlatformOwner = "6"
                },
                new PlatformSummary
                {
                    PlatformArn = "arn:aws:elasticbeanstalk:us-west-2::platform/.NET Core running on 64bit Amazon Linux 2/2.6.0",
                    PlatformBranchName = ".NET Core running on 64bit Amazon Linux 2",
                    PlatformVersion = "2.6.0",
                    PlatformOwner = "7"
                },
                new PlatformSummary
                {
                    PlatformArn = "arn:aws:elasticbeanstalk:us-west-2::platform/.NET Core running on 64bit Amazon Linux 2/2.5.7",
                    PlatformBranchName = ".NET Core running on 64bit Amazon Linux 2",
                    PlatformVersion = "2.5.7",
                    PlatformOwner = "8"
                }
            };

            var sortedPlatforms = AWSResourceQueryer.SortElasticBeanstalkLinuxPlatforms(string.Empty, platforms);

            for (var i = 0; i < sortedPlatforms.Count; i++)
            {
                Assert.Equal(i.ToString(), sortedPlatforms[i].PlatformOwner);
            }
        }

        [Fact]
        public void SortElasticBeanstalkLinuxPlatforms_InvalidPlatform_RunningStringNotFound()
        {
            // Use PlatformOwner as a placeholder to store where the summary should be sorted to.
            var platforms = new List<PlatformSummary>()
            {
                new PlatformSummary
                {
                    PlatformArn = "arn:aws:elasticbeanstalk:us-west-2::platform/.NET 6 on 64bit Amazon Linux 2023/3.1.3",
                    PlatformBranchName = ".NET 6 on 64bit Amazon Linux 2023",
                    PlatformVersion = "3.1.3",
                    PlatformOwner = "2"
                },
                new PlatformSummary
                {
                    PlatformArn = "arn:aws:elasticbeanstalk:us-west-2::platform/.NET 6 running on 64bit Amazon Linux 2023/3.1.3",
                    PlatformBranchName = ".NET 6 running on 64bit Amazon Linux 2023",
                    PlatformVersion = "3.1.3",
                    PlatformOwner = "1"
                },
                new PlatformSummary
                {
                    PlatformArn = "arn:aws:elasticbeanstalk:us-west-2::platform/.NET 8 running on 64bit Amazon Linux 2023/3.1.3",
                    PlatformBranchName = ".NET 8 running on 64bit Amazon Linux 2023",
                    PlatformVersion = "3.1.3",
                    PlatformOwner = "0"
                },
                new PlatformSummary
                {
                    PlatformArn = "arn:aws:elasticbeanstalk:us-west-2::platform/.NET Core running on 64bit Amazon Linux 2/3.1.3",
                    PlatformBranchName = ".NET Core running on 64bit Amazon Linux 2",
                    PlatformVersion = "3.1.3",
                    PlatformOwner = "3"
                }
            };

            var sortedPlatforms = AWSResourceQueryer.SortElasticBeanstalkLinuxPlatforms(string.Empty, platforms);

            for (var i = 0; i < sortedPlatforms.Count; i++)
            {
                Assert.Equal(i.ToString(), sortedPlatforms[i].PlatformOwner);
            }
        }

        [Fact]
        public void SortElasticBeanstalkLinuxPlatforms_InvalidPlatform_InvalidBranchName()
        {
            // Use PlatformOwner as a placeholder to store where the summary should be sorted to.
            var platforms = new List<PlatformSummary>()
            {
                new PlatformSummary
                {
                    PlatformArn = "arn:aws:elasticbeanstalk:us-west-2::platform/.NET6 running on 64bit Amazon Linux 2023/3.1.3",
                    PlatformBranchName = ".NET6 running on 64bit Amazon Linux 2023",
                    PlatformVersion = "3.1.3",
                    PlatformOwner = "2"
                },
                new PlatformSummary
                {
                    PlatformArn = "arn:aws:elasticbeanstalk:us-west-2::platform/.NET 6 running on 64bit Amazon Linux 2023/3.1.3",
                    PlatformBranchName = ".NET 6 running on 64bit Amazon Linux 2023",
                    PlatformVersion = "3.1.3",
                    PlatformOwner = "1"
                },
                new PlatformSummary
                {
                    PlatformArn = "arn:aws:elasticbeanstalk:us-west-2::platform/.NET 8 running on 64bit Amazon Linux 2023/3.1.3",
                    PlatformBranchName = ".NET 8 running on 64bit Amazon Linux 2023",
                    PlatformVersion = "3.1.3",
                    PlatformOwner = "0"
                },
                new PlatformSummary
                {
                    PlatformArn = "arn:aws:elasticbeanstalk:us-west-2::platform/.NET Core running on 64bit Amazon Linux 2/3.1.3",
                    PlatformBranchName = ".NET Core running on 64bit Amazon Linux 2",
                    PlatformVersion = "3.1.3",
                    PlatformOwner = "3"
                }
            };

            var sortedPlatforms = AWSResourceQueryer.SortElasticBeanstalkLinuxPlatforms(string.Empty, platforms);

            for (var i = 0; i < sortedPlatforms.Count; i++)
            {
                Assert.Equal(i.ToString(), sortedPlatforms[i].PlatformOwner);
            }
        }

        [Fact]
        public async Task CreateRepository_TagsWithRecipeName_Success()
        {
            var awsResourceQueryer = new AWSResourceQueryer(_mockAWSClientFactory.Object);
            _mockAWSClientFactory.Setup(x => x.GetAWSClient<IAmazonECR>(It.IsAny<string>())).Returns(_mockECRClient.Object);

            await awsResourceQueryer.CreateECRRepository("myRepository", "myRecipe");

            // Assert that it creates the repository with the expected name and deploy tool tag
            _mockECRClient.Verify(x => x.CreateRepositoryAsync(
                It.Is<CreateRepositoryRequest>(request =>
                    request.RepositoryName == "myRepository" &&
                    request.Tags.Count == 1 &&
                    request.Tags[0].Key == "aws-dotnet-deploy" &&
                    request.Tags[0].Value == "myRecipe"),
                It.IsAny<CancellationToken>()), Times.Once());
            _mockECRClient.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task CreateRepository_NoTag_Success()
        {
            var awsResourceQueryer = new AWSResourceQueryer(_mockAWSClientFactory.Object);
            _mockAWSClientFactory.Setup(x => x.GetAWSClient<IAmazonECR>(It.IsAny<string>())).Returns(_mockECRClient.Object);

            await awsResourceQueryer.CreateECRRepository("myRepository", "");

            // If for some reason there is no recipe ID, verify that the tag was skipped
            _mockECRClient.Verify(x => x.CreateRepositoryAsync(
                It.Is<CreateRepositoryRequest>(request =>
                    request.RepositoryName == "myRepository" &&
                    request.Tags.Count == 0),
                It.IsAny<CancellationToken>()), Times.Once());
            _mockECRClient.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task CreateRepository_TruncatedTag_Success()
        {
            var awsResourceQueryer = new AWSResourceQueryer(_mockAWSClientFactory.Object);
            _mockAWSClientFactory.Setup(x => x.GetAWSClient<IAmazonECR>(It.IsAny<string>())).Returns(_mockECRClient.Object);

            // This is longer than ECR supports for a tag value
            var recipeName = new string('a', 500);

            await awsResourceQueryer.CreateECRRepository("myRepository", recipeName);

            // Verify that the recipe name was truncated as expected for the tag value
            _mockECRClient.Verify(x => x.CreateRepositoryAsync(
                It.Is<CreateRepositoryRequest>(request =>
                    request.RepositoryName == "myRepository" &&
                    request.Tags.Count == 1 &&
                    request.Tags[0].Key == "aws-dotnet-deploy" &&
                    request.Tags[0].Value == recipeName.Substring(0, 256)),
                It.IsAny<CancellationToken>()), Times.Once());
            _mockECRClient.VerifyNoOtherCalls();
        }
    }
}
