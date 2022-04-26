// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using AWS.Deploy.CLI.Commands;
using AWS.Deploy.CLI.IntegrationTests.Helpers;
using AWS.Deploy.ServerMode.Client;
using Xunit;

namespace AWS.Deploy.CLI.IntegrationTests.BeanstalkBackwardsCompatibilityTests
{
    [Collection(nameof(TestContextFixture))]
    public class ServerModeTests
    {
        private readonly TestContextFixture _fixture;
        private const string BEANSTALK_ENVIRONMENT_RECIPE_ID = "AspNetAppExistingBeanstalkEnvironment";

        public ServerModeTests(TestContextFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task DeployToExistingBeanstalkEnvironment()
        {
            var projectPath = _fixture.TestAppManager.GetProjectPath(Path.Combine("testapps", "WebAppNoDockerFile", "WebAppNoDockerFile.csproj"));
            var portNumber = 4001;
            using var httpClient = ServerModeHttpClientFactory.ConstructHttpClient(ResolveCredentials);

            var serverCommand = new ServerModeCommand(_fixture.ToolInteractiveService, portNumber, null, true);
            var cancelSource = new CancellationTokenSource();

            var serverTask = serverCommand.ExecuteAsync(cancelSource.Token);
            try
            {
                var baseUrl = $"http://localhost:{portNumber}/";
                var restClient = new RestAPIClient(baseUrl, httpClient);

                await WaitTillServerModeReady(restClient);

                var startSessionOutput = await restClient.StartDeploymentSessionAsync(new StartDeploymentSessionInput
                {
                    AwsRegion = "us-west-2",
                    ProjectPath = projectPath
                });

                var sessionId = startSessionOutput.SessionId;
                Assert.NotNull(sessionId);

                var existingDeployments = await restClient.GetExistingDeploymentsAsync(sessionId);
                var existingDeployment = existingDeployments.ExistingDeployments.First(x => string.Equals(_fixture.EnvironmentName, x.Name));

                Assert.Equal(_fixture.EnvironmentName, existingDeployment.Name);
                Assert.Equal(BEANSTALK_ENVIRONMENT_RECIPE_ID, existingDeployment.RecipeId);
                Assert.Equal(_fixture.EnvironmentId, existingDeployment.ExistingDeploymentId);
                Assert.Equal(DeploymentTypes.BeanstalkEnvironment, existingDeployment.DeploymentType);

                var signalRClient = new DeploymentCommunicationClient(baseUrl);
                await signalRClient.JoinSession(sessionId);

                var logOutput = new StringBuilder();
                AWS.Deploy.CLI.IntegrationTests.ServerModeTests.RegisterSignalRMessageCallbacks(signalRClient, logOutput);

                await restClient.SetDeploymentTargetAsync(sessionId, new SetDeploymentTargetInput
                {
                    ExistingDeploymentId = _fixture.EnvironmentId
                });

                await restClient.StartDeploymentAsync(sessionId);

                await WaitForDeployment(restClient, sessionId);

                Assert.True(logOutput.Length > 0);
                var successMessagePrefix = $"The Elastic Beanstalk Environment {_fixture.EnvironmentName} has been successfully updated";
                var deployStdOutput = logOutput.ToString().Split(Environment.NewLine);
                var successMessage = deployStdOutput.First(line => line.Trim().StartsWith(successMessagePrefix));
                Assert.False(string.IsNullOrEmpty(successMessage));

                var expectedVersionLabel = successMessage.Split(" ").Last();
                Assert.True(await _fixture.EBHelper.VerifyEnvironmentVersionLabel(_fixture.EnvironmentName, expectedVersionLabel));
            }
            finally
            {
                cancelSource.Cancel();
            }
        }

        private async Task<DeploymentStatus> WaitForDeployment(RestAPIClient restApiClient, string sessionId)
        {
            // Do an initial delay to avoid a race condition of the status being checked before the deployment has kicked off.
            await Task.Delay(TimeSpan.FromSeconds(3));

            await WaitUntilHelper.WaitUntil(async () =>
            {
                DeploymentStatus status = (await restApiClient.GetDeploymentStatusAsync(sessionId)).Status; ;
                return status != DeploymentStatus.Executing;
            }, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(15));

            return (await restApiClient.GetDeploymentStatusAsync(sessionId)).Status;
        }

        private async Task WaitTillServerModeReady(RestAPIClient restApiClient)
        {
            await WaitUntilHelper.WaitUntil(async () =>
            {
                SystemStatus status = SystemStatus.Error;
                try
                {
                    status = (await restApiClient.HealthAsync()).Status;
                }
                catch (Exception)
                {
                }

                return status == SystemStatus.Ready;
            }, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10));
        }

        private Task<AWSCredentials> ResolveCredentials()
        {
            var testCredentials = FallbackCredentialsFactory.GetCredentials();
            return Task.FromResult<AWSCredentials>(testCredentials);
        }
    }
}
