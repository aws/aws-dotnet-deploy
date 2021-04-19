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
using Environment = System.Environment;

namespace AWS.Deploy.CLI.IntegrationTests
{
    public class WebAppNoDockerFileTest
    {
        private readonly HttpHelper _httpHelper;
        private readonly CloudFormationHelper _cloudFormationHelper;

        public WebAppNoDockerFileTest()
        {
            _httpHelper = new HttpHelper();

            var cloudFormationClient = new AmazonCloudFormationClient();
            _cloudFormationHelper = new CloudFormationHelper(cloudFormationClient);
        }

        [Fact]
        public async Task DefaultConfigurations()
        {
            var projectPath = Path.Combine("testapps", "WebAppNoDockerFile", "WebAppNoDockerFile.csproj");
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddCustomServices();
            serviceCollection.AddTestServices();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            // calls the Run method in App, which is replacing Main
            var app = serviceProvider.GetService<App>();
            Assert.NotNull(app);

            var toolInteractiveService = (InMemoryInteractiveService)serviceProvider.GetService<IToolInteractiveService>();
            Assert.NotNull(toolInteractiveService);

            var stackName = $"WebAppNoDockerFile{Guid.NewGuid().ToString().Split('-').Last()}";

            await toolInteractiveService.StdInWriter.WriteAsync(Environment.NewLine);
            await toolInteractiveService.StdInWriter.WriteAsync(Environment.NewLine);
            await toolInteractiveService.StdInWriter.FlushAsync();

            var deployArgs = new[] { "deploy", "--project-path", projectPath, "--stack-name", stackName };

            await app.Run(deployArgs);

            Assert.Equal(StackStatus.CREATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(stackName));

            var deployStdOut = toolInteractiveService.StdOutReader.ReadAllLines();

            // Example: WebAppNoDockerFile-3cf258f103d2.EndpointURL = http://52.36.216.238/
            var applicationUrl = deployStdOut.First(line => line.StartsWith($"{stackName}.EndpointURL"))
                .Split("=")[1]
                .Trim();
            Assert.True(await _httpHelper.IsSuccessStatusCode(applicationUrl));

            await toolInteractiveService.StdInWriter.WriteAsync("y");
            await toolInteractiveService.StdInWriter.FlushAsync();
            var deleteArgs = new[] { "delete-deployment", stackName };
            await app.Run(deleteArgs);

            var exception = await Assert.ThrowsAsync<AmazonCloudFormationException>(async () =>
            {
                await _cloudFormationHelper.GetStackStatus(stackName);
            });
            Assert.Equal($"Stack with id {stackName} does not exist", exception.Message);
        }
    }
}
