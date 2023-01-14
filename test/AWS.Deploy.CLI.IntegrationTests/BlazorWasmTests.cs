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
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace AWS.Deploy.CLI.IntegrationTests
{
    [TestFixture]
    public class BlazorWasmTests
    {
        private HttpHelper _httpHelper;
        private CloudFormationHelper _cloudFormationHelper;
        private CloudFrontHelper _cloudFrontHelper;
        private InMemoryInteractiveService _interactiveService;
        private App _app;
        private string _stackName;
        private TestAppManager _testAppManager;

        [SetUp]
        public void Initialize()
        {
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
            _cloudFrontHelper = new CloudFrontHelper(new Amazon.CloudFront.AmazonCloudFrontClient());

            _testAppManager = new TestAppManager();
        }

        [Test]
        [TestCase("testapps", "BlazorWasm60", "BlazorWasm60.csproj")]
        public async Task DefaultConfigurations(params string[] components)
        {
            _stackName = $"{components[1]}{Guid.NewGuid().ToString().Split('-').Last()}";

            // Arrange input for deploy
            await _interactiveService.StdInWriter.WriteAsync(Environment.NewLine); // Select default recommendation
            await _interactiveService.StdInWriter.WriteAsync(Environment.NewLine); // Select default option settings
            await _interactiveService.StdInWriter.FlushAsync();

            // Deploy
            var deployArgs = new[] { "deploy", "--project-path", _testAppManager.GetProjectPath(Path.Combine(components)), "--application-name", _stackName, "--diagnostics" };
            Assert.AreEqual(CommandReturnCodes.SUCCESS, await _app.Run(deployArgs));

            // Verify application is deployed and running
            Assert.AreEqual(StackStatus.CREATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(_stackName));

            var deployStdOut = _interactiveService.StdOutReader.ReadAllLines();

            var tempCdkProjectLine = deployStdOut.First(line => line.StartsWith("Saving AWS CDK deployment project to: "));
            var tempCdkProject = tempCdkProjectLine.Split(": ")[1].Trim();
            Assert.False(Directory.Exists(tempCdkProject), $"{tempCdkProject} must not exist.");

            // Example URL string: BlazorWasm6068e7a879d5ee.EndpointURL = http://blazorwasm6068e7a879d5ee-blazorhostc7106839-a2585dcq9xve.s3-website-us-west-2.amazonaws.com/
            var applicationUrl = deployStdOut.First(line => line.Contains("https://") && line.Contains("cloudfront.net/"))
                .Split("=")[1]
                .Trim();

            // URL could take few more minutes to come live, therefore, we want to wait and keep trying for a specified timeout
            await _httpHelper.WaitUntilSuccessStatusCode(applicationUrl, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5));

            // The initial state of logging should be false, the test will test enabling logging during redeployment.
            var distributionId = await _cloudFormationHelper.GetResourceId(_stackName, "RecipeCloudFrontDistribution2BE25932");
            var distribution = await _cloudFrontHelper.GetDistribution(distributionId);
            Assert.AreEqual(Amazon.CloudFront.PriceClass.PriceClass_All, distribution.DistributionConfig.PriceClass);

            // list
            var listArgs = new[] { "list-deployments", "--diagnostics" };
            Assert.AreEqual(CommandReturnCodes.SUCCESS, await _app.Run(listArgs));;

            // Verify stack exists in list of deployments
            var listDeployStdOut = _interactiveService.StdOutReader.ReadAllLines().Select(x => x.Split()[0]).ToList();
            CollectionAssert.Contains(listDeployStdOut, _stackName);

            // Setup for redeployment turning on access logging via settings file.
            await _interactiveService.StdInWriter.WriteAsync(Environment.NewLine); // Select default option settings
            await _interactiveService.StdInWriter.FlushAsync();

            var applyLoggingSettingsFile = Path.Combine(Directory.GetParent(_testAppManager.GetProjectPath(Path.Combine(components))).FullName, "apply-settings.json");
            deployArgs = new[] { "deploy", "--project-path", _testAppManager.GetProjectPath(Path.Combine(components)), "--application-name", _stackName, "--diagnostics", "--apply", applyLoggingSettingsFile };
            Assert.AreEqual(CommandReturnCodes.SUCCESS, await _app.Run(deployArgs));

            // URL could take few more minutes to come live, therefore, we want to wait and keep trying for a specified timeout
            await _httpHelper.WaitUntilSuccessStatusCode(applicationUrl, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5));

            distribution = await _cloudFrontHelper.GetDistribution(distributionId);
            Assert.AreEqual(Amazon.CloudFront.PriceClass.PriceClass_100, distribution.DistributionConfig.PriceClass);

            // Arrange input for delete
            await _interactiveService.StdInWriter.WriteAsync("y"); // Confirm delete
            await _interactiveService.StdInWriter.FlushAsync();
            var deleteArgs = new[] { "delete-deployment", _stackName, "--diagnostics" };

            // Delete
            Assert.AreEqual(CommandReturnCodes.SUCCESS, await _app.Run(deleteArgs));;

            // Verify application is deleted
            Assert.True(await _cloudFormationHelper.IsStackDeleted(_stackName), $"{_stackName} still exists.");
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
