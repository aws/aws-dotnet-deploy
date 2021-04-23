// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using AWS.Deploy.CLI.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Helpers;
using AWS.Deploy.CLI.IntegrationTests.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static System.Net.WebRequestMethods;

namespace AWS.Deploy.CLI.IntegrationTests
{
    [Collection("Serial")]
    public class BlazorWasmTests : IDisposable
    {
        private readonly HttpHelper _httpHelper;
        private readonly CloudFormationHelper _cloudFormationHelper;
        private readonly InMemoryInteractiveService _interactiveService;
        private readonly App _app;
        private string _stackName;
        private bool _isDisposed;

        public BlazorWasmTests()
        {
            _httpHelper = new HttpHelper();

            var cloudFormationClient = new AmazonCloudFormationClient();
            _cloudFormationHelper = new CloudFormationHelper(cloudFormationClient);

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddCustomServices();
            serviceCollection.AddTestServices();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            _app = serviceProvider.GetService<App>();
            Assert.NotNull(_app);

            _interactiveService = serviceProvider.GetService<InMemoryInteractiveService>();
            Assert.NotNull(_interactiveService);
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
            await _interactiveService.StdInWriter.WriteAsync("y"); // Blazor webassembly confirmation
            await _interactiveService.StdInWriter.FlushAsync();

            // Deploy
            var deployArgs = new[] { "deploy", "--project-path", Path.Combine(components), "--stack-name", _stackName };
            await _app.Run(deployArgs);

            // Verify application is deployed and running
            Assert.Equal(StackStatus.CREATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(_stackName));

            var deployStdOut = _interactiveService.StdOutReader.ReadAllLines();

            // Example URL string: BlazorWasm5068e7a879d5ee.EndpointURL = http://blazorwasm5068e7a879d5ee-blazorhostc7106839-a2585dcq9xve.s3-website-us-west-2.amazonaws.com/
            var applicationUrl = deployStdOut.First(line => line.StartsWith($"{_stackName}.EndpointURL"))
                .Split("=")[1]
                .Trim();

            // URL could take few more minutes to come live, therefore, we want to wait and keep trying for a specified timeout
            await _httpHelper.WaitUntilSuccessStatusCode(applicationUrl, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5));

            // Arrange input for delete
            await _interactiveService.StdInWriter.WriteAsync("y"); // Confirm delete
            await _interactiveService.StdInWriter.FlushAsync();
            var deleteArgs = new[] { "delete-deployment", _stackName };

            // Delete
            await _app.Run(deleteArgs);

            // Verify application is delete
            Assert.True(await _cloudFormationHelper.IsStackDeleted(_stackName));
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
