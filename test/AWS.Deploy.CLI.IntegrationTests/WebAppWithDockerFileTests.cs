// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using Amazon.CloudFormation;
using Amazon.ECS;
using Amazon.ECS.Model;
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.CLI.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Helpers;
using AWS.Deploy.CLI.IntegrationTests.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace AWS.Deploy.CLI.IntegrationTests
{
    public class WebAppWithDockerFileTests : IDisposable
    {
        private readonly HttpHelper _httpHelper;
        private readonly CloudFormationHelper _cloudFormationHelper;
        private readonly ECSHelper _ecsHelper;
        private readonly App _app;
        private readonly InMemoryInteractiveService _interactiveService;
        private bool _isDisposed;
        private string _stackName;
        private readonly TestAppManager _testAppManager;

        public WebAppWithDockerFileTests()
        {
            _httpHelper = new HttpHelper();

            var cloudFormationClient = new AmazonCloudFormationClient();
            _cloudFormationHelper = new CloudFormationHelper(cloudFormationClient);

            var ecsClient = new AmazonECSClient();
            _ecsHelper = new ECSHelper(ecsClient);

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddCustomServices();
            serviceCollection.AddTestServices();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            _app = serviceProvider.GetService<App>();
            Assert.NotNull(_app);

            _interactiveService = serviceProvider.GetService<InMemoryInteractiveService>();
            Assert.NotNull(_interactiveService);

            _testAppManager = new TestAppManager();
        }

        [Fact]
        public async Task DefaultConfigurations()
        {
            _stackName = $"WebAppWithDockerFile{Guid.NewGuid().ToString().Split('-').Last()}";

            // Arrange input for deploy
            await _interactiveService.StdInWriter.WriteAsync(Environment.NewLine); // Select default recommendation
            await _interactiveService.StdInWriter.WriteAsync(Environment.NewLine); // Select default option settings
            await _interactiveService.StdInWriter.FlushAsync();

            // Deploy
            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppWithDockerFile", "WebAppWithDockerFile.csproj"));
            var deployArgs = new[] { "deploy", "--project-path", projectPath, "--stack-name", _stackName, "--diagnostics" };
            await _app.Run(deployArgs);

            // Verify application is deployed and running
            Assert.Equal(StackStatus.CREATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(_stackName));

            var cluster = await _ecsHelper.GetCluster(_stackName);
            Assert.Equal("ACTIVE", cluster.Status);

            var deployStdDebug = _interactiveService.StdDebugReader.ReadAllLines();

            var tempCdkProject = deployStdDebug.FirstOrDefault(line => line.Trim().Contains("The CDK Project is saved at: "))?
                .Split(": ")[1]
                .Trim();

            Assert.NotNull(tempCdkProject);
            Assert.False(Directory.Exists(tempCdkProject));

            var deployStdOut = _interactiveService.StdOutReader.ReadAllLines();

            var applicationUrl = deployStdOut.First(line => line.Trim().StartsWith("Endpoint:"))
                .Split(" ")[1]
                .Trim();

            // URL could take few more minutes to come live, therefore, we want to wait and keep trying for a specified timeout
            await _httpHelper.WaitUntilSuccessStatusCode(applicationUrl, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5));

            // list
            var listArgs = new[] { "list-deployments" };
            await _app.Run(listArgs);

            // Verify stack exists in list of deployments
            var listDeployStdOut = _interactiveService.StdOutReader.ReadAllLines();
            Assert.Contains(listDeployStdOut, (deployment) => _stackName.Equals(deployment));

            // Arrange input for re-deployment
            await _interactiveService.StdInWriter.WriteAsync(Environment.NewLine); // Select default option settings
            await _interactiveService.StdInWriter.FlushAsync();

            // Perform re-deployment
            deployArgs = new[] { "deploy", "--project-path", projectPath, "--stack-name", _stackName, "--diagnostics" };
            var returnCode  = await _app.Run(deployArgs);
            Assert.Equal(CommandReturnCodes.SUCCESS, returnCode);
            Assert.Equal(StackStatus.UPDATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(_stackName));
            Assert.Equal("ACTIVE", cluster.Status);

            // Arrange input for delete
            await _interactiveService.StdInWriter.WriteAsync("y"); // Confirm delete
            await _interactiveService.StdInWriter.FlushAsync();
            var deleteArgs = new[] { "delete-deployment", _stackName };

            // Delete
            await _app.Run(deleteArgs);

            // Verify application is deleted
            Assert.True(await _cloudFormationHelper.IsStackDeleted(_stackName), $"{_stackName} still exists.");
        }

        [Fact]
        public async Task AppRunnerDeployment()
        {
            _stackName = $"WebAppWithDockerFile{Guid.NewGuid().ToString().Split('-').Last()}";

            // Arrange input for deploy
            await _interactiveService.StdInWriter.WriteAsync("2" + Environment.NewLine); // Select App Runner recommendation
            await _interactiveService.StdInWriter.WriteAsync(Environment.NewLine); // Select default option settings
            await _interactiveService.StdInWriter.FlushAsync();

            // Deploy
            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppWithDockerFile", "WebAppWithDockerFile.csproj"));
            var deployArgs = new[] { "deploy", "--project-path", projectPath, "--stack-name", _stackName };
            await _app.Run(deployArgs);

            // Verify application is deployed and running
            Assert.Equal(StackStatus.CREATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(_stackName));

            var deployStdOut = _interactiveService.StdOutReader.ReadAllLines();

            var applicationUrl = deployStdOut.First(line => line.StartsWith($"{_stackName}.RecipeEndpointURL"))
                .Split("=")[1]
                .Trim();

            Assert.Contains("awsapprunner", applicationUrl);

            // URL could take few more minutes to come live, therefore, we want to wait and keep trying for a specified timeout
            await _httpHelper.WaitUntilSuccessStatusCode(applicationUrl, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5));

            // list
            var listArgs = new[] { "list-deployments" };
            await _app.Run(listArgs);

            // Verify stack exists in list of deployments
            var listDeployStdOut = _interactiveService.StdOutReader.ReadAllLines();
            Assert.Contains(listDeployStdOut, (deployment) => _stackName.Equals(deployment));

            // Arrange input for delete
            await _interactiveService.StdInWriter.WriteAsync("y"); // Confirm delete
            await _interactiveService.StdInWriter.FlushAsync();
            var deleteArgs = new[] { "delete-deployment", _stackName };

            // Delete
            await _app.Run(deleteArgs);

            // Verify application is deleted
            Assert.True(await _cloudFormationHelper.IsStackDeleted(_stackName));
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
            }

            _isDisposed = true;
        }

        ~WebAppWithDockerFileTests()
        {
            Dispose(false);
        }
    }
}
