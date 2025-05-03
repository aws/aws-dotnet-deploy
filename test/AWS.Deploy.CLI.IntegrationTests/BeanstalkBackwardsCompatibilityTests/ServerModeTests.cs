// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AWS.Deploy.CLI.Commands;
using AWS.Deploy.CLI.Commands.Settings;
using AWS.Deploy.CLI.IntegrationTests.Utilities;
using AWS.Deploy.ServerMode.Client;
using AWS.Deploy.ServerMode.Client.Utilities;
using Xunit;

namespace AWS.Deploy.CLI.IntegrationTests.BeanstalkBackwardsCompatibilityTests
{
    [Collection(nameof(TestContextFixture))]
    public class ServerModeTests(TestContextFixture fixture)
    {
        private const string BEANSTALK_ENVIRONMENT_RECIPE_ID = "AspNetAppExistingBeanstalkEnvironment";

        [Fact]
        public async Task DeployToExistingBeanstalkEnvironment()
        {
            var projectPath = fixture.TestAppManager.GetProjectPath(Path.Combine("testapps", "WebAppNoDockerFile", "WebAppNoDockerFile.csproj"));
            var portNumber = 4031;
            using var httpClient = ServerModeHttpClientFactory.ConstructHttpClient(ServerModeUtilities.ResolveDefaultCredentials);

            var serverCommandSettings = new ServerModeCommandSettings
            {
                Port = portNumber,
                ParentPid = null,
                UnsecureMode = true
            };
            var serverCommand = new ServerModeCommand(fixture.ToolInteractiveService);
            var cancelSource = new CancellationTokenSource();

            var serverTask = serverCommand.ExecuteAsync(null!, serverCommandSettings, cancelSource);
            try
            {
                var baseUrl = $"http://localhost:{portNumber}/";
                var restClient = new RestAPIClient(baseUrl, httpClient);

                await restClient.WaitUntilServerModeReady();

                var startSessionOutput = await restClient.StartDeploymentSessionAsync(new StartDeploymentSessionInput
                {
                    AwsRegion = "us-west-2",
                    ProjectPath = projectPath
                });

                var sessionId = startSessionOutput.SessionId;
                Assert.NotNull(sessionId);

                var existingDeployments = await restClient.GetExistingDeploymentsAsync(sessionId);
                var existingDeployment = existingDeployments.ExistingDeployments.First(x => string.Equals(fixture.EnvironmentName, x.Name));

                Assert.Equal(fixture.EnvironmentName, existingDeployment.Name);
                Assert.Equal(BEANSTALK_ENVIRONMENT_RECIPE_ID, existingDeployment.RecipeId);
                Assert.Null(existingDeployment.BaseRecipeId);
                Assert.False(existingDeployment.IsPersistedDeploymentProject);
                Assert.Equal(fixture.EnvironmentId, existingDeployment.ExistingDeploymentId);
                Assert.Equal(DeploymentTypes.BeanstalkEnvironment, existingDeployment.DeploymentType);

                var signalRClient = new DeploymentCommunicationClient(baseUrl);
                await signalRClient.JoinSession(sessionId);

                var logOutput = new StringBuilder();
                AWS.Deploy.CLI.IntegrationTests.ServerModeTests.RegisterSignalRMessageCallbacks(signalRClient, logOutput);

                await restClient.SetDeploymentTargetAsync(sessionId, new SetDeploymentTargetInput
                {
                    ExistingDeploymentId = fixture.EnvironmentId
                });

                await restClient.StartDeploymentAsync(sessionId);

                await restClient.WaitForDeployment(sessionId);

                Assert.True(logOutput.Length > 0);
                var successMessagePrefix = $"The Elastic Beanstalk Environment {fixture.EnvironmentName} has been successfully updated";
                var deployStdOutput = logOutput.ToString().Split(Environment.NewLine);
                var successMessage = deployStdOutput.First(line => line.Trim().StartsWith(successMessagePrefix));
                Assert.False(string.IsNullOrEmpty(successMessage));

                var expectedVersionLabel = successMessage.Split(" ").Last();
                Assert.True(await fixture.EBHelper.VerifyEnvironmentVersionLabel(fixture.EnvironmentName, expectedVersionLabel));
            }
            finally
            {
                cancelSource.Cancel();
            }
        }
    }
}
