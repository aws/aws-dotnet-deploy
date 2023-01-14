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
using AWS.Deploy.Orchestration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace AWS.Deploy.CLI.IntegrationTests.ConfigFileDeployment
{
    [TestFixture]
    public class ElasticBeanStalkDeploymentTest
    {
        private HttpHelper _httpHelper;
        private CloudFormationHelper _cloudFormationHelper;
        private App _app;
        private InMemoryInteractiveService _interactiveService;
        private string _stackName;
        private TestAppManager _testAppManager;
        private IServiceProvider _serviceProvider;

        [SetUp]
        public void Initialize()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddCustomServices(ServiceLifetime.Scoped);
            serviceCollection.AddTestServices();

            _serviceProvider = serviceCollection.BuildServiceProvider();

            _app = _serviceProvider.GetService<App>();
            Assert.NotNull(_app);

            _interactiveService = _serviceProvider.GetService<InMemoryInteractiveService>();
            Assert.NotNull(_interactiveService);

            _httpHelper = new HttpHelper(_interactiveService);

            var cloudFormationClient = new AmazonCloudFormationClient();
            _cloudFormationHelper = new CloudFormationHelper(cloudFormationClient);

            _testAppManager = new TestAppManager();
        }

        [Test]
        public async Task PerformDeployment()
        {
            // Create the config file
            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppNoDockerFile", "WebAppNoDockerFile.csproj"));
            var stackNamePlaceholder = "{StackName}";
            var configFilePath = Path.Combine(Path.GetTempPath(), $"DeploymentSettings-{Guid.NewGuid().ToString().Split('-').Last()}.json");
            var expectedConfigFilePath = Path.Combine(Directory.GetParent(projectPath).FullName, "ElasticBeanStalkConfigFile.json");
            var optionSettings = new Dictionary<string, object>
            {
                {"BeanstalkApplication.CreateNew", true },
                {"BeanstalkApplication.ApplicationName", $"{stackNamePlaceholder}-app" },
                {"BeanstalkEnvironment.EnvironmentName", $"{stackNamePlaceholder}-dev" },
                {"EnvironmentType", "LoadBalanced" },
                {"LoadBalancerType", "application" },
                {"ApplicationIAMRole.CreateNew", true },
                {"XRayTracingSupportEnabled", true }
            };
            await ConfigFileHelper.CreateConfigFile(_serviceProvider, stackNamePlaceholder, "AspNetAppElasticBeanstalkLinux", optionSettings, projectPath, configFilePath, SaveSettingsType.Modified);
            Assert.True(await ConfigFileHelper.VerifyConfigFileContents(expectedConfigFilePath, configFilePath));

            // Deploy
            _stackName = $"WebAppNoDockerFile{Guid.NewGuid().ToString().Split('-').Last()}";
            ConfigFileHelper.ApplyReplacementTokens(new Dictionary<string, string> { {stackNamePlaceholder, _stackName } }, configFilePath);

            var deployArgs = new[] { "deploy", "--project-path", projectPath, "--apply", configFilePath, "--silent", "--diagnostics" };
            Assert.AreEqual(CommandReturnCodes.SUCCESS, await _app.Run(deployArgs));

            // Verify application is deployed and running
            Assert.AreEqual(StackStatus.CREATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(_stackName));

            var deployStdOut = _interactiveService.StdOutReader.ReadAllLines();

            // Example:     Endpoint: http://52.36.216.238/
            var applicationUrl = deployStdOut.First(line => line.Trim().StartsWith("Endpoint:"))
                .Split(" ")[1]
                .Trim();

            // URL could take few more minutes to come live, therefore, we want to wait and keep trying for a specified timeout
            await _httpHelper.WaitUntilSuccessStatusCode(applicationUrl, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5));

            // list
            var listArgs = new[] { "list-deployments", "--diagnostics" };
            Assert.AreEqual(CommandReturnCodes.SUCCESS, await _app.Run(listArgs));;

            // Verify stack exists in list of deployments
            var listDeployStdOut = _interactiveService.StdOutReader.ReadAllLines().Select(x => x.Split()[0]).ToList();
            CollectionAssert.Contains(listDeployStdOut, _stackName);

            // Arrange input for delete
            await _interactiveService.StdInWriter.WriteAsync("y"); // Confirm delete
            await _interactiveService.StdInWriter.FlushAsync();
            var deleteArgs = new[] { "delete-deployment", _stackName, "--diagnostics" };

            // Delete
            Assert.AreEqual(CommandReturnCodes.SUCCESS, await _app.Run(deleteArgs));;

            // Verify application is deleted
            Assert.True(await _cloudFormationHelper.IsStackDeleted(_stackName), $"{_stackName} still exists.");

            _interactiveService.ReadStdOutStartToEnd();
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
