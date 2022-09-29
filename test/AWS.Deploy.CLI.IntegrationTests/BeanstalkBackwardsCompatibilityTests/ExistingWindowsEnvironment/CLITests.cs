// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.CLI.IntegrationTests.Extensions;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Orchestration.ServiceHandlers;
using Moq;
using Xunit;

namespace AWS.Deploy.CLI.IntegrationTests.BeanstalkBackwardsCompatibilityTests.ExistingWindowsEnvironment
{
    [Collection(nameof(WindowsTestContextFixture))]
    public class CLITests
    {
        private readonly WindowsTestContextFixture _fixture;
        private readonly AWSElasticBeanstalkHandler _awsElasticBeanstalkHandler;
        private readonly Mock<IOptionSettingHandler> _optionSettingHandler;
        private readonly ProjectDefinitionParser _projectDefinitionParser;
        private readonly RecipeDefinition _recipeDefinition;

        public CLITests(WindowsTestContextFixture fixture)
        {
            _projectDefinitionParser = new ProjectDefinitionParser(new FileManager(), new DirectoryManager());
            _fixture = fixture;
            _optionSettingHandler = new Mock<IOptionSettingHandler>();
            _awsElasticBeanstalkHandler = new AWSElasticBeanstalkHandler(null, null, null, _optionSettingHandler.Object);
            _recipeDefinition = new Mock<RecipeDefinition>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DeploymentTypes>(),
                It.IsAny<DeploymentBundleTypes>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()).Object;
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
