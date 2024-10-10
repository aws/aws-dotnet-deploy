// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.ElasticBeanstalk.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.Deploy.CLI.Commands.TypeHints;
using AWS.Deploy.CLI.UnitTests.Utilities;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Recipes.Validation;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.Data;
using Moq;
using Xunit;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Common.IO;
using Should;

namespace AWS.Deploy.CLI.UnitTests
{
    public class TypeHintTests
    {
        private readonly IOptionSettingHandler _optionSettingHandler;
        private readonly Mock<IAWSResourceQueryer> _awsResourceQueryer;
        private readonly Mock<IServiceProvider> _serviceProvider;

        public TypeHintTests()
        {
            _awsResourceQueryer = new Mock<IAWSResourceQueryer>();
            _serviceProvider = new Mock<IServiceProvider>();
            _serviceProvider
                .Setup(x => x.GetService(typeof(IAWSResourceQueryer)))
                .Returns(_awsResourceQueryer.Object);
            _optionSettingHandler = new OptionSettingHandler(new ValidatorFactory(_serviceProvider.Object));
        }

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
            var typeHintCommand = new DynamoDBTableCommand(awsResourceQueryer, null, _optionSettingHandler);

            var resources = await typeHintCommand.GetResources(null, null);
            Assert.Equal(2, resources.Rows.Count);
            Assert.Equal("Table1", resources.Rows[0].DisplayName);
            Assert.Equal("Table1", resources.Rows[0].SystemName);
            Assert.Equal("Table2", resources.Rows[1].DisplayName);
            Assert.Equal("Table2", resources.Rows[1].SystemName);
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
            var typeHintCommand = new SQSQueueUrlCommand(awsResourceQueryer, null, _optionSettingHandler);

            var resources = await typeHintCommand.GetResources(null, null);
            Assert.Equal(2, resources.Rows.Count);
            Assert.Equal("queue1", resources.Rows[0].DisplayName);
            Assert.Equal("https://sqs.us-west-2.amazonaws.com/123412341234/queue1", resources.Rows[0].SystemName);
            Assert.Equal("queue2", resources.Rows[1].DisplayName);
            Assert.Equal("https://sqs.us-west-2.amazonaws.com/123412341234/queue2", resources.Rows[1].SystemName);
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
            var typeHintCommand = new SNSTopicArnsCommand(awsResourceQueryer, null, _optionSettingHandler);

            var resources = await typeHintCommand.GetResources(null, null);
            Assert.Equal(2, resources.Rows.Count);
            Assert.Equal("Topic1", resources.Rows[0].DisplayName);
            Assert.Equal("arn:aws:sns:us-west-2:123412341234:Topic1", resources.Rows[0].SystemName);
            Assert.Equal("Topic2", resources.Rows[1].DisplayName);
            Assert.Equal("arn:aws:sns:us-west-2:123412341234:Topic2", resources.Rows[1].SystemName);
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
            var typeHintCommand = new S3BucketNameCommand(awsResourceQueryer, null, _optionSettingHandler);

            var resources = await typeHintCommand.GetResources(null, null);
            Assert.Equal(2, resources.Rows.Count);
            Assert.Equal("Bucket1", resources.Rows[0].DisplayName);
            Assert.Equal("Bucket1", resources.Rows[0].SystemName);
            Assert.Equal("Bucket2", resources.Rows[1].DisplayName);
            Assert.Equal("Bucket2", resources.Rows[1].SystemName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(new [] { SupportedArchitecture.X86_64 })]
        [InlineData(new [] { SupportedArchitecture.Arm64 })]
        [InlineData(new [] { SupportedArchitecture.X86_64, SupportedArchitecture.Arm64 })]
        public async Task TestEnvironmentArchitectureTypeHint_NoArchitectureDefined(SupportedArchitecture[] architectures)
        {
            var archList = architectures?.ToList();
            var recipeDefinition = new Mock<RecipeDefinition>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DeploymentTypes>(),
                It.IsAny<DeploymentBundleTypes>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()).Object;
            var projectDefinitionParser = new ProjectDefinitionParser(new FileManager(), new DirectoryManager());
            var projectPath = SystemIOUtilities.ResolvePath("ConsoleAppTask");
            var project = await projectDefinitionParser.Parse(projectPath);
            recipeDefinition.SupportedArchitectures = archList;
            var recommendation = new Recommendation(recipeDefinition, project, 0, new Dictionary<string, object>());

            var typeHintCommand = new EnvironmentArchitectureCommand(null, _optionSettingHandler);

            var resources = await typeHintCommand.GetResources(recommendation, null);

            if (archList is null)
            {
                var expectedArchitecture = SupportedArchitecture.X86_64.ToString();
                var row = Assert.Single(resources.Rows);
                Assert.Equal(expectedArchitecture, row.SystemName);
                Assert.Equal(expectedArchitecture, row.DisplayName);
            }
            else
            {
                Assert.Equal(architectures.Length, resources.Rows.Count);
                foreach (var row in resources.Rows)
                {
                    Assert.Contains(archList, x => x.ToString().Equals(row.DisplayName));
                    Assert.Contains(archList, x => x.ToString().Equals(row.SystemName));
                }
            }
        }

        [Fact]
        public async Task DotnetBeanstalkPlatformArnCommandTest()
        {
            var recipeDefinition = new Mock<RecipeDefinition>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DeploymentTypes>(),
                It.IsAny<DeploymentBundleTypes>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()).Object;
            var projectDefinitionParser = new ProjectDefinitionParser(new FileManager(), new DirectoryManager());
            var projectPath = SystemIOUtilities.ResolvePath("ConsoleAppTask");
            var project = await projectDefinitionParser.Parse(projectPath);
            var recommendation = new Recommendation(recipeDefinition, project, 0, new Dictionary<string, object>());

            var platformSummaries = new List<PlatformSummary>
            {
                new PlatformSummary
                {
                    PlatformArn = "arn:aws:elasticbeanstalk:us-west-2::platform/.NET 8 running on 64bit Amazon Linux 2023/3.1.3",
                    PlatformBranchName = ".NET 8 running on 64bit Amazon Linux 2023",
                    PlatformVersion = "3.1.3",
                }
            };
            var awsResourceQueryer = new Mock<IAWSResourceQueryer>();
            awsResourceQueryer
                .Setup(x => x.GetElasticBeanstalkPlatformArns(It.IsAny<string>(), BeanstalkPlatformType.Linux))
                .ReturnsAsync(platformSummaries);
            var typeHintCommand = new DotnetBeanstalkPlatformArnCommand(awsResourceQueryer.Object, null, _optionSettingHandler);
            var resources = await typeHintCommand.GetResources(recommendation, null);

            var row = Assert.Single(resources.Rows);
            Assert.Equal(".NET 8 running on 64bit Amazon Linux 2023 v3.1.3", row.DisplayName);
            Assert.Equal("arn:aws:elasticbeanstalk:us-west-2::platform/.NET 8 running on 64bit Amazon Linux 2023/3.1.3", row.SystemName);
            Assert.Equal(2, row.ColumnValues.Count);
            Assert.Equal(".NET 8 running on 64bit Amazon Linux 2023", row.ColumnValues[0]);
            Assert.Equal("3.1.3", row.ColumnValues[1]);
        }

        [Fact]
        public async Task DotnetWindowsBeanstalkPlatformArnCommandTest()
        {
            var recipeDefinition = new Mock<RecipeDefinition>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DeploymentTypes>(),
                It.IsAny<DeploymentBundleTypes>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()).Object;
            var projectDefinitionParser = new ProjectDefinitionParser(new FileManager(), new DirectoryManager());
            var projectPath = SystemIOUtilities.ResolvePath("ConsoleAppTask");
            var project = await projectDefinitionParser.Parse(projectPath);
            var recommendation = new Recommendation(recipeDefinition, project, 0, new Dictionary<string, object>());

            var platformSummaries = new List<PlatformSummary>
            {
                new PlatformSummary
                {
                    PlatformArn = "arn:aws:elasticbeanstalk:us-west-2::platform/IIS 10.0 running on 64bit Windows Server 2016/2.0.0",
                    PlatformBranchName = "IIS 10.0 running on 64bit Windows Server 2016",
                    PlatformVersion = "2.0.0"
                }
            };
            var awsResourceQueryer = new Mock<IAWSResourceQueryer>();
            awsResourceQueryer
                .Setup(x => x.GetElasticBeanstalkPlatformArns(It.IsAny<string>(), BeanstalkPlatformType.Windows))
                .ReturnsAsync(platformSummaries);
            var typeHintCommand = new DotnetWindowsBeanstalkPlatformArnCommand(awsResourceQueryer.Object, null, _optionSettingHandler);
            var resources = await typeHintCommand.GetResources(recommendation, null);

            var row = Assert.Single(resources.Rows);
            Assert.Equal("IIS 10.0 running on 64bit Windows Server 2016 v2.0.0", row.DisplayName);
            Assert.Equal("arn:aws:elasticbeanstalk:us-west-2::platform/IIS 10.0 running on 64bit Windows Server 2016/2.0.0", row.SystemName);
            Assert.Equal(2, row.ColumnValues.Count);
            Assert.Equal("IIS 10.0 running on 64bit Windows Server 2016", row.ColumnValues[0]);
            Assert.Equal("2.0.0", row.ColumnValues[1]);
        }
    }
}
