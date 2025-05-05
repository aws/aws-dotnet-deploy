// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.CLI.IntegrationTests.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AWS.Deploy.CLI.IntegrationTests.BeanstalkBackwardsCompatibilityTests.ExistingWindowsEnvironment
{
    [Collection(nameof(WindowsTestContextFixture))]
    public class CLITests(WindowsTestContextFixture fixture)
    {
        [Fact]
        public async Task DeployToExistingBeanstalkEnvironment()
        {
            var projectPath = fixture.TestAppManager.GetProjectPath(Path.Combine("testapps", "WebAppNoDockerFile", "WebAppNoDockerFile.csproj"));
            var deployArgs = new[] { "deploy", "--project-path", projectPath, "--application-name", fixture.EnvironmentName, "--diagnostics", "--silent", "--region", "us-west-2" };
            InMemoryInteractiveService interactiveService = null!;
            Assert.Equal(CommandReturnCodes.SUCCESS, await fixture.ServiceCollection.RunDeployToolAsync(deployArgs,
                provider =>
                {
                    interactiveService = provider.GetRequiredService<InMemoryInteractiveService>();
                }));

            var environmentDescription = await fixture.AWSResourceQueryer.DescribeElasticBeanstalkEnvironment(fixture.EnvironmentName);

            // URL could take few more minutes to come live, therefore, we want to wait and keep trying for a specified timeout
            await fixture.HttpHelper.WaitUntilSuccessStatusCode(environmentDescription.CNAME, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5));

            var successMessagePrefix = $"The Elastic Beanstalk Environment {fixture.EnvironmentName} has been successfully updated";
            var deployStdOutput = interactiveService.StdOutReader.ReadAllLines();
            var successMessage = deployStdOutput.First(line => line.Trim().StartsWith(successMessagePrefix));
            Assert.False(string.IsNullOrEmpty(successMessage));

            var expectedVersionLabel = successMessage.Split(" ").Last();
            Assert.True(await fixture.EBHelper.VerifyEnvironmentVersionLabel(fixture.EnvironmentName, expectedVersionLabel));
        }
    }
}
