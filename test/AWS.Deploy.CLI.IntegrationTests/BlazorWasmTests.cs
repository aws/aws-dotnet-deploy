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

namespace AWS.Deploy.CLI.IntegrationTests
{
    public class BlazorWasmTests : IDisposable
    {
        private readonly IServiceCollection _serviceCollection;
        private readonly CloudFormationHelper _cloudFormationHelper;
        private readonly CloudFrontHelper _cloudFrontHelper;
        private string? _stackName;
        private bool _isDisposed;
        private readonly TestAppManager _testAppManager;

        public BlazorWasmTests()
        {
            _serviceCollection = new ServiceCollection();

            _serviceCollection.AddCustomServices();
            _serviceCollection.AddTestServices();

            var cloudFormationClient = new AmazonCloudFormationClient();
            _cloudFormationHelper = new CloudFormationHelper(cloudFormationClient);
            _cloudFrontHelper = new CloudFrontHelper(new Amazon.CloudFront.AmazonCloudFrontClient());

            _testAppManager = new TestAppManager();
        }

        [Theory]
        [InlineData("testapps", "BlazorWasm60", "BlazorWasm60.csproj")]
        public async Task DefaultConfigurations(params string[] components)
        {
            _stackName = $"{components[1]}{Guid.NewGuid().ToString().Split('-').Last()}";
            string? applicationUrl = null;
            string? distributionId = null;
            InMemoryInteractiveService interactiveService = null!;
            try
            {
                // Deploy
                var deployArgs = new[] { "deploy", "--project-path", _testAppManager.GetProjectPath(Path.Combine(components)), "--application-name", _stackName, "--diagnostics" };
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

                var deployStdOut = interactiveService.StdOutReader.ReadAllLines();

                var tempCdkProjectLine = deployStdOut.First(line => line.StartsWith("Saving AWS CDK deployment project to: "));
                var tempCdkProject = tempCdkProjectLine.Split(": ")[1].Trim();
                Assert.False(Directory.Exists(tempCdkProject), $"{tempCdkProject} must not exist.");

                // Example URL string: BlazorWasm6068e7a879d5ee.EndpointURL = http://blazorwasm6068e7a879d5ee-blazorhostc7106839-a2585dcq9xve.s3-website-us-west-2.amazonaws.com/
                applicationUrl = deployStdOut.First(line => line.Contains("https://") && line.Contains("cloudfront.net/"))
                    .Split("=")[1]
                    .Trim();

                // URL could take few more minutes to come live, therefore, we want to wait and keep trying for a specified timeout
                var httpHelper = new HttpHelper(interactiveService);
                await httpHelper.WaitUntilSuccessStatusCode(applicationUrl, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5));

                // The initial state of logging should be false, the test will test enabling logging during redeployment.
                distributionId = await _cloudFormationHelper.GetResourceId(_stackName, "RecipeCloudFrontDistribution2BE25932");
                var distribution = await _cloudFrontHelper.GetDistribution(distributionId);
                Assert.Equal(Amazon.CloudFront.PriceClass.PriceClass_All, distribution.DistributionConfig.PriceClass);
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
                var applyLoggingSettingsFile = Path.Combine(Directory.GetParent(_testAppManager.GetProjectPath(Path.Combine(components)))!.FullName, "apply-settings.json");
                var deployArgs = new[] { "deploy", "--project-path", _testAppManager.GetProjectPath(Path.Combine(components)), "--application-name", _stackName, "--diagnostics", "--apply", applyLoggingSettingsFile };
                Assert.Equal(CommandReturnCodes.SUCCESS, await _serviceCollection.RunDeployToolAsync(deployArgs,
                    provider =>
                    {
                        interactiveService = provider.GetRequiredService<InMemoryInteractiveService>();

                        // Setup for redeployment turning on access logging via settings file.
                        interactiveService.StdInWriter.Write(Environment.NewLine); // Select default option settings
                        interactiveService.StdInWriter.Flush();
                    }));

                // URL could take few more minutes to come live, therefore, we want to wait and keep trying for a specified timeout
                var httpHelper = new HttpHelper(interactiveService);
                await httpHelper.WaitUntilSuccessStatusCode(applicationUrl, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5));

                var distribution = await _cloudFrontHelper.GetDistribution(distributionId);
                Assert.Equal(Amazon.CloudFront.PriceClass.PriceClass_100, distribution.DistributionConfig.PriceClass);
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

        ~BlazorWasmTests()
        {
            Dispose(false);
        }
    }
}
