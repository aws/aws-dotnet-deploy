// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using Amazon.CloudFormation;
using Amazon.ECS;
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.CLI.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Helpers;
using AWS.Deploy.CLI.IntegrationTests.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace AWS.Deploy.CLI.IntegrationTests.SaveCdkDeploymentProject
{
    public class RedeploymentTests : IDisposable
    {
        private readonly HttpHelper _httpHelper;
        private readonly CloudFormationHelper _cloudFormationHelper;
        private readonly ECSHelper _ecsHelper;
        private readonly App _app;
        private readonly InMemoryInteractiveService _interactiveService;
        private bool _isDisposed;
        private string _stackName;

        public RedeploymentTests()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddCustomServices();
            serviceCollection.AddTestServices();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            _app = serviceProvider.GetService<App>();
            Assert.NotNull(_app);

            _interactiveService = serviceProvider.GetService<InMemoryInteractiveService>();
            Assert.NotNull(_interactiveService);

            _httpHelper = new HttpHelper(_interactiveService);

            var cloudFormationClient = new AmazonCloudFormationClient();
            _cloudFormationHelper = new CloudFormationHelper(cloudFormationClient);

            var ecsClient = new AmazonECSClient();
            _ecsHelper = new ECSHelper(ecsClient);
        }

        [Fact]
        public async Task AttemptWorkFlow()
        {
            _stackName = $"WebAppWithDockerFile{Guid.NewGuid().ToString().Split('-').Last()}";
            var tempDirectoryPath = new TestAppManager().GetProjectPath(string.Empty);
            var projectPath = Path.Combine(tempDirectoryPath, "testapps", "WebAppWithDockerFile");
            var compatibleDeploymentProjectPath = Path.Combine(tempDirectoryPath, "DeploymentProjects", "CompatibleCdkApp");
            var incompatibleDeploymentProjectPath = Path.Combine(tempDirectoryPath, "DeploymentProjects", "IncompatibleCdkApp");

            // perform inital deployment using ECS Fargate recipe
            await PerformInitialDeployment(projectPath);

            // Create a compatible CDK deployment project using the ECS recipe
            await Utilities.CreateCDKDeploymentProjectWithRecipeName(projectPath, "Custom ECS Fargate Recipe", "1", compatibleDeploymentProjectPath, underSourceControl: false);

            // Create an incompatible CDK deployment project using the App runner recipe
            await Utilities.CreateCDKDeploymentProjectWithRecipeName(projectPath, "Custom App Runner Recipe", "2", incompatibleDeploymentProjectPath, underSourceControl: false);

            // attempt re-deployment using incompatible CDK project
            var deployArgs = new[] { "deploy", "--project-path", projectPath, "--deployment-project", incompatibleDeploymentProjectPath, "--application-name", _stackName, "--diagnostics", "--direct-deploy" };
            var returnCode = await _app.Run(deployArgs);
            Assert.Equal(CommandReturnCodes.USER_ERROR, returnCode);

            // attempt re-deployment using compatible CDK project
            await _interactiveService.StdInWriter.WriteAsync(Environment.NewLine); // Select default option settings
            await _interactiveService.StdInWriter.FlushAsync();
            deployArgs = new[] { "deploy", "--project-path", projectPath, "--deployment-project", compatibleDeploymentProjectPath, "--application-name", _stackName, "--diagnostics", "--direct-deploy" };
            returnCode = await _app.Run(deployArgs);
            Assert.Equal(CommandReturnCodes.SUCCESS, returnCode);
            Assert.Equal(StackStatus.UPDATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(_stackName));
            var cluster = await _ecsHelper.GetCluster(_stackName);
            Assert.Equal(TaskDefinitionStatus.ACTIVE, cluster.Status);

            // Delete stack
            await DeleteStack();
        }

        private async Task PerformInitialDeployment(string projectPath)
        {
            // Arrange input for deploy
            await _interactiveService.StdInWriter.WriteLineAsync("1"); // Select ECS Fargate recommendation
            await _interactiveService.StdInWriter.WriteAsync(Environment.NewLine); // Select default option settings
            await _interactiveService.StdInWriter.FlushAsync();

            // Deploy
            var deployArgs = new[] { "deploy", "--project-path", projectPath, "--application-name", _stackName, "--diagnostics", "--direct-deploy" };
            var returnCode = await _app.Run(deployArgs);
            Assert.Equal(CommandReturnCodes.SUCCESS, returnCode);

            // Verify application is deployed and running
            Assert.Equal(StackStatus.CREATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(_stackName));

            var cluster = await _ecsHelper.GetCluster(_stackName);
            Assert.Equal(TaskDefinitionStatus.ACTIVE, cluster.Status);

            var deployStdOut = _interactiveService.StdOutReader.ReadAllLines();

            var applicationUrl = deployStdOut.First(line => line.Trim().StartsWith("Endpoint:"))
                .Split(" ")[1]
                .Trim();

            // URL could take few more minutes to come live, therefore, we want to wait and keep trying for a specified timeout
            await _httpHelper.WaitUntilSuccessStatusCode(applicationUrl, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5));

            // list
            var listArgs = new[] { "list-deployments", "--diagnostics" };
            returnCode = await _app.Run(listArgs);
            Assert.Equal(CommandReturnCodes.SUCCESS, returnCode);

            // Verify stack exists in list of deployments
            var listDeployStdOut = _interactiveService.StdOutReader.ReadAllLines().Select(x => x.Split()[0]).ToList();
            Assert.Contains(listDeployStdOut, (deployment) => _stackName.Equals(deployment));
        }

        private async Task DeleteStack()
        {
            // Arrange input for delete
            await _interactiveService.StdInWriter.WriteAsync("y"); // Confirm delete
            await _interactiveService.StdInWriter.FlushAsync();
            var deleteArgs = new[] { "delete-deployment", _stackName, "--diagnostics" };

            // Delete
            var returnCode = await _app.Run(deleteArgs);
            Assert.Equal(CommandReturnCodes.SUCCESS, returnCode);

            // Verify application is deleted
            Assert.True(await _cloudFormationHelper.IsStackDeleted(_stackName), $"{_stackName} still exists.");
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

        ~RedeploymentTests()
        {
            Dispose(false);
        }
    }
}
