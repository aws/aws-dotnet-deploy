// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Collections.Generic;
using Amazon.CloudFormation;
using Amazon.ECS;
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.CLI.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Helpers;
using AWS.Deploy.CLI.IntegrationTests.Services;
using Microsoft.Extensions.DependencyInjection;
using Task = System.Threading.Tasks.Task;
using NUnit.Framework;
using Amazon.ECS.Model;
using System.Diagnostics;

namespace AWS.Deploy.CLI.IntegrationTests
{
    [TestFixture]
    public class WebAppWithDockerFileTests
    {
        private HttpHelper _httpHelper;
        private CloudFormationHelper _cloudFormationHelper;
        private ECSHelper _ecsHelper;
        private App _app;
        private InMemoryInteractiveService _interactiveService;
        private TestAppManager _testAppManager;
        private readonly Dictionary<string, string> _stackNames = new Dictionary<string, string>();

        [SetUp]
        public void Initialize()
        {
            var ecsClient = new AmazonECSClient();
            _ecsHelper = new ECSHelper(ecsClient);

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddCustomServices(ServiceLifetime.Scoped);
            serviceCollection.AddTestServices();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            _app = serviceProvider.GetService<App>();
            Assert.NotNull(_app);

            _interactiveService = serviceProvider.GetService<InMemoryInteractiveService>();
            Assert.NotNull(_interactiveService);

            _httpHelper = new HttpHelper(_interactiveService);

            var cloudFormationClient = new AmazonCloudFormationClient();
            _cloudFormationHelper = new CloudFormationHelper(cloudFormationClient);

            _testAppManager = new TestAppManager();
        }

        [Test]
        public async Task DefaultConfigurations()
        {
            var stackName = $"WebAppWithDockerFile{Guid.NewGuid().ToString().Split('-').Last()}";
            _stackNames.Add(TestContext.CurrentContext.Test.ID, stackName);

            // Arrange input for deploy
            await _interactiveService.StdInWriter.WriteAsync(Environment.NewLine); // Select default recommendation
            await _interactiveService.StdInWriter.WriteAsync(Environment.NewLine); // Select default option settings
            await _interactiveService.StdInWriter.FlushAsync();

            // Deploy
            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppWithDockerFile", "WebAppWithDockerFile.csproj"));
            var deployArgs = new[] { "deploy", "--project-path", projectPath, "--application-name", stackName, "--diagnostics" };
            Assert.AreEqual(CommandReturnCodes.SUCCESS, await _app.Run(deployArgs));

            // Verify application is deployed and running
            Assert.AreEqual(StackStatus.CREATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(stackName));

            var cluster = await _ecsHelper.GetCluster(stackName);
            Assert.AreEqual("ACTIVE", cluster.Status);

            var deployStdOut = _interactiveService.StdOutReader.ReadAllLines();

            var tempCdkProjectLine = deployStdOut.First(line => line.StartsWith("Saving AWS CDK deployment project to: "));
            var tempCdkProject = tempCdkProjectLine.Split(": ")[1].Trim();
            Assert.False(Directory.Exists(tempCdkProject), $"{tempCdkProject} must not exist.");

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
            CollectionAssert.Contains(listDeployStdOut, stackName);

            // Arrange input for re-deployment
            await _interactiveService.StdInWriter.WriteAsync(Environment.NewLine); // Select default option settings
            await _interactiveService.StdInWriter.FlushAsync();

            // Perform re-deployment
            deployArgs = new[] { "deploy", "--project-path", projectPath, "--application-name", stackName, "--diagnostics" };
            Assert.AreEqual(CommandReturnCodes.SUCCESS, await _app.Run(deployArgs));
            Assert.AreEqual(StackStatus.UPDATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(stackName));
            Assert.AreEqual("ACTIVE", cluster.Status);

            // Arrange input for delete
            await _interactiveService.StdInWriter.WriteAsync("y"); // Confirm delete
            await _interactiveService.StdInWriter.FlushAsync();
            var deleteArgs = new[] { "delete-deployment", stackName, "--diagnostics" };

            // Delete
            Assert.AreEqual(CommandReturnCodes.SUCCESS, await _app.Run(deleteArgs));;

            // Verify application is deleted
            Assert.True(await _cloudFormationHelper.IsStackDeleted(stackName), $"{stackName} still exists.");
        }

        [Test]
        public async Task AppRunnerDeployment()
        {
            var stackNamePlaceholder = "{StackName}";
            var stackName = $"WebAppWithDockerFile{Guid.NewGuid().ToString().Split('-').Last()}";
            _stackNames.Add(TestContext.CurrentContext.Test.ID, stackName);
            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppWithDockerFile", "WebAppWithDockerFile.csproj"));
            var configFilePath = Path.Combine(Directory.GetParent(projectPath).FullName, "AppRunnerConfigFile.json");
            ConfigFileHelper.ApplyReplacementTokens(new Dictionary<string, string> { { stackNamePlaceholder, stackName } }, configFilePath);

            // Deploy
            var deployArgs = new[] { "deploy", "--project-path", projectPath, "--application-name", stackName, "--diagnostics", "--apply", configFilePath, "--silent" };
            Assert.AreEqual(CommandReturnCodes.SUCCESS, await _app.Run(deployArgs));

            // Verify application is deployed and running
            Assert.AreEqual(StackStatus.CREATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(stackName));

            var deployStdOut = _interactiveService.StdOutReader.ReadAllLines();

            var applicationUrl = deployStdOut.First(line => line.StartsWith($"{stackName}.RecipeEndpointURL"))
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
            CollectionAssert.Contains(listDeployStdOut, stackName);

            // Arrange input for delete
            await _interactiveService.StdInWriter.WriteAsync("y"); // Confirm delete
            await _interactiveService.StdInWriter.FlushAsync();
            var deleteArgs = new[] { "delete-deployment", stackName, "--diagnostics" };

            // Delete
            Assert.AreEqual(CommandReturnCodes.SUCCESS, await _app.Run(deleteArgs));;

            // Verify application is deleted
            Assert.True(await _cloudFormationHelper.IsStackDeleted(stackName));
        }

        [TearDown]
        public async Task Cleanup()
        {
            var stackName = _stackNames[TestContext.CurrentContext.Test.ID];
            var isStackDeleted = await _cloudFormationHelper.IsStackDeleted(stackName);
            if (!isStackDeleted)
            {
                await _cloudFormationHelper.DeleteStack(stackName);
            }

            _interactiveService.ReadStdOutStartToEnd();
        }
    }
}
