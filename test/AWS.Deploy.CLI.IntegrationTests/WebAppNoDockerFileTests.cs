// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using NUnit.Framework;
[assembly:Parallelizable(ParallelScope.Children)]

namespace AWS.Deploy.CLI.IntegrationTests
{
    [TestFixture]
    public class WebAppNoDockerFileTests
    {
        private HttpHelper _httpHelper;
        private CloudFormationHelper _cloudFormationHelper;
        private App _app;
        private InMemoryInteractiveService _interactiveService;
        private TestAppManager _testAppManager;
        private string _customWorkspace;
        private readonly Dictionary<string, string> _stackNames = new Dictionary<string, string>();

        [SetUp]
        public void Initialize()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddCustomServices(ServiceLifetime.Scoped);
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

        [Test]
        [TestCase("ElasticBeanStalkConfigFile-Linux.json", true)]
        [TestCase("ElasticBeanStalkConfigFile-Linux-SelfContained.json", true)]
        [TestCase("ElasticBeanStalkConfigFile-Windows.json", false)]
        [TestCase("ElasticBeanStalkConfigFile-Windows-SelfContained.json", false)]
        public async Task EBDefaultConfigurations(string configFile, bool linux)
        {
            var stackName = $"BeanstalkTest-{Guid.NewGuid().ToString().Split('-').Last()}";
            _stackNames.Add(TestContext.CurrentContext.Test.ID, stackName);

            // Deploy
            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppNoDockerFile", "WebAppNoDockerFile.csproj"));
            var deployArgs = new[] { "deploy", "--project-path", projectPath, "--application-name", stackName, "--diagnostics", "--silent", "--apply", configFile };
            Assert.AreEqual(CommandReturnCodes.SUCCESS, await _app.Run(deployArgs));

            // Verify application is deployed and running
            Assert.AreEqual(StackStatus.CREATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(stackName));

            var deployStdOut = _interactiveService.StdOutReader.ReadAllLines();

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
            await _httpHelper.WaitUntilSuccessStatusCode(applicationUrl, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5));

            // list
            var listArgs = new[] { "list-deployments", "--diagnostics" };
            Assert.AreEqual(CommandReturnCodes.SUCCESS, await _app.Run(listArgs)); ;

            // Verify stack exists in list of deployments
            var listStdOut = _interactiveService.StdOutReader.ReadAllLines().Select(x => x.Split()[0]).ToList();
            CollectionAssert.Contains(listStdOut, stackName);

            // Arrange input for delete
            // Use --silent flag to delete without user prompts
            var deleteArgs = new[] { "delete-deployment", stackName, "--diagnostics", "--silent" };

            // Delete
            Assert.AreEqual(CommandReturnCodes.SUCCESS, await _app.Run(deleteArgs)); ;

            // Verify application is deleted
            Assert.True(await _cloudFormationHelper.IsStackDeleted(stackName), $"{stackName} still exists.");
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
