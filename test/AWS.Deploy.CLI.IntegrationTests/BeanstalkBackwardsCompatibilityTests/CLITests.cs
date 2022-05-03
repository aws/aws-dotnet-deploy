// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.CLI.IntegrationTests.Extensions;
using Xunit;

namespace AWS.Deploy.CLI.IntegrationTests.BeanstalkBackwardsCompatibilityTests
{
    [Collection(nameof(TestContextFixture))]
    public class CLITests
    {
        private readonly TestContextFixture _fixture;

        public CLITests(TestContextFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task DeployToExistingBeanstalkEnvironment()
        {
            var projectPath = _fixture.TestAppManager.GetProjectPath(Path.Combine("testapps", "WebAppNoDockerFile", "WebAppNoDockerFile.csproj"));
            var deployArgs = new[] { "deploy", "--project-path", projectPath, "--application-name", _fixture.EnvironmentName, "--diagnostics", "--silent", "--region", "us-west-2" };
            Assert.Equal(CommandReturnCodes.SUCCESS, await _fixture.App.Run(deployArgs));

            var environmentDescription = await _fixture.AWSResourceQueryer.DescribeElasticBeanstalkEnvironment(_fixture.EnvironmentName);

            // URL could take few more minutes to come live, therefore, we want to wait and keep trying for a specified timeout
            await _fixture.HttpHelper.WaitUntilSuccessStatusCode(environmentDescription.CNAME, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5));

            var successMessagePrefix = $"The Elastic Beanstalk Environment {_fixture.EnvironmentName} has been successfully updated";
            var deployStdOutput = _fixture.InteractiveService.StdOutReader.ReadAllLines();
            var successMessage = deployStdOutput.First(line => line.Trim().StartsWith(successMessagePrefix));
            Assert.False(string.IsNullOrEmpty(successMessage));

            var expectedVersionLabel = successMessage.Split(" ").Last();
            Assert.True(await _fixture.EBHelper.VerifyEnvironmentVersionLabel(_fixture.EnvironmentName, expectedVersionLabel));
        }
    }
}
