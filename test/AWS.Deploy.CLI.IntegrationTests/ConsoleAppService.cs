// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudWatchLogs;
using Amazon.ECS;
using AWS.Deploy.CLI.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Helpers;
using AWS.Deploy.CLI.IntegrationTests.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AWS.Deploy.CLI.IntegrationTests
{

    public class ConsoleAppServiceTest
    {
        private readonly HttpHelper _httpHelper;
        private readonly CloudFormationHelper _cloudFormationHelper;
        private readonly ECSHelper _ecsHelper;
        private readonly CloudWatchLogsHelper _cloudWatchLogsHelper;

        public ConsoleAppServiceTest()
        {
            _httpHelper = new HttpHelper();

            var cloudFormationClient = new AmazonCloudFormationClient();
            _cloudFormationHelper = new CloudFormationHelper(cloudFormationClient);

            var ecsClient = new AmazonECSClient();
            _ecsHelper = new ECSHelper(ecsClient);

            var cloudWatchLogsClient = new AmazonCloudWatchLogsClient();
            _cloudWatchLogsHelper = new CloudWatchLogsHelper(cloudWatchLogsClient);
        }

        [Fact]
        public async Task DefaultConfigurations()
        {
            var projectPath = Path.Combine("testapps", "ConsoleAppService", "ConsoleAppService.csproj");
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddCustomServices();
            serviceCollection.AddTestServices();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            // calls the Run method in App, which is replacing Main
            var app = serviceProvider.GetService<App>();
            Assert.NotNull(app);

            var toolInteractiveService = (InMemoryInteractiveService)serviceProvider.GetService<IToolInteractiveService>();
            Assert.NotNull(toolInteractiveService);

            var stackName = $"ConsoleAppService{Guid.NewGuid().ToString().Split('-').Last()}";

            await toolInteractiveService.StdInWriter.WriteAsync(Environment.NewLine);
            await toolInteractiveService.StdInWriter.WriteAsync(Environment.NewLine);
            await toolInteractiveService.StdInWriter.FlushAsync();

            var deployArgs = new[] { "deploy", "--project-path", projectPath, "--stack-name", stackName };

            await app.Run(deployArgs);

            Assert.Equal(StackStatus.CREATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(stackName));

            var cluster = await _ecsHelper.GetCluster(stackName);
            Assert.Equal("ACTIVE", cluster.Status);

            var logGroup = await _ecsHelper.GetLogGroup(stackName);
            var logMessages = await _cloudWatchLogsHelper.GetLogMessages(logGroup);
            Assert.Contains(logMessages, message => message.Equals("Hello World!"));

            await toolInteractiveService.StdInWriter.WriteAsync("y");
            await toolInteractiveService.StdInWriter.FlushAsync();
            var deleteArgs = new[] { "delete-deployment", stackName };
            await app.Run(deleteArgs);

            var exception = await Assert.ThrowsAsync<AmazonCloudFormationException>(async () =>
            {
                await _cloudFormationHelper.GetStackStatus(stackName);
            });
            Assert.Equal($"Stack with id {stackName} does not exist", exception.Message);
        }
    }
}
