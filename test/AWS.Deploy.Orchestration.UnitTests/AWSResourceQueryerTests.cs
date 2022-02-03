// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading;
using System.Threading.Tasks;
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

        public AWSResourceQueryerTests()
        {
            _mockAWSClientFactory = new Mock<IAWSClientFactory>();
            _mockSTSClient = new Mock<IAmazonSecurityTokenService>();
            _mockSTSClientDefaultRegion = new Mock<IAmazonSecurityTokenService>();
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
    }
}
