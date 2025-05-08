// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.CLI.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Helpers;
using AWS.Deploy.CLI.IntegrationTests.Services;
using AWS.Deploy.Orchestration.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
using Environment = System.Environment;

namespace AWS.Deploy.CLI.IntegrationTests
{
    public class WebAppNoDockerFileTests : IDisposable
    {
        private readonly IServiceCollection _serviceCollection;
        private readonly CloudFormationHelper _cloudFormationHelper;
        private bool _isDisposed;
        private string? _stackName;
        private readonly TestAppManager _testAppManager;
        private readonly string _customWorkspace;

        public WebAppNoDockerFileTests()
        {
            _serviceCollection = new ServiceCollection();

            _serviceCollection.AddCustomServices();
            _serviceCollection.AddTestServices();

            foreach (var item in _serviceCollection)
            {
                if (item.ServiceType == typeof(IEnvironmentVariableManager))
                {
                    _serviceCollection.Remove(item);
                    break;
                }
            }

            _serviceCollection.TryAdd(new ServiceDescriptor(typeof(IEnvironmentVariableManager), typeof(TestEnvironmentVariableManager), ServiceLifetime.Singleton));

            _customWorkspace = Path.Combine(Path.GetTempPath(), $"deploy-tool-workspace{Guid.NewGuid().ToString().Split('-').Last()}");
            Helpers.Utilities.OverrideDefaultWorkspace(_serviceCollection.BuildServiceProvider(), _customWorkspace);

            var cloudFormationClient = new AmazonCloudFormationClient();
            _cloudFormationHelper = new CloudFormationHelper(cloudFormationClient);

            _testAppManager = new TestAppManager();
        }

        [Theory]
        [InlineData("ElasticBeanStalkConfigFile-Linux.json", true)]
        [InlineData("ElasticBeanStalkConfigFile-Linux-SelfContained.json", true)]
        [InlineData("ElasticBeanStalkConfigFile-Windows.json", false)]
        [InlineData("ElasticBeanStalkConfigFile-Windows-SelfContained.json", false)]
        public async Task EBDefaultConfigurations(string configFile, bool linux)
        {
            _stackName = $"BeanstalkTest-{Guid.NewGuid().ToString().Split('-').Last()}";

            InMemoryInteractiveService interactiveService = null!;
            try
            {
                // Deploy
                var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppNoDockerFile", "WebAppNoDockerFile.csproj"));
                var deployArgs = new[] { "deploy", "--project-path", projectPath, "--application-name", _stackName, "--diagnostics", "--silent", "--apply", configFile };
                Assert.Equal(CommandReturnCodes.SUCCESS, await _serviceCollection.RunDeployToolAsync(deployArgs,
                    provider =>
                    {
                        interactiveService = provider.GetRequiredService<InMemoryInteractiveService>();
                    }));

                // Verify application is deployed and running
                Assert.Equal(StackStatus.CREATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(_stackName));

                var deployStdOut = interactiveService.StdOutReader.ReadAllLines();

                var tempCdkProjectLine = deployStdOut.First(line => line.StartsWith("Saving AWS CDK deployment project to: "));
                var tempCdkProject = tempCdkProjectLine.Split(": ")[1].Trim();
                Assert.False(Directory.Exists(tempCdkProject), $"{tempCdkProject} must not exist.");

                // Example:     Endpoint: http://52.36.216.238/
                var endpointLine = deployStdOut.First(line => line.Trim().StartsWith($"Endpoint"));
                var applicationUrl = endpointLine.Substring(endpointLine.IndexOf(":") + 1).Trim();
                Assert.True(Uri.IsWellFormedUriString(applicationUrl, UriKind.Absolute));

                if(!linux)
                {
                    // "extra-path" is the IISAppPath set in the config file.
                    applicationUrl = new Uri(new Uri(applicationUrl), "extra-path").AbsoluteUri;
                }

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
                var listStdOut = interactiveService.StdOutReader.ReadAllLines().Select(x => x.Split()[0]).ToList();
                Assert.Contains(listStdOut, (deployment) => _stackName.Equals(deployment));
            }
            finally
            {
                interactiveService?.ReadStdOutStartToEnd();
            }

            // Delete
            // Use --silent flag to delete without user prompts
            var deleteArgs = new[] { "delete-deployment", _stackName, "--diagnostics", "--silent" };
            Assert.Equal(CommandReturnCodes.SUCCESS, await _serviceCollection.RunDeployToolAsync(deleteArgs)); ;

            // Verify application is deleted
            Assert.True(await _cloudFormationHelper.IsStackDeleted(_stackName), $"{_stackName} still exists.");
        }

        [Fact]
        public async Task BeanstalkArmDeployment()
        {
            _stackName = $"BeanstalkArm{Guid.NewGuid().ToString().Split('-').Last()}";
            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppArmDeployment", "WebAppArmDeployment.csproj"));

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
                        interactiveService.StdInWriter.WriteLine("8"); // Select "Environment Architecture"
                        interactiveService.StdInWriter.WriteLine("2"); // Select "Arm64"
                        interactiveService.StdInWriter.Write(Environment.NewLine); // Confirm selection and deploy
                        interactiveService.StdInWriter.Flush();
                    }));

                // Verify application is deployed and running
                Assert.Equal(StackStatus.CREATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(_stackName));

                var deployStdOut = interactiveService.StdOutReader.ReadAllLines();

                var tempCdkProjectLine = deployStdOut.First(line => line.StartsWith("Saving AWS CDK deployment project to: "));
                var tempCdkProject = tempCdkProjectLine.Split(": ")[1].Trim();
                Assert.False(Directory.Exists(tempCdkProject), $"{tempCdkProject} must not exist.");

                // Example:     Endpoint: http://52.36.216.238/
                var endpointLine = deployStdOut.First(line => line.Trim().StartsWith($"Endpoint"));
                var applicationUrl = endpointLine.Substring(endpointLine.IndexOf(":", StringComparison.Ordinal) + 1).Trim();
                Assert.True(Uri.IsWellFormedUriString(applicationUrl, UriKind.Absolute));

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
                var deployArgs = new[] { "deploy", "--project-path", projectPath, "--application-name", _stackName, "--diagnostics" };
                Assert.Equal(CommandReturnCodes.SUCCESS, await _serviceCollection.RunDeployToolAsync(deployArgs,
                    provider =>
                    {
                        interactiveService = provider.GetRequiredService<InMemoryInteractiveService>();

                        // Try switching from ARM to X86_64 on redeployment
                        interactiveService.StdInWriter.WriteLine("3"); // Select "Environment Architecture"
                        interactiveService.StdInWriter.WriteLine("1"); // Select "X86_64"
                        interactiveService.StdInWriter.Write(Environment.NewLine); // Confirm selection and deploy
                        interactiveService.StdInWriter.WriteLine("more"); // Select "Environment Architecture"
                        interactiveService.StdInWriter.WriteLine("1"); // Select "EC2 Instance Type"
                        interactiveService.StdInWriter.WriteLine("y"); // Select "Free tier"
                        interactiveService.StdInWriter.WriteLine("1"); // Select "x86_64"
                        interactiveService.StdInWriter.WriteLine("1"); // Select "CPU Cores"
                        interactiveService.StdInWriter.WriteLine("1"); // Select "Instance Memory"
                        interactiveService.StdInWriter.WriteLine("1"); // Select "Instance Type"
                        interactiveService.StdInWriter.Write(Environment.NewLine); // Confirm selection and deploy
                        interactiveService.StdInWriter.Flush();
                    }));
                Assert.Equal(StackStatus.UPDATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(_stackName));

                var deployStdOut = interactiveService.StdOutReader.ReadAllLines();
                Assert.Contains(deployStdOut, x => x.Contains("Please select an Instance Type that supports the currently selected Environment Architecture."));
            }
            finally
            {
                interactiveService?.ReadStdOutStartToEnd();
            }

            // Delete
            // Use --silent flag to delete without user prompts
            var deleteArgs = new[] { "delete-deployment", _stackName, "--diagnostics", "--silent" };
            Assert.Equal(CommandReturnCodes.SUCCESS, await _serviceCollection.RunDeployToolAsync(deleteArgs)); ;

            // Verify application is deleted
            Assert.True(await _cloudFormationHelper.IsStackDeleted(_stackName), $"{_stackName} still exists.");
        }

        [Fact]
        public async Task DeployRetiredDotnetVersion()
        {
            _stackName = $"RetiredDotnetBeanstalk{Guid.NewGuid().ToString().Split('-').Last()}";
            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebApiNET6", "WebApiNET6.csproj"));

            InMemoryInteractiveService interactiveService = null!;
            try
            {
                var deployArgs = new[] { "deploy", "--project-path", projectPath, "--application-name", _stackName, "--diagnostics", "--silent", "--apply", "ElasticBeanStalkConfigFile.json" };
                Assert.Equal(CommandReturnCodes.SUCCESS, await _serviceCollection.RunDeployToolAsync(deployArgs,
                    provider =>
                    {
                        interactiveService = provider.GetRequiredService<InMemoryInteractiveService>();
                    }));

                // Verify application is deployed and running
                Assert.Equal(StackStatus.CREATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(_stackName));

                var deployStdOut = interactiveService.StdOutReader.ReadAllLines();

                var tempCdkProjectLine = deployStdOut.First(line => line.StartsWith("Saving AWS CDK deployment project to: "));
                var tempCdkProject = tempCdkProjectLine.Split(": ")[1].Trim();
                Assert.False(Directory.Exists(tempCdkProject), $"{tempCdkProject} must not exist.");

                // Example:     Endpoint: http://52.36.216.238/
                var endpointLine = deployStdOut.First(line => line.Trim().StartsWith($"Endpoint"));
                var applicationUrl = endpointLine.Substring(endpointLine.IndexOf(":", StringComparison.Ordinal) + 1).Trim();
                Assert.True(Uri.IsWellFormedUriString(applicationUrl, UriKind.Absolute));

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
                var listStdOut = interactiveService.StdOutReader.ReadAllLines().Select(x => x.Split()[0]).ToList();
                Assert.Contains(listStdOut, (deployment) => _stackName.Equals(deployment));
            }
            finally
            {
                interactiveService?.ReadStdOutStartToEnd();
            }

            // Delete
            // Use --silent flag to delete without user prompts
            var deleteArgs = new[] { "delete-deployment", _stackName, "--diagnostics", "--silent" };
            Assert.Equal(CommandReturnCodes.SUCCESS, await _serviceCollection.RunDeployToolAsync(deleteArgs)); ;

            // Verify application is deleted
            Assert.True(await _cloudFormationHelper.IsStackDeleted(_stackName), $"{_stackName} still exists.");
        }


        [Fact]
        public async Task DeployDotnet10Version()
        {
            _stackName = $"Dotnet10Beanstalk{Guid.NewGuid().ToString().Split('-').Last()}";
            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebApiNET10", "WebApiNET10.csproj"));

            InMemoryInteractiveService interactiveService = null!;
            try
            {
                var deployArgs = new[] { "deploy", "--project-path", projectPath, "--application-name", _stackName, "--diagnostics", "--silent", "--apply", "ElasticBeanStalkConfigFile.json" };
                Assert.Equal(CommandReturnCodes.SUCCESS, await _serviceCollection.RunDeployToolAsync(deployArgs,
                    provider =>
                    {
                        interactiveService = provider.GetRequiredService<InMemoryInteractiveService>();
                    }));

                // Verify application is deployed and running
                Assert.Equal(StackStatus.CREATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(_stackName));

                var deployStdOut = interactiveService.StdOutReader.ReadAllLines();

                var tempCdkProjectLine = deployStdOut.First(line => line.StartsWith("Saving AWS CDK deployment project to: "));
                var tempCdkProject = tempCdkProjectLine.Split(": ")[1].Trim();
                Assert.False(Directory.Exists(tempCdkProject), $"{tempCdkProject} must not exist.");

                // Example:     Endpoint: http://52.36.216.238/
                var endpointLine = deployStdOut.First(line => line.Trim().StartsWith($"Endpoint"));
                var applicationUrl = endpointLine.Substring(endpointLine.IndexOf(":", StringComparison.Ordinal) + 1).Trim();
                Assert.True(Uri.IsWellFormedUriString(applicationUrl, UriKind.Absolute));

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
                var listStdOut = interactiveService.StdOutReader.ReadAllLines().Select(x => x.Split()[0]).ToList();
                Assert.Contains(listStdOut, (deployment) => _stackName.Equals(deployment));
            }
            finally
            {
                interactiveService?.ReadStdOutStartToEnd();
            }

            // Delete
            // Use --silent flag to delete without user prompts
            var deleteArgs = new[] { "delete-deployment", _stackName, "--diagnostics", "--silent" };
            Assert.Equal(CommandReturnCodes.SUCCESS, await _serviceCollection.RunDeployToolAsync(deleteArgs)); ;

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

        ~WebAppNoDockerFileTests()
        {
            Dispose(false);
        }
    }
}
