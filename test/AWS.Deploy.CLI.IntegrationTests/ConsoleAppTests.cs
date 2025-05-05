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
using Xunit;

namespace AWS.Deploy.CLI.IntegrationTests
{
    public class ConsoleAppTests : IDisposable
    {
        private readonly IServiceCollection _serviceCollection;
        private readonly CloudFormationHelper _cloudFormationHelper;
        private readonly ECSHelper _ecsHelper;
        private readonly CloudWatchLogsHelper _cloudWatchLogsHelper;
        private bool _isDisposed;
        private string? _stackName;
        private readonly TestAppManager _testAppManager;

        public ConsoleAppTests()
        {
            var ecsClient = new AmazonECSClient();
            _ecsHelper = new ECSHelper(ecsClient);

            var cloudWatchLogsClient = new AmazonCloudWatchLogsClient();
            _cloudWatchLogsHelper = new CloudWatchLogsHelper(cloudWatchLogsClient);

            _serviceCollection = new ServiceCollection();

            _serviceCollection.AddCustomServices();
            _serviceCollection.AddTestServices();

            var cloudFormationClient = new AmazonCloudFormationClient();
            _cloudFormationHelper = new CloudFormationHelper(cloudFormationClient);

            _testAppManager = new TestAppManager();
        }

        [Theory]
        [InlineData("testapps", "ConsoleAppService", "ConsoleAppService.csproj")]
        [InlineData("testapps", "ConsoleAppTask", "ConsoleAppTask.csproj")]
        public async Task DefaultConfigurations(params string[] components)
        {
            _stackName = $"{components[1]}{Guid.NewGuid().ToString().Split('-').Last()}";

            InMemoryInteractiveService interactiveService = null!;
            try
            {
                // Deploy
                var deployArgs = new[] { "deploy", "--project-path", _testAppManager.GetProjectPath(Path.Combine(components)), "--application-name", _stackName, "--diagnostics" };
                Assert.Equal(CommandReturnCodes.SUCCESS, await _serviceCollection.RunDeployToolAsync(deployArgs,
                    provider =>
                    {
                        interactiveService = provider.GetRequiredService<InMemoryInteractiveService>();

                        // Arrange input for deploy
                        interactiveService.StdInWriter.Write(Environment.NewLine); // Select default recommendation
                        interactiveService.StdInWriter.Write(Environment.NewLine); // Select default option settings
                        interactiveService.StdInWriter.Flush();
                    }));

                // Verify application is deployed and running
                Assert.Equal(StackStatus.CREATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(_stackName));

                var cluster = await _ecsHelper.GetCluster(_stackName);
                Assert.Equal("ACTIVE", cluster.Status);

                // Verify CloudWatch logs
                var logGroup = await _ecsHelper.GetLogGroup(_stackName);
                var logMessages = await _cloudWatchLogsHelper.GetLogMessages(logGroup);
                Assert.Contains("Hello World!", logMessages);

                var deployStdOut = interactiveService.StdOutReader.ReadAllLines();

                var tempCdkProjectLine = deployStdOut.First(line => line.StartsWith("Saving AWS CDK deployment project to: "));
                var tempCdkProject = tempCdkProjectLine.Split(": ")[1].Trim();
                Assert.False(Directory.Exists(tempCdkProject), $"{tempCdkProject} must not exist.");

            }
            finally
            {
                interactiveService?.ReadStdOutStartToEnd();
            }

            try
            {
                // list
                var listArgs = new[] { "list-deployments", "--diagnostics" };
                Assert.Equal(CommandReturnCodes.SUCCESS, await _serviceCollection.RunDeployToolAsync(listArgs,
                    provider =>
                    {
                        interactiveService = provider.GetRequiredService<InMemoryInteractiveService>();
                    }));

                // Verify stack exists in list of deployments
                var listStdOut = interactiveService.StdOutReader.ReadAllLines().Select(x => x.Split()[0]).ToList();
                Assert.Contains(listStdOut, (deployment) => _stackName.Equals(deployment));
            }
            finally
            {
                interactiveService?.ReadStdOutStartToEnd();
            }

            try
            {
                // Perform re-deployment
                var deployArgs = new[] { "deploy", "--project-path", _testAppManager.GetProjectPath(Path.Combine(components)), "--application-name", _stackName, "--diagnostics" };
                Assert.Equal(CommandReturnCodes.SUCCESS, await _serviceCollection.RunDeployToolAsync(deployArgs,
                    provider =>
                    {
                        interactiveService = provider.GetRequiredService<InMemoryInteractiveService>();

                        // Arrange input for re-deployment
                        interactiveService.StdInWriter.Write(Environment.NewLine); // Select default option settings
                        interactiveService.StdInWriter.Flush();
                    }));
                Assert.Equal(StackStatus.UPDATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(_stackName));
            }
            finally
            {
                interactiveService?.ReadStdOutStartToEnd();
            }

            try
            {
                // Delete
                var deleteArgs = new[] { "delete-deployment", _stackName, "--diagnostics" };
                Assert.Equal(CommandReturnCodes.SUCCESS, await _serviceCollection.RunDeployToolAsync(deleteArgs,
                    provider =>
                    {
                        interactiveService = provider.GetRequiredService<InMemoryInteractiveService>();

                        // Arrange input for delete
                        interactiveService.StdInWriter.Write("y"); // Select default option settings
                        interactiveService.StdInWriter.Flush();
                    }));

                // Verify application is deleted
                Assert.True(await _cloudFormationHelper.IsStackDeleted(_stackName), $"{_stackName} still exists.");
            }
            finally
            {
                interactiveService?.ReadStdOutStartToEnd();
            }
        }

        [Theory]
        [InlineData("testapps", "ConsoleAppArmDeployment", "ConsoleAppArmDeployment.csproj")]
        public async Task FargateArmDeployment(params string[] components)
        {
            _stackName = $"{components[1]}Arm{Guid.NewGuid().ToString().Split('-').Last()}";

            InMemoryInteractiveService interactiveService = null!;
            try
            {
                // Deploy
                var deployArgs = new[] { "deploy", "--project-path", _testAppManager.GetProjectPath(Path.Combine(components)), "--application-name", _stackName, "--diagnostics" };
                Assert.Equal(CommandReturnCodes.SUCCESS, await _serviceCollection.RunDeployToolAsync(deployArgs,
                    provider =>
                    {
                        interactiveService = provider.GetRequiredService<InMemoryInteractiveService>();

                        // Arrange input for deploy
                        interactiveService.StdInWriter.Write(Environment.NewLine); // Select default recommendation
                        interactiveService.StdInWriter.WriteLine("7"); // Select "Environment Architecture"
                        interactiveService.StdInWriter.WriteLine("2"); // Select "Arm64"
                        interactiveService.StdInWriter.Write(Environment.NewLine); // Confirm selection and deploy
                        interactiveService.StdInWriter.Flush();
                    }));

                // Verify application is deployed and running
                Assert.Equal(StackStatus.CREATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(_stackName));

                var cluster = await _ecsHelper.GetCluster(_stackName);
                Assert.Equal("ACTIVE", cluster.Status);

                // Verify CloudWatch logs
                var logGroup = await _ecsHelper.GetLogGroup(_stackName);
                var logMessages = await _cloudWatchLogsHelper.GetLogMessages(logGroup);
                Assert.Contains("Hello World!", logMessages);

                var deployStdOut = interactiveService.StdOutReader.ReadAllLines();

                var tempCdkProjectLine = deployStdOut.First(line => line.StartsWith("Saving AWS CDK deployment project to: "));
                var tempCdkProject = tempCdkProjectLine.Split(": ")[1].Trim();
                Assert.False(Directory.Exists(tempCdkProject), $"{tempCdkProject} must not exist.");
            }
            finally
            {
                interactiveService?.ReadStdOutStartToEnd();
            }

            try
            {
                // list
                var listArgs = new[] { "list-deployments", "--diagnostics" };
                Assert.Equal(CommandReturnCodes.SUCCESS, await _serviceCollection.RunDeployToolAsync(listArgs,
                    provider =>
                    {
                        interactiveService = provider.GetRequiredService<InMemoryInteractiveService>();
                    }));

                // Verify stack exists in list of deployments
                var listStdOut = interactiveService.StdOutReader.ReadAllLines().Select(x => x.Split()[0]).ToList();
                Assert.Contains(listStdOut, (deployment) => _stackName.Equals(deployment));
            }
            finally
            {
                interactiveService?.ReadStdOutStartToEnd();
            }

            try
            {
                // Perform re-deployment
                var deployArgs = new[] { "deploy", "--project-path", _testAppManager.GetProjectPath(Path.Combine(components)), "--application-name", _stackName, "--diagnostics" };
                Assert.Equal(CommandReturnCodes.SUCCESS, await _serviceCollection.RunDeployToolAsync(deployArgs,
                    provider =>
                    {
                        interactiveService = provider.GetRequiredService<InMemoryInteractiveService>();

                        // Arrange input for re-deployment
                        interactiveService.StdInWriter.Write(Environment.NewLine); // Select default option settings
                        interactiveService.StdInWriter.Flush();
                    }));
                Assert.Equal(StackStatus.UPDATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(_stackName));
            }
            finally
            {
                interactiveService?.ReadStdOutStartToEnd();
            }

            try
            {
                // Delete
                var deleteArgs = new[] { "delete-deployment", _stackName, "--diagnostics" };
                Assert.Equal(CommandReturnCodes.SUCCESS, await _serviceCollection.RunDeployToolAsync(deleteArgs,
                    provider =>
                    {
                        interactiveService = provider.GetRequiredService<InMemoryInteractiveService>();

                        // Arrange input for delete
                        interactiveService.StdInWriter.Write("y"); // Select default option settings
                        interactiveService.StdInWriter.Flush();
                    }));

                // Verify application is deleted
                Assert.True(await _cloudFormationHelper.IsStackDeleted(_stackName), $"{_stackName} still exists.");
            }
            finally
            {
                interactiveService?.ReadStdOutStartToEnd();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing && !string.IsNullOrEmpty(_stackName))
            {
                var isStackDeleted = _cloudFormationHelper.IsStackDeleted(_stackName).GetAwaiter().GetResult();
                if (!isStackDeleted)
                {
                    _cloudFormationHelper.DeleteStack(_stackName).GetAwaiter().GetResult();
                }
            }

            _isDisposed = true;
        }

        ~ConsoleAppTests()
        {
            Dispose(false);
        }
    }
}
