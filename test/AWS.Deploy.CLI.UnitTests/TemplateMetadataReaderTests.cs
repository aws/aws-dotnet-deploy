// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Xunit;
using AWS.Deploy.Orchestration.Utilities;
using Moq;

namespace AWS.Deploy.CLI.UnitTests
{
    public class TemplateMetadataReaderTests
    {
        [Fact]
        public async Task ReadYamlMetadata()
        {
            // ARRANGE
            var templateBody = File.ReadAllText("./TestFiles/ReadYamlTemplateMetadata.yml");

            var mockClient = new Mock<IAmazonCloudFormation>();
            mockClient
                .Setup(x => x.GetTemplateAsync(It.IsAny<GetTemplateRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new GetTemplateResponse
                {
                    TemplateBody = templateBody
                }));

            var templateMetadataReader = new TemplateMetadataReader(new TestAWSClientFactory(mockClient.Object));

            // ACT
            var metadata = await templateMetadataReader.LoadCloudApplicationMetadata("");

            // ASSERT
            Assert.Equal("aws-elasticbeanstalk-role", metadata.Settings["ApplicationIAMRole"].ToString());
        }
    }
}
