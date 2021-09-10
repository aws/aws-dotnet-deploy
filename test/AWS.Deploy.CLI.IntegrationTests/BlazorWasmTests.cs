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
using Xunit;
using static System.Net.WebRequestMethods;

namespace AWS.Deploy.CLI.IntegrationTests
{
    public class BlazorWasmTests : IDisposable
    {
        private readonly HttpHelper _httpHelper;
        private readonly CloudFormationHelper _cloudFormationHelper;
        private readonly CloudFrontHelper _cloudFrontHelper;
        private readonly InMemoryInteractiveService _interactiveService;
        private readonly App _app;
        private string _stackName;
        private bool _isDisposed;
        private readonly TestAppManager _testAppManager;

        public BlazorWasmTests()
        {
            _httpHelper = new HttpHelper();

            _cloudFormationHelper = new CloudFormationHelper(new AmazonCloudFormationClient());
            _cloudFrontHelper = new CloudFrontHelper(new Amazon.CloudFront.AmazonCloudFrontClient());

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddCustomServices();
            serviceCollection.AddTestServices();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            _app = serviceProvider.GetService<App>();
            Assert.NotNull(_app);

            _interactiveService = serviceProvider.GetService<InMemoryInteractiveService>();
            Assert.NotNull(_interactiveService);

            _testAppManager = new TestAppManager();
        }

        [Theory]
        [InlineData("testapps", "BlazorWasm31", "BlazorWasm31.csproj")]
        [InlineData("testapps", "BlazorWasm50", "BlazorWasm50.csproj")]
        public async Task DefaultConfigurations(params string[] components)
        {
            _stackName = $"{components[1]}{Guid.NewGuid().ToString().Split('-').Last()}";

            // Arrange input for deploy
            await _interactiveService.StdInWriter.WriteAsync(Environment.NewLine); // Select default recommendation
            await _interactiveService.StdInWriter.WriteAsync(Environment.NewLine); // Select default option settings
            await _interactiveService.StdInWriter.FlushAsync();

            // Deploy
            var deployArgs = new[] { "deploy", "--project-path", _testAppManager.GetProjectPath(Path.Combine(components)), "--stack-name", _stackName, "--diagnostics" };
            await _app.Run(deployArgs);

            // Verify application is deployed and running
            Assert.Equal(StackStatus.CREATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(_stackName));

            var deployStdDebug = _interactiveService.StdDebugReader.ReadAllLines();

            var tempCdkProject = deployStdDebug.FirstOrDefault(line => line.Trim().Contains("The CDK Project is saved at: "))?
                .Split(": ")[1]
                .Trim();

            Assert.NotNull(tempCdkProject);
            Assert.False(Directory.Exists(tempCdkProject));

            var deployStdOut = _interactiveService.StdOutReader.ReadAllLines();

            // Example URL string: BlazorWasm5068e7a879d5ee.EndpointURL = http://blazorwasm5068e7a879d5ee-blazorhostc7106839-a2585dcq9xve.s3-website-us-west-2.amazonaws.com/
            var applicationUrl = deployStdOut.First(line => line.Contains("https://") && line.Contains("cloudfront.net/"))
                .Split("=")[1]
                .Trim();

            // URL could take few more minutes to come live, therefore, we want to wait and keep trying for a specified timeout
            await _httpHelper.WaitUntilSuccessStatusCode(applicationUrl, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5));

            // The initial state of logging should be false, the test will test enabling logging during redeployment.
            var distributionId = await _cloudFormationHelper.GetResourceId(_stackName, "RecipeCloudFrontDistribution2BE25932");
            var distribution = await _cloudFrontHelper.GetDistribution(distributionId);
            Assert.Equal(Amazon.CloudFront.PriceClass.PriceClass_All, distribution.DistributionConfig.PriceClass);

            // list
            var listArgs = new[] { "list-deployments" };
            await _app.Run(listArgs);

            // Verify stack exists in list of deployments
            var listDeployStdOut = _interactiveService.StdOutReader.ReadAllLines();
            Assert.Contains(listDeployStdOut, (deployment) => _stackName.Equals(deployment));

            // Setup for redeployment turning on access logging via settings file.
            await _interactiveService.StdInWriter.WriteAsync(Environment.NewLine); // Select default option settings
            await _interactiveService.StdInWriter.FlushAsync();

            var applyLoggingSettingsFile = Path.Combine(Directory.GetParent(_testAppManager.GetProjectPath(Path.Combine(components))).FullName, "apply-settings.json");
            deployArgs = new[] { "deploy", "--project-path", _testAppManager.GetProjectPath(Path.Combine(components)), "--stack-name", _stackName, "--diagnostics", "--apply", applyLoggingSettingsFile };
            await _app.Run(deployArgs);

            // URL could take few more minutes to come live, therefore, we want to wait and keep trying for a specified timeout
            await _httpHelper.WaitUntilSuccessStatusCode(applicationUrl, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5));

            distribution = await _cloudFrontHelper.GetDistribution(distributionId);
            Assert.Equal(Amazon.CloudFront.PriceClass.PriceClass_100, distribution.DistributionConfig.PriceClass);

            // Arrange input for delete
            await _interactiveService.StdInWriter.WriteAsync("y"); // Confirm delete
            await _interactiveService.StdInWriter.FlushAsync();
            var deleteArgs = new[] { "delete-deployment", _stackName };

            // Delete
            await _app.Run(deleteArgs);

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
            }

            _isDisposed = true;
        }

        ~BlazorWasmTests()
        {
            Dispose(false);
        }
    }
}
