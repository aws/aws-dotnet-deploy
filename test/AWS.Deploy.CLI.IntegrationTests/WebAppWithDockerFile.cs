// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using Amazon.CloudFormation;
using Amazon.ECS;
using Amazon.ECS.Model;
using AWS.Deploy.CLI.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Helpers;
using AWS.Deploy.CLI.IntegrationTests.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace AWS.Deploy.CLI.IntegrationTests
{
    public class WebAppWithDockerFileTest
    {
        private readonly HttpHelper _httpHelper;
        private readonly CloudFormationHelper _cloudFormationHelper;
        private readonly ECSHelper _ecsHelper;

        public WebAppWithDockerFileTest()
        {
            _httpHelper = new HttpHelper();

            var cloudFormationClient = new AmazonCloudFormationClient();
            _cloudFormationHelper = new CloudFormationHelper(cloudFormationClient);

            var ecsClient = new AmazonECSClient();
            _ecsHelper = new ECSHelper(ecsClient);
        }

        [Fact]
        public async Task DefaultConfigurations()
        {
            var projectPath = Path.Combine("testapps", "WebAppWithDockerFile", "WebAppWithDockerFile.csproj");
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddCustomServices();
            serviceCollection.AddTestServices();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            // calls the Run method in App, which is replacing Main
            var app = serviceProvider.GetService<App>();
            Assert.NotNull(app);

            var toolInteractiveService = (InMemoryInteractiveService)serviceProvider.GetService<IToolInteractiveService>();
            Assert.NotNull(toolInteractiveService);

            var stackName = $"WebAppWithDockerFile{Guid.NewGuid().ToString().Split('-').Last()}";
            //var stackName = $"WebAppWithDockerFile";

            await toolInteractiveService.StdInWriter.WriteAsync(Environment.NewLine);
            await toolInteractiveService.StdInWriter.WriteAsync(Environment.NewLine);
            await toolInteractiveService.StdInWriter.FlushAsync();

            var deployArgs = new[] { "deploy", "--project-path", projectPath, "--stack-name", stackName };

            await app.Run(deployArgs);

            Assert.Equal(StackStatus.CREATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(stackName));

            var cluster = await _ecsHelper.GetCluster(stackName);
            Assert.Equal("ACTIVE", cluster.Status);

            var deployStdOut = toolInteractiveService.StdOutReader.ReadAllLines();

            // Example: WebAppWithDockerFile-3d078c3ca551.FargateServiceServiceURL47701F45 = http://WebAp-Farga-12O3W5VNB5OLC-166471465.us-west-2.elb.amazonaws.com
            var applicationUrl = deployStdOut.First(line => line.StartsWith($"{stackName}.FargateServiceServiceURL"))
                .Split("=")[1]
                .Trim();
            Assert.True(await _httpHelper.IsSuccessStatusCode(applicationUrl));

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
