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
using System.Collections.Generic;
using AWS.Deploy.CLI.TypeHintResponses;
using Newtonsoft.Json;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Common.IO;

namespace AWS.Deploy.CLI.UnitTests
{
    public class TemplateMetadataReaderTests
    {
        private readonly Mock<IDeployToolWorkspaceMetadata> _deployToolWorkspaceMetadata;
        private readonly Mock<IFileManager> _fileManager;

        public TemplateMetadataReaderTests()
        {
            _deployToolWorkspaceMetadata = new Mock<IDeployToolWorkspaceMetadata>();
            _fileManager = new Mock<IFileManager>();
        }

        [Fact]
        public async Task ReadJSONMetadata()
        {
            // ARRANGE
            var templateBody = File.ReadAllText("./TestFiles/ReadJsonTemplateMetadata.json");

            var mockClient = new Mock<IAmazonCloudFormation>();
            mockClient
                .Setup(x => x.GetTemplateAsync(It.IsAny<GetTemplateRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new GetTemplateResponse
                {
                    TemplateBody = templateBody
                }));

            var templateMetadataReader = new CloudFormationTemplateReader(new TestAWSClientFactory(mockClient.Object), _deployToolWorkspaceMetadata.Object, _fileManager.Object);

            // ACT
            var metadata = await templateMetadataReader.LoadCloudApplicationMetadata("");

            // ASSERT
            Assert.Equal("SingleInstance", metadata.Settings["EnvironmentType"].ToString());
            Assert.Equal("application", metadata.Settings["LoadBalancerType"].ToString());

            var applicationIAMRole = JsonConvert.DeserializeObject<IAMRoleTypeHintResponse>(metadata.Settings["ApplicationIAMRole"].ToString());
            Assert.True(applicationIAMRole.CreateNew);
        }

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

            var templateMetadataReader = new CloudFormationTemplateReader(new TestAWSClientFactory(mockClient.Object), _deployToolWorkspaceMetadata.Object, _fileManager.Object);

            // ACT
            var metadata = await templateMetadataReader.LoadCloudApplicationMetadata("");

            // ASSERT
            Assert.Equal("aws-elasticbeanstalk-role", metadata.Settings["ApplicationIAMRole"].ToString());
        }
    }
}
