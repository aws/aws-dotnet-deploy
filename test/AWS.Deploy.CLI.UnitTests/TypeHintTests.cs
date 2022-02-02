// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Moq;
using Xunit;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.Deploy.CLI.Commands.TypeHints;
using AWS.Deploy.Common;
using AWS.Deploy.Orchestration.Data;
using AWS.Deploy.CLI.UnitTests.Utilities;

namespace AWS.Deploy.CLI.UnitTests
{
    public class TypeHintTests
    {
        [Fact]
        public async Task TestDynamoDBTableNameTypeHint()
        {
            var listPaginator = new Mock<IListTablesPaginator>();
            listPaginator.Setup(x => x.TableNames)
                .Returns(new MockPaginatedEnumerable<string>(new string[] { "Table1", "Table2" }));

            var ddbPaginators = new Mock<IDynamoDBv2PaginatorFactory>();
            ddbPaginators.Setup(x => x.ListTables(It.IsAny<ListTablesRequest>()))
                    .Returns(listPaginator.Object);

            var ddbClient = new Mock<IAmazonDynamoDB>();
            ddbClient.Setup(x => x.Paginators)
                    .Returns(ddbPaginators.Object);

            var awsClientFactory = new Mock<IAWSClientFactory>();
            awsClientFactory.Setup(x => x.GetAWSClient<IAmazonDynamoDB>(It.IsAny<string>()))
                    .Returns(ddbClient.Object);

            var awsResourceQueryer = new AWSResourceQueryer(awsClientFactory.Object);
            var typeHintCommand = new DynamoDBTableCommand(awsResourceQueryer, null);

            var resources = await typeHintCommand.GetResources(null, null);
            Assert.Equal(2, resources.Count);
            Assert.Equal("Table1", resources[0].DisplayName);
            Assert.Equal("Table1", resources[0].SystemName);
            Assert.Equal("Table2", resources[1].DisplayName);
            Assert.Equal("Table2", resources[1].SystemName);
        }

        [Fact]
        public async Task TestSQSQueueUrlTypeHint()
        {
            var listPaginator = new Mock<IListQueuesPaginator>();
            listPaginator.Setup(x => x.QueueUrls)
                .Returns(new MockPaginatedEnumerable<string>(new string[] { "https://sqs.us-west-2.amazonaws.com/123412341234/queue1", "https://sqs.us-west-2.amazonaws.com/123412341234/queue2" }));

            var sqsPaginators = new Mock<ISQSPaginatorFactory>();
            sqsPaginators.Setup(x => x.ListQueues(It.IsAny<ListQueuesRequest>()))
                    .Returns(listPaginator.Object);

            var sqsClient = new Mock<IAmazonSQS>();
            sqsClient.Setup(x => x.Paginators)
                    .Returns(sqsPaginators.Object);

            var awsClientFactory = new Mock<IAWSClientFactory>();
            awsClientFactory.Setup(x => x.GetAWSClient<IAmazonSQS>(It.IsAny<string>()))
                    .Returns(sqsClient.Object);

            var awsResourceQueryer = new AWSResourceQueryer(awsClientFactory.Object);
            var typeHintCommand = new SQSQueueUrlCommand(awsResourceQueryer, null);

            var resources = await typeHintCommand.GetResources(null, null);
            Assert.Equal(2, resources.Count);
            Assert.Equal("queue1", resources[0].DisplayName);
            Assert.Equal("https://sqs.us-west-2.amazonaws.com/123412341234/queue1", resources[0].SystemName);
            Assert.Equal("queue2", resources[1].DisplayName);
            Assert.Equal("https://sqs.us-west-2.amazonaws.com/123412341234/queue2", resources[1].SystemName);
        }

        [Fact]
        public async Task TestSNSTopicArnTypeHint()
        {
            var listPaginator = new Mock<IListTopicsPaginator>();
            listPaginator.Setup(x => x.Topics)
                .Returns(new MockPaginatedEnumerable<Topic>(new Topic[] { new Topic { TopicArn = "arn:aws:sns:us-west-2:123412341234:Topic1" }, new Topic { TopicArn = "arn:aws:sns:us-west-2:123412341234:Topic2" } }));

            var snsPaginators = new Mock<ISimpleNotificationServicePaginatorFactory>();
            snsPaginators.Setup(x => x.ListTopics(It.IsAny<ListTopicsRequest>()))
                    .Returns(listPaginator.Object);

            var snsClient = new Mock<IAmazonSimpleNotificationService>();
            snsClient.Setup(x => x.Paginators)
                    .Returns(snsPaginators.Object);

            var awsClientFactory = new Mock<IAWSClientFactory>();
            awsClientFactory.Setup(x => x.GetAWSClient<IAmazonSimpleNotificationService>(It.IsAny<string>()))
                    .Returns(snsClient.Object);

            var awsResourceQueryer = new AWSResourceQueryer(awsClientFactory.Object);
            var typeHintCommand = new SNSTopicArnsCommand(awsResourceQueryer, null);

            var resources = await typeHintCommand.GetResources(null, null);
            Assert.Equal(2, resources.Count);
            Assert.Equal("Topic1", resources[0].DisplayName);
            Assert.Equal("arn:aws:sns:us-west-2:123412341234:Topic1", resources[0].SystemName);
            Assert.Equal("Topic2", resources[1].DisplayName);
            Assert.Equal("arn:aws:sns:us-west-2:123412341234:Topic2", resources[1].SystemName);
        }

        [Fact]
        public async Task TestS3BucketNameTypeHint()
        {
            var s3Client = new Mock<IAmazonS3>();
            s3Client.Setup(x => x.ListBucketsAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(new ListBucketsResponse { Buckets = new List<S3Bucket> { new S3Bucket {BucketName = "Bucket1" }, new S3Bucket { BucketName = "Bucket2" } } }));

            var awsClientFactory = new Mock<IAWSClientFactory>();
            awsClientFactory.Setup(x => x.GetAWSClient<IAmazonS3>(It.IsAny<string>()))
                    .Returns(s3Client.Object);

            var awsResourceQueryer = new AWSResourceQueryer(awsClientFactory.Object);
            var typeHintCommand = new S3BucketNameCommand(awsResourceQueryer, null);

            var resources = await typeHintCommand.GetResources(null, null);
            Assert.Equal(2, resources.Count);
            Assert.Equal("Bucket1", resources[0].DisplayName);
            Assert.Equal("Bucket1", resources[0].SystemName);
            Assert.Equal("Bucket2", resources[1].DisplayName);
            Assert.Equal("Bucket2", resources[1].SystemName);
        }
    }
}
