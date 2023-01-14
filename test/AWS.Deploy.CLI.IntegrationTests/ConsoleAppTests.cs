// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudWatchLogs;
using Amazon.ECS;
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.CLI.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Helpers;
using AWS.Deploy.CLI.IntegrationTests.Services;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace AWS.Deploy.CLI.IntegrationTests
{
    [TestFixture]
    public class ConsoleAppTests
    {
        private CloudFormationHelper _cloudFormationHelper;
        private ECSHelper _ecsHelper;
        private CloudWatchLogsHelper _cloudWatchLogsHelper;
        private App _app;
        private InMemoryInteractiveService _interactiveService;
        private string _stackName;
        private TestAppManager _testAppManager;

        [SetUp]
        public void Initialize()
        {
            var ecsClient = new AmazonECSClient();
            _ecsHelper = new ECSHelper(ecsClient);

            var cloudWatchLogsClient = new AmazonCloudWatchLogsClient();
            _cloudWatchLogsHelper = new CloudWatchLogsHelper(cloudWatchLogsClient);

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddCustomServices();
            serviceCollection.AddTestServices();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            _app = serviceProvider.GetService<App>();
            Assert.NotNull(_app);

            _interactiveService = serviceProvider.GetService<InMemoryInteractiveService>();
            Assert.NotNull(_interactiveService);

            var cloudFormationClient = new AmazonCloudFormationClient();
            _cloudFormationHelper = new CloudFormationHelper(cloudFormationClient);

            _testAppManager = new TestAppManager();
        }

        [Test]
        [TestCase("testapps", "ConsoleAppService", "ConsoleAppService.csproj")]
        [TestCase("testapps", "ConsoleAppTask", "ConsoleAppTask.csproj")]
        public async Task DefaultConfigurations(params string[] components)
        {
            _stackName = $"{components[1]}{Guid.NewGuid().ToString().Split('-').Last()}";

            // Arrange input for deploy
            await _interactiveService.StdInWriter.WriteAsync(Environment.NewLine); // Select default recommendation
            await _interactiveService.StdInWriter.WriteAsync(Environment.NewLine); // Select default option settings
            await _interactiveService.StdInWriter.FlushAsync();

            // Deploy
            var deployArgs = new[] { "deploy", "--project-path", _testAppManager.GetProjectPath(Path.Combine(components)), "--application-name", _stackName, "--diagnostics" };
            Assert.AreEqual(CommandReturnCodes.SUCCESS, await _app.Run(deployArgs));

            // Verify application is deployed and running
            Assert.AreEqual(StackStatus.CREATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(_stackName));

            var cluster = await _ecsHelper.GetCluster(_stackName);
            Assert.AreEqual("ACTIVE", cluster.Status);

            // Verify CloudWatch logs
            var logGroup = await _ecsHelper.GetLogGroup(_stackName);
            var logMessages = await _cloudWatchLogsHelper.GetLogMessages(logGroup);
            CollectionAssert.Contains(logMessages, "Hello World!");

            var deployStdOut = _interactiveService.StdOutReader.ReadAllLines();

            var tempCdkProjectLine = deployStdOut.First(line => line.StartsWith("Saving AWS CDK deployment project to: "));
            var tempCdkProject = tempCdkProjectLine.Split(": ")[1].Trim();
            Assert.False(Directory.Exists(tempCdkProject), $"{tempCdkProject} must not exist.");

            // list
            var listArgs = new[] { "list-deployments", "--diagnostics" };
            Assert.AreEqual(CommandReturnCodes.SUCCESS, await _app.Run(listArgs));;

            // Verify stack exists in list of deployments
            var listStdOut = _interactiveService.StdOutReader.ReadAllLines().Select(x => x.Split()[0]).ToList();
            CollectionAssert.Contains(listStdOut, _stackName);

            // Arrange input for re-deployment
            await _interactiveService.StdInWriter.WriteAsync(Environment.NewLine); // Select default option settings
            await _interactiveService.StdInWriter.FlushAsync();

            // Perform re-deployment
            deployArgs = new[] { "deploy", "--project-path", _testAppManager.GetProjectPath(Path.Combine(components)), "--application-name", _stackName, "--diagnostics" };
            Assert.AreEqual(CommandReturnCodes.SUCCESS, await _app.Run(deployArgs));
            Assert.AreEqual(StackStatus.UPDATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(_stackName));

            // Arrange input for delete
            await _interactiveService.StdInWriter.WriteAsync("y"); // Confirm delete
            await _interactiveService.StdInWriter.FlushAsync();
            var deleteArgs = new[] { "delete-deployment", _stackName, "--diagnostics" };

            // Delete
            Assert.AreEqual(CommandReturnCodes.SUCCESS, await _app.Run(deleteArgs));;

            // Verify application is deleted
            Assert.True(await _cloudFormationHelper.IsStackDeleted(_stackName), $"{_stackName} still exists.");
        }

        [TearDown]
        public async Task Cleanup()
        {
            var isStackDeleted = await _cloudFormationHelper.IsStackDeleted(_stackName);
            if (!isStackDeleted)
            {
                await _cloudFormationHelper.DeleteStack(_stackName);
            }

            _interactiveService.ReadStdOutStartToEnd();
        }
    }
}
