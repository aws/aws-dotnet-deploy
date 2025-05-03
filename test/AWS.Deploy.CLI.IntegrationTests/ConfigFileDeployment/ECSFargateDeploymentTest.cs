// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
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

namespace AWS.Deploy.CLI.IntegrationTests.ConfigFileDeployment
{
    public class ECSFargateDeploymentTest : IDisposable
    {
        private readonly IServiceCollection _serviceCollection;
        private readonly CloudFormationHelper _cloudFormationHelper;
        private readonly ECSHelper _ecsHelper;
        private bool _isDisposed;
        private string _stackName;
        private string _clusterName;
        private readonly TestAppManager _testAppManager;

        public ECSFargateDeploymentTest()
        {
            var ecsClient = new AmazonECSClient();
            _ecsHelper = new ECSHelper(ecsClient);

            _serviceCollection = new ServiceCollection();

            _serviceCollection.AddCustomServices();
            _serviceCollection.AddTestServices();

            var cloudFormationClient = new AmazonCloudFormationClient();
            _cloudFormationHelper = new CloudFormationHelper(cloudFormationClient);

            _testAppManager = new TestAppManager();
        }

        [Fact]
        public async Task PerformDeployment()
        {
            InMemoryInteractiveService interactiveService = null;
            try
            {
                var stackNamePlaceholder = "{StackName}";
                _stackName = $"WebAppWithDockerFile{Guid.NewGuid().ToString().Split('-').Last()}";
                _clusterName = $"{_stackName}-cluster";
                var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppWithDockerFile", "WebAppWithDockerFile.csproj"));
                var configFilePath = Path.Combine(Directory.GetParent(projectPath).FullName, "ECSFargateConfigFile.json");
                ConfigFileHelper.ApplyReplacementTokens(new Dictionary<string, string> { { stackNamePlaceholder, _stackName } }, configFilePath);

                // Deploy
                var deployArgs = new[] { "deploy", "--project-path", projectPath, "--apply", configFilePath, "--silent", "--diagnostics" };
                Assert.Equal(CommandReturnCodes.SUCCESS, await _serviceCollection.RunDeployToolAsync(deployArgs,
                    provider =>
                    {
                        interactiveService = provider.GetRequiredService<InMemoryInteractiveService>();
                    }));

                // Verify application is deployed and running
                Assert.Equal(StackStatus.CREATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(_stackName));

                var cluster = await _ecsHelper.GetCluster(_clusterName);
                Assert.Equal("ACTIVE", cluster.Status);
                Assert.Equal(cluster.ClusterName, _clusterName);

                var deployStdOut = interactiveService.StdOutReader.ReadAllLines();

                var applicationUrl = deployStdOut.First(line => line.Trim().StartsWith("Endpoint:"))
                    .Split(" ")[1]
                    .Trim();

                // URL could take few more minutes to come live, therefore, we want to wait and keep trying for a specified timeout
                var httpHelper = new HttpHelper(interactiveService);
                await httpHelper.WaitUntilSuccessStatusCode(applicationUrl, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5));
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
                var listDeployStdOut = interactiveService.StdOutReader.ReadAllLines().Select(x => x.Split()[0]).ToList();
                Assert.Contains(listDeployStdOut, (deployment) => _stackName.Equals(deployment));
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

                        interactiveService.StdInWriter.Write("y"); // Confirm delete
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

        ~ECSFargateDeploymentTest()
        {
            Dispose(false);
        }
    }
}
