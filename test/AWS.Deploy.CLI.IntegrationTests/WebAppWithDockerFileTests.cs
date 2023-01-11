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
using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AWS.Deploy.CLI.IntegrationTests
{
    [TestClass]
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
            var ecsClient = new AmazonECSClient();
            _ecsHelper = new ECSHelper(ecsClient);

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddCustomServices();
            serviceCollection.AddTestServices();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            _app = serviceProvider.GetService<App>();
            Assert.IsNotNull(_app);

            _interactiveService = serviceProvider.GetService<InMemoryInteractiveService>();
            Assert.IsNotNull(_interactiveService);

            _httpHelper = new HttpHelper(_interactiveService);

            var cloudFormationClient = new AmazonCloudFormationClient();
            _cloudFormationHelper = new CloudFormationHelper(cloudFormationClient);

            _testAppManager = new TestAppManager();
        }

        [TestMethod]
        public async Task DefaultConfigurations()
        {
            _stackName = $"WebAppWithDockerFile{Guid.NewGuid().ToString().Split('-').Last()}";

            // Arrange input for deploy
            await _interactiveService.StdInWriter.WriteAsync(Environment.NewLine); // Select default recommendation
            await _interactiveService.StdInWriter.WriteAsync(Environment.NewLine); // Select default option settings
            await _interactiveService.StdInWriter.FlushAsync();

            // Deploy
            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppWithDockerFile", "WebAppWithDockerFile.csproj"));
            var deployArgs = new[] { "deploy", "--project-path", projectPath, "--application-name", _stackName, "--diagnostics", "--direct-deploy" };
            Assert.AreEqual(CommandReturnCodes.SUCCESS, await _app.Run(deployArgs));

            // Verify application is deployed and running
            Assert.AreEqual(StackStatus.CREATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(_stackName));

            var cluster = await _ecsHelper.GetCluster(_stackName);
            Assert.AreEqual("ACTIVE", cluster.Status);

            var deployStdOut = _interactiveService.StdOutReader.ReadAllLines();

            var tempCdkProjectLine = deployStdOut.First(line => line.StartsWith("Saving AWS CDK deployment project to: "));
            var tempCdkProject = tempCdkProjectLine.Split(": ")[1].Trim();
            Assert.IsFalse(Directory.Exists(tempCdkProject), $"{tempCdkProject} must not exist.");

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
            

            // Arrange input for re-deployment
            await _interactiveService.StdInWriter.WriteAsync(Environment.NewLine); // Select default option settings
            await _interactiveService.StdInWriter.FlushAsync();

            // Perform re-deployment
            deployArgs = new[] { "deploy", "--project-path", projectPath, "--application-name", _stackName, "--diagnostics", "--direct-deploy" };
            Assert.AreEqual(CommandReturnCodes.SUCCESS, await _app.Run(deployArgs));
            Assert.AreEqual(StackStatus.UPDATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(_stackName));
            Assert.AreEqual("ACTIVE", cluster.Status);

            // Arrange input for delete
            await _interactiveService.StdInWriter.WriteAsync("y"); // Confirm delete
            await _interactiveService.StdInWriter.FlushAsync();
            var deleteArgs = new[] { "delete-deployment", _stackName, "--diagnostics" };

            // Delete
            Assert.AreEqual(CommandReturnCodes.SUCCESS, await _app.Run(deleteArgs));;

            // Verify application is deleted
            Assert.IsTrue(await _cloudFormationHelper.IsStackDeleted(_stackName), $"{_stackName} still exists.");
        }

        [TestMethod]
        public async Task AppRunnerDeployment()
        {
            var stackNamePlaceholder = "{StackName}";
            _stackName = $"WebAppWithDockerFile{Guid.NewGuid().ToString().Split('-').Last()}";
            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppWithDockerFile", "WebAppWithDockerFile.csproj"));
            var configFilePath = Path.Combine(Directory.GetParent(projectPath).FullName, "AppRunnerConfigFile.json");
            ConfigFileHelper.ApplyReplacementTokens(new Dictionary<string, string> { { stackNamePlaceholder, _stackName } }, configFilePath);

            // Deploy
            var deployArgs = new[] { "deploy", "--project-path", projectPath, "--application-name", _stackName, "--diagnostics", "--apply", configFilePath, "--silent", "--direct-deploy" };
            Assert.AreEqual(CommandReturnCodes.SUCCESS, await _app.Run(deployArgs));

            // Verify application is deployed and running
            Assert.AreEqual(StackStatus.CREATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(_stackName));

            var deployStdOut = _interactiveService.StdOutReader.ReadAllLines();

            var applicationUrl = deployStdOut.First(line => line.StartsWith($"{_stackName}.RecipeEndpointURL"))
                .Split("=")[1]
                .Trim();

            StringAssert.Contains("awsapprunner", applicationUrl);

            // URL could take few more minutes to come live, therefore, we want to wait and keep trying for a specified timeout
            await _httpHelper.WaitUntilSuccessStatusCode(applicationUrl, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5));

            // Ensure environemnt variables specified in AppRunnerConfigFile.json are set for the service. 
            var checkEnvironmentVariableUrl = applicationUrl + "envvar/TEST_Key1";
            using var httpClient = new HttpClient();
            var envVarValue = await httpClient.GetStringAsync(checkEnvironmentVariableUrl);
            Assert.AreEqual("Value1", envVarValue);

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
            Assert.IsTrue(await _cloudFormationHelper.IsStackDeleted(_stackName));
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

        ~WebAppWithDockerFileTests()
        {
            Dispose(false);
        }
    }
}
