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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AWS.Deploy.CLI.IntegrationTests
{
    [TestClass]
    public class ConsoleAppTests : IDisposable
    {
        private readonly CloudFormationHelper _cloudFormationHelper;
        private readonly ECSHelper _ecsHelper;
        private readonly CloudWatchLogsHelper _cloudWatchLogsHelper;
        private readonly App _app;
        private readonly InMemoryInteractiveService _interactiveService;
        private bool _isDisposed;
        private string _stackName;
        private readonly TestAppManager _testAppManager;

        public ConsoleAppTests()
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
            Assert.IsNotNull(_app);

            _interactiveService = serviceProvider.GetService<InMemoryInteractiveService>();
            Assert.IsNotNull(_interactiveService);

            var cloudFormationClient = new AmazonCloudFormationClient();
            _cloudFormationHelper = new CloudFormationHelper(cloudFormationClient);

            _testAppManager = new TestAppManager();
        }

        [TestMethod]
        [DataRow("testapps", "ConsoleAppService", "ConsoleAppService.csproj")]
        [DataRow("testapps", "ConsoleAppTask", "ConsoleAppTask.csproj")]
        public async Task DefaultConfigurations(params string[] components)
        {
            _stackName = $"{components[1]}{Guid.NewGuid().ToString().Split('-').Last()}";

            // Arrange input for deploy
            await _interactiveService.StdInWriter.WriteAsync(Environment.NewLine); // Select default recommendation
            await _interactiveService.StdInWriter.WriteAsync(Environment.NewLine); // Select default option settings
            await _interactiveService.StdInWriter.FlushAsync();

            // Deploy
            var deployArgs = new[] { "deploy", "--project-path", _testAppManager.GetProjectPath(Path.Combine(components)), "--application-name", _stackName, "--diagnostics", "--direct-deploy" };
            Assert.AreEqual(CommandReturnCodes.SUCCESS, await _app.Run(deployArgs));

            // Verify application is deployed and running
            Assert.AreEqual(StackStatus.CREATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(_stackName));

            var cluster = await _ecsHelper.GetCluster(_stackName);
            Assert.AreEqual("ACTIVE", cluster.Status);

            // Verify CloudWatch logs
            var logGroup = await _ecsHelper.GetLogGroup(_stackName);
            var logMessages = await _cloudWatchLogsHelper.GetLogMessages(logGroup);
            //Assert.Contains("Hello World!", logMessages);

            var deployStdOut = _interactiveService.StdOutReader.ReadAllLines();

            var tempCdkProjectLine = deployStdOut.First(line => line.StartsWith("Saving AWS CDK deployment project to: "));
            var tempCdkProject = tempCdkProjectLine.Split(": ")[1].Trim();
            Assert.IsFalse(Directory.Exists(tempCdkProject), $"{tempCdkProject} must not exist.");

            // list
            var listArgs = new[] { "list-deployments", "--diagnostics" };
            Assert.AreEqual(CommandReturnCodes.SUCCESS, await _app.Run(listArgs));;

            // Verify stack exists in list of deployments
            var listStdOut = _interactiveService.StdOutReader.ReadAllLines().Select(x => x.Split()[0]).ToList();
            //Assert.Contains(listStdOut, (deployment) => _stackName.Equals(deployment));

            // Arrange input for re-deployment
            await _interactiveService.StdInWriter.WriteAsync(Environment.NewLine); // Select default option settings
            await _interactiveService.StdInWriter.FlushAsync();

            // Perform re-deployment
            deployArgs = new[] { "deploy", "--project-path", _testAppManager.GetProjectPath(Path.Combine(components)), "--application-name", _stackName, "--diagnostics", "--direct-deploy" };
            Assert.AreEqual(CommandReturnCodes.SUCCESS, await _app.Run(deployArgs));
            Assert.AreEqual(StackStatus.UPDATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(_stackName));

            // Arrange input for delete
            await _interactiveService.StdInWriter.WriteAsync("y"); // Confirm delete
            await _interactiveService.StdInWriter.FlushAsync();
            var deleteArgs = new[] { "delete-deployment", _stackName, "--diagnostics" };

            // Delete
            Assert.AreEqual(CommandReturnCodes.SUCCESS, await _app.Run(deleteArgs));;

            // Verify application is deleted
            Assert.IsTrue(await _cloudFormationHelper.IsStackDeleted(_stackName), $"{_stackName} still exists.");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                var isStackDeleted = _cloudFormationHelper.IsStackDeleted(_stackName).GetAwaiter().GetResult();
                if (!isStackDeleted)
                {
                    _cloudFormationHelper.DeleteStack(_stackName).GetAwaiter().GetResult();
                }

                _interactiveService.ReadStdOutStartToEnd();
            }

            _isDisposed = true;
        }

        ~ConsoleAppTests()
        {
            Dispose(false);
        }
    }
}
