// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Collections.Generic;
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
        private readonly IServiceCollection _serviceCollection;
        private readonly CloudFormationHelper _cloudFormationHelper;
        private readonly ECSHelper _ecsHelper;
        private bool _isDisposed;
        private string? _stackName;
        private readonly TestAppManager _testAppManager;

        public WebAppWithDockerFileTests()
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
        public async Task ApplySettingsWithoutDeploying()
        {
            _stackName = $"WebAppWithDockerFile{Guid.NewGuid().ToString().Split('-').Last()}";

            InMemoryInteractiveService interactiveService = null!;
            try
            {
                // Deploy
                var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppWithDockerFile", "WebAppWithDockerFile.csproj"));
                var deployArgs = new[] { "deploy", "--project-path", projectPath, "--application-name", _stackName, "--diagnostics" };

                await _serviceCollection.RunDeployToolAsync(deployArgs,
                    provider =>
                    {
                        interactiveService = provider.GetRequiredService<InMemoryInteractiveService>();

                        // Arrange input for deploy
                        interactiveService.StdInWriter.Write(Environment.NewLine); // Select default recommendation
                        interactiveService.StdInWriter.WriteLine("more"); // Select 'more'
                        interactiveService.StdInWriter.WriteLine("14"); // Select 'Environment Architecture'
                        interactiveService.StdInWriter.WriteLine("1"); // Select 'X86_64'
                        interactiveService.StdInWriter.WriteLine("14"); // Select 'Environment Architecture' again for Code Coverage
                        interactiveService.StdInWriter.WriteLine("1"); // Select 'X86_64'
                        interactiveService.StdInWriter.WriteLine("9"); // Select 'Task CPU'
                        interactiveService.StdInWriter.WriteLine("2"); // Select '512 (.5 vCPU)'
                        interactiveService.StdInWriter.Flush();
                    });

                var consoleOutput = interactiveService.StdOutReader.ReadAllLines();

                // Assert 'Environment Architecture' is set to 'X86_64'
                var environmentArchitecture = Assert.IsType<string>(consoleOutput.LastOrDefault(x => x.StartsWith("14. Environment Architecture:")));
                var environmentArchitectureSplit = environmentArchitecture.Split(':').ToList().Select(x => x.Trim()).ToList();
                Assert.Equal(2, environmentArchitectureSplit.Count);
                Assert.Equal("X86_64", environmentArchitectureSplit[1]);

                // Assert 'Task CPU' is set to '512'
                var taskCpu = Assert.IsType<string>(consoleOutput.LastOrDefault(x => x.StartsWith("9 . Task CPU:")));
                var taskCpuSplit = taskCpu.Split(':').ToList().Select(x => x.Trim()).ToList();
                Assert.Equal(2, taskCpuSplit.Count);
                Assert.Equal("512", taskCpuSplit[1]);
            }
            finally
            {
                interactiveService?.ReadStdOutStartToEnd();
            }
        }

        [Fact]
        public async Task DefaultConfigurations()
        {
            _stackName = $"WebAppWithDockerFile{Guid.NewGuid().ToString().Split('-').Last()}";
            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppWithDockerFile", "WebAppWithDockerFile.csproj"));

            Cluster cluster;
            InMemoryInteractiveService interactiveService = null!;
            try
            {
                // Deploy
                var deployArgs = new[] { "deploy", "--project-path", projectPath, "--application-name", _stackName, "--diagnostics" };
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

                cluster = await _ecsHelper.GetCluster(_stackName);
                Assert.Equal("ACTIVE", cluster.Status);

                var deployStdOut = interactiveService.StdOutReader.ReadAllLines();

                var tempCdkProjectLine = deployStdOut.First(line => line.StartsWith("Saving AWS CDK deployment project to: "));
                var tempCdkProject = tempCdkProjectLine.Split(": ")[1].Trim();
                Assert.False(Directory.Exists(tempCdkProject), $"{tempCdkProject} must not exist.");

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
                // Perform re-deployment
                var deployArgs = new[] { "deploy", "--project-path", projectPath, "--application-name", _stackName, "--diagnostics" };
                Assert.Equal(CommandReturnCodes.SUCCESS, await _serviceCollection.RunDeployToolAsync(deployArgs,
                    provider =>
                    {
                        interactiveService = provider.GetRequiredService<InMemoryInteractiveService>();

                        // Arrange input for deploy
                        interactiveService.StdInWriter.Write(Environment.NewLine); // Select default option settings
                        interactiveService.StdInWriter.Flush();
                    }));
                Assert.Equal(StackStatus.UPDATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(_stackName));
                Assert.Equal("ACTIVE", cluster.Status);
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

        [Fact]
        public async Task CustomContainerPortConfigurations()
        {
            var stackNamePlaceholder = "{StackName}";
            _stackName = $"WebAppNoDockerFile{Guid.NewGuid().ToString().Split('-').Last()}";
            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppNoDockerFile", "WebAppNoDockerFile.csproj"));

            InMemoryInteractiveService interactiveService = null!;
            try
            {
                // Deploy
                var configFilePath = Path.Combine(Directory.GetParent(projectPath)!.FullName, "ECSFargateCustomPortConfigFile.json");
                ConfigFileHelper.ApplyReplacementTokens(new Dictionary<string, string> { { stackNamePlaceholder, _stackName } }, configFilePath);

                // Deploy
                var deployArgs = new[] { "deploy", "--project-path", projectPath, "--application-name", _stackName, "--diagnostics", "--apply", configFilePath, "--silent" };
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

                var deployStdOut = interactiveService.StdOutReader.ReadAllLines();

                var tempCdkProjectLine = deployStdOut.First(line => line.StartsWith("Saving AWS CDK deployment project to: "));
                var tempCdkProject = tempCdkProjectLine.Split(": ")[1].Trim();
                Assert.False(Directory.Exists(tempCdkProject), $"{tempCdkProject} must not exist.");

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
        }

        [Fact]
        public async Task AppRunnerDeployment()
        {
            var stackNamePlaceholder = "{StackName}";
            _stackName = $"WebAppWithDockerFile{Guid.NewGuid().ToString().Split('-').Last()}";
            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppWithDockerFile", "WebAppWithDockerFile.csproj"));
            var configFilePath = Path.Combine(Directory.GetParent(projectPath)!.FullName, "AppRunnerConfigFile.json");
            ConfigFileHelper.ApplyReplacementTokens(new Dictionary<string, string> { { stackNamePlaceholder, _stackName } }, configFilePath);

            InMemoryInteractiveService interactiveService = null!;
            try
            {
                // Deploy
                var deployArgs = new[] { "deploy", "--project-path", projectPath, "--application-name", _stackName, "--diagnostics", "--apply", configFilePath, "--silent" };
                Assert.Equal(CommandReturnCodes.SUCCESS, await _serviceCollection.RunDeployToolAsync(deployArgs,
                    provider =>
                    {
                        interactiveService = provider.GetRequiredService<InMemoryInteractiveService>();
                    }));

                // Verify application is deployed and running
                Assert.Equal(StackStatus.CREATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(_stackName));

                var deployStdOut = interactiveService.StdOutReader.ReadAllLines();

                var applicationUrl = deployStdOut.First(line => line.StartsWith($"{_stackName}.RecipeEndpointURL"))
                    .Split("=")[1]
                    .Trim();

                Assert.Contains("awsapprunner", applicationUrl);

                // URL could take few more minutes to come live, therefore, we want to wait and keep trying for a specified timeout
                var httpHelper = new HttpHelper(interactiveService);
                await httpHelper.WaitUntilSuccessStatusCode(applicationUrl, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5));

                // Ensure environemnt variables specified in AppRunnerConfigFile.json are set for the service.
                var checkEnvironmentVariableUrl = applicationUrl + "envvar/TEST_Key1";
                using var httpClient = new HttpClient();
                var envVarValue = await httpClient.GetStringAsync(checkEnvironmentVariableUrl);
                Assert.Equal("Value1", envVarValue);
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

        [Fact]
        public async Task FargateArmDeployment()
        {
            _stackName = $"FargateArmDeployment{Guid.NewGuid().ToString().Split('-').Last()}";

            InMemoryInteractiveService interactiveService = null!;
            try
            {
                // Deploy
                var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppArmWithDocker", "WebAppArmWithDocker.csproj"));
                var deployArgs = new[] { "deploy", "--project-path", projectPath, "--application-name", _stackName, "--diagnostics" };
                Assert.Equal(CommandReturnCodes.SUCCESS, await _serviceCollection.RunDeployToolAsync(deployArgs,
                    provider =>
                    {
                        interactiveService = provider.GetRequiredService<InMemoryInteractiveService>();

                        // Arrange input for deploy
                        interactiveService.StdInWriter.Write(Environment.NewLine); // Select default recommendation
                        interactiveService.StdInWriter.WriteLine("8"); // Select "Environment Architecture"
                        interactiveService.StdInWriter.WriteLine("2"); // Select "Arm64"
                        interactiveService.StdInWriter.Write(Environment.NewLine); // Confirm selection and deploy
                        interactiveService.StdInWriter.Flush();
                    }));

                // Verify application is deployed and running
                Assert.Equal(StackStatus.CREATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(_stackName));

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

        ~WebAppWithDockerFileTests()
        {
            Dispose(false);
        }
    }
}
