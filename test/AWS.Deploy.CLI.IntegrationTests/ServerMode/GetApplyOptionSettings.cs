// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.Runtime;
using AWS.Deploy.CLI.Commands;
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.CLI.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Helpers;
using AWS.Deploy.CLI.IntegrationTests.Services;
using AWS.Deploy.ServerMode.Client;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;

namespace AWS.Deploy.CLI.IntegrationTests.ServerMode
{
    public class GetApplyOptionSettings : IDisposable
    {
        private bool _isDisposed;
        private string _stackName;
        private readonly IServiceProvider _serviceProvider;
        private readonly CloudFormationHelper _cloudFormationHelper;

        private readonly string _awsRegion;
        private readonly TestAppManager _testAppManager;
        private readonly InMemoryInteractiveService _interactiveService;

        public GetApplyOptionSettings()
        {
            _interactiveService = new InMemoryInteractiveService();

            var cloudFormationClient = new AmazonCloudFormationClient(Amazon.RegionEndpoint.USWest2);
            _cloudFormationHelper = new CloudFormationHelper(cloudFormationClient);

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddCustomServices();
            serviceCollection.AddTestServices();

            _serviceProvider = serviceCollection.BuildServiceProvider();

            _awsRegion = "us-west-2";

            _testAppManager = new TestAppManager();
        }

        public Task<AWSCredentials> ResolveCredentials()
        {
            var testCredentials = FallbackCredentialsFactory.GetCredentials();
            return Task.FromResult<AWSCredentials>(testCredentials);
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

        [Fact]
        public async Task GetAndApplyAppRunnerSettings_VPCConnector()
        {
            _stackName = $"ServerModeWebFargate{Guid.NewGuid().ToString().Split('-').Last()}";

            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppWithDockerFile", "WebAppWithDockerFile.csproj"));
            var portNumber = 4001;
            using var httpClient = ServerModeHttpClientFactory.ConstructHttpClient(ResolveCredentials);

            var serverCommand = new ServerModeCommand(_serviceProvider.GetRequiredService<IToolInteractiveService>(), portNumber, null, true);
            var cancelSource = new CancellationTokenSource();

            var serverTask = serverCommand.ExecuteAsync(cancelSource.Token);
            try
            {
                var baseUrl = $"http://localhost:{portNumber}/";
                var restClient = new RestAPIClient(baseUrl, httpClient);

                await WaitTillServerModeReady(restClient);

                var startSessionOutput = await restClient.StartDeploymentSessionAsync(new StartDeploymentSessionInput
                {
                    AwsRegion = _awsRegion,
                    ProjectPath = projectPath
                });

                var sessionId = startSessionOutput.SessionId;
                Assert.NotNull(sessionId);

                var signalRClient = new DeploymentCommunicationClient(baseUrl);
                await signalRClient.JoinSession(sessionId);

                var logOutput = new StringBuilder();
                signalRClient.ReceiveLogAllLogAction = (line) =>
                {
                    logOutput.AppendLine(line);
                };

                var getRecommendationOutput = await restClient.GetRecommendationsAsync(sessionId);
                Assert.NotEmpty(getRecommendationOutput.Recommendations);

                var appRunnerRecommendation = getRecommendationOutput.Recommendations.FirstOrDefault(x => string.Equals(x.RecipeId, "AspNetAppAppRunner"));
                Assert.NotNull(appRunnerRecommendation);

                await restClient.SetDeploymentTargetAsync(sessionId, new SetDeploymentTargetInput
                {
                    NewDeploymentName = _stackName,
                    NewDeploymentRecipeId = appRunnerRecommendation.RecipeId
                });

                var vpcResources = await restClient.GetConfigSettingResourcesAsync(sessionId, "VPCConnector.VpcId");
                var subnetsResourcesEmpty = await restClient.GetConfigSettingResourcesAsync(sessionId, "VPCConnector.Subnets");
                var securityGroupsResourcesEmpty = await restClient.GetConfigSettingResourcesAsync(sessionId, "VPCConnector.SecurityGroups");
                Assert.NotEmpty(vpcResources.Resources);
                Assert.Empty(subnetsResourcesEmpty.Resources);
                Assert.Empty(securityGroupsResourcesEmpty.Resources);

                await restClient.ApplyConfigSettingsAsync(sessionId, new ApplyConfigSettingsInput()
                {
                    UpdatedSettings = new Dictionary<string, string>()
                    {
                        {"VPCConnector.CreateNew", "true"},
                        {"VPCConnector.VpcId", vpcResources.Resources.First().SystemName}
                    }
                });

                var subnetsResources = await restClient.GetConfigSettingResourcesAsync(sessionId, "VPCConnector.Subnets");
                var securityGroupsResources = await restClient.GetConfigSettingResourcesAsync(sessionId, "VPCConnector.SecurityGroups");
                Assert.NotEmpty(subnetsResources.Resources);
                Assert.NotEmpty(securityGroupsResources.Resources);

                var setConfigResult = await restClient.ApplyConfigSettingsAsync(sessionId, new ApplyConfigSettingsInput()
                {
                    UpdatedSettings = new Dictionary<string, string>()
                    {
                        {"VPCConnector.Subnets", JsonConvert.SerializeObject(new List<string>{subnetsResources.Resources.Last().SystemName})},
                        {"VPCConnector.SecurityGroups", JsonConvert.SerializeObject(new List<string>{securityGroupsResources.Resources.Last().SystemName})}
                    }
                });

                await restClient.StartDeploymentAsync(sessionId);

                await WaitForDeployment(restClient, sessionId);

                var stackStatus = await _cloudFormationHelper.GetStackStatus(_stackName);
                Assert.Equal(StackStatus.CREATE_COMPLETE, stackStatus);

                Assert.True(logOutput.Length > 0);
                Assert.Contains("Initiating deployment", logOutput.ToString());

                var redeploymentSessionOutput = await restClient.StartDeploymentSessionAsync(new StartDeploymentSessionInput
                {
                    AwsRegion = _awsRegion,
                    ProjectPath = projectPath
                });

                var redeploymentSessionId = redeploymentSessionOutput.SessionId;

                var existingDeployments = await restClient.GetExistingDeploymentsAsync(redeploymentSessionId);
                var existingDeployment = existingDeployments.ExistingDeployments.First(x => string.Equals(_stackName, x.Name));

                Assert.Equal(_stackName, existingDeployment.Name);
                Assert.Equal(appRunnerRecommendation.RecipeId, existingDeployment.RecipeId);
                Assert.Equal(appRunnerRecommendation.Name, existingDeployment.RecipeName);
                Assert.Equal(appRunnerRecommendation.ShortDescription, existingDeployment.ShortDescription);
                Assert.Equal(appRunnerRecommendation.Description, existingDeployment.Description);
                Assert.Equal(appRunnerRecommendation.TargetService, existingDeployment.TargetService);
                Assert.Equal(DeploymentTypes.CloudFormationStack, existingDeployment.DeploymentType);
            }
            finally
            {
                cancelSource.Cancel();
                await _cloudFormationHelper.DeleteStack(_stackName);
                _stackName = null;
            }
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
                if(!string.IsNullOrEmpty(_stackName))
                {
                    var isStackDeleted = _cloudFormationHelper.IsStackDeleted(_stackName).GetAwaiter().GetResult();
                    if (!isStackDeleted)
                    {
                        _cloudFormationHelper.DeleteStack(_stackName).GetAwaiter().GetResult();
                    }

                    _interactiveService.ReadStdOutStartToEnd();
                }
            }

            _isDisposed = true;
        }

        ~GetApplyOptionSettings()
        {
            Dispose(false);
        }
    }
}
