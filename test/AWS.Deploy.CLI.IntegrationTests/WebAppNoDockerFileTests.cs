// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
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
        private readonly HttpHelper _httpHelper;
        private readonly CloudFormationHelper _cloudFormationHelper;
        private readonly App _app;
        private readonly InMemoryInteractiveService _interactiveService;
        private bool _isDisposed;
        private string _stackName;
        private readonly TestAppManager _testAppManager;
        private readonly string _customWorkspace;

        public WebAppNoDockerFileTests()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddCustomServices();
            serviceCollection.AddTestServices();

            foreach (var item in serviceCollection)
            {
                if (item.ServiceType == typeof(IEnvironmentVariableManager))
                {
                    serviceCollection.Remove(item);
                    break;
                }
            }

            serviceCollection.TryAdd(new ServiceDescriptor(typeof(IEnvironmentVariableManager), typeof(TestEnvironmentVariableManager), ServiceLifetime.Singleton));

            var serviceProvider = serviceCollection.BuildServiceProvider();

            _customWorkspace = Path.Combine(Path.GetTempPath(), $"deploy-tool-workspace{Guid.NewGuid().ToString().Split('-').Last()}");
            Helpers.Utilities.OverrideDefaultWorkspace(serviceProvider, _customWorkspace);

            _app = serviceProvider.GetService<App>();
            Assert.NotNull(_app);

            _interactiveService = serviceProvider.GetService<InMemoryInteractiveService>();
            Assert.NotNull(_interactiveService);

            _httpHelper = new HttpHelper(_interactiveService);

            var cloudFormationClient = new AmazonCloudFormationClient();
            _cloudFormationHelper = new CloudFormationHelper(cloudFormationClient);

            _testAppManager = new TestAppManager();
        }

        [Fact]
        public async Task DefaultConfigurations()
        {
            _stackName = $"WebAppNoDockerFile{Guid.NewGuid().ToString().Split('-').Last()}";

            // Arrange input for deploy
            await _interactiveService.StdInWriter.WriteAsync(Environment.NewLine); // Select default recommendation
            await _interactiveService.StdInWriter.WriteAsync(Environment.NewLine); // Select default option settings
            await _interactiveService.StdInWriter.FlushAsync();

            // Deploy
            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppNoDockerFile", "WebAppNoDockerFile.csproj"));
            var deployArgs = new[] { "deploy", "--project-path", projectPath, "--application-name", _stackName, "--diagnostics" };
            Assert.Equal(CommandReturnCodes.SUCCESS, await _app.Run(deployArgs));

            // Verify application is deployed and running
            Assert.Equal(StackStatus.CREATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(_stackName));

            var deployStdOut = _interactiveService.StdOutReader.ReadAllLines();

            var tempCdkProjectLine = deployStdOut.First(line => line.StartsWith("Saving AWS CDK deployment project to: "));
            var tempCdkProject = tempCdkProjectLine.Split(": ")[1].Trim();
            Assert.False(Directory.Exists(tempCdkProject), $"{tempCdkProject} must not exist.");

            // Example:     Endpoint: http://52.36.216.238/
            var endpointLine = deployStdOut.First(line => line.Trim().StartsWith($"Endpoint"));
            var applicationUrl = endpointLine.Substring(endpointLine.IndexOf(":") + 1).Trim();
            Assert.True(Uri.IsWellFormedUriString(applicationUrl, UriKind.Absolute));

            // URL could take few more minutes to come live, therefore, we want to wait and keep trying for a specified timeout
            await _httpHelper.WaitUntilSuccessStatusCode(applicationUrl, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5));

            // Check the overridden workspace
            Assert.True(File.Exists(Path.Combine(_customWorkspace, "CDKBootstrapTemplate.yaml")));
            Assert.True(Directory.Exists(Path.Combine(_customWorkspace, "temp")));
            Assert.True(Directory.Exists(Path.Combine(_customWorkspace, "Projects")));

            // list
            var listArgs = new[] { "list-deployments", "--diagnostics" };
            Assert.Equal(CommandReturnCodes.SUCCESS, await _app.Run(listArgs));;

            // Verify stack exists in list of deployments
            var listStdOut = _interactiveService.StdOutReader.ReadAllLines().Select(x => x.Split()[0]).ToList();
            Assert.Contains(listStdOut, (deployment) => _stackName.Equals(deployment));

            // Arrange input for delete
            // Use --silent flag to delete without user prompts
            var deleteArgs = new[] { "delete-deployment", _stackName, "--diagnostics", "--silent" };

            // Delete
            Assert.Equal(CommandReturnCodes.SUCCESS, await _app.Run(deleteArgs));;

            // Verify application is deleted
            Assert.True(await _cloudFormationHelper.IsStackDeleted(_stackName), $"{_stackName} still exists.");
        }

        [Fact]
        public async Task WindowsEBDefaultConfigurations()
        {
            _stackName = $"WinTest-{Guid.NewGuid().ToString().Split('-').Last()}";

            // Deploy
            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppNoDockerFile", "WebAppNoDockerFile.csproj"));
            var deployArgs = new[] { "deploy", "--project-path", projectPath, "--application-name", _stackName, "--diagnostics", "--silent", "--apply",  "ElasticBeanStalkConfigFile-Windows.json" };
            Assert.Equal(CommandReturnCodes.SUCCESS, await _app.Run(deployArgs));

            // Verify application is deployed and running
            Assert.Equal(StackStatus.CREATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(_stackName));

            var deployStdOut = _interactiveService.StdOutReader.ReadAllLines();

            var tempCdkProjectLine = deployStdOut.First(line => line.StartsWith("Saving AWS CDK deployment project to: "));
            var tempCdkProject = tempCdkProjectLine.Split(": ")[1].Trim();
            Assert.False(Directory.Exists(tempCdkProject), $"{tempCdkProject} must not exist.");

            // Example:     Endpoint: http://52.36.216.238/
            var endpointLine = deployStdOut.First(line => line.Trim().StartsWith($"Endpoint"));
            var applicationUrl = endpointLine.Substring(endpointLine.IndexOf(":") + 1).Trim();
            Assert.True(Uri.IsWellFormedUriString(applicationUrl, UriKind.Absolute));

            // "extra-path" is the IISAppPath set in the config file.
            applicationUrl = new Uri(new Uri(applicationUrl), "extra-path").AbsoluteUri;

            // URL could take few more minutes to come live, therefore, we want to wait and keep trying for a specified timeout
            await _httpHelper.WaitUntilSuccessStatusCode(applicationUrl, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5));

            // list
            var listArgs = new[] { "list-deployments", "--diagnostics" };
            Assert.Equal(CommandReturnCodes.SUCCESS, await _app.Run(listArgs)); ;

            // Verify stack exists in list of deployments
            var listStdOut = _interactiveService.StdOutReader.ReadAllLines().Select(x => x.Split()[0]).ToList();
            Assert.Contains(listStdOut, (deployment) => _stackName.Equals(deployment));

            // Arrange input for delete
            // Use --silent flag to delete without user prompts
            var deleteArgs = new[] { "delete-deployment", _stackName, "--diagnostics", "--silent" };

            // Delete
            Assert.Equal(CommandReturnCodes.SUCCESS, await _app.Run(deleteArgs)); ;

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

        ~WebAppNoDockerFileTests()
        {
            Dispose(false);
        }
    }
}
