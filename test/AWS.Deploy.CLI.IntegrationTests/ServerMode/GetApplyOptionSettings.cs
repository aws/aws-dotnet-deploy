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
using Amazon.CloudFormation.Model;
using Amazon.Runtime;
using AWS.Deploy.CLI.Commands;
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.CLI.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Helpers;
using AWS.Deploy.CLI.TypeHintResponses;
using AWS.Deploy.Common;
using AWS.Deploy.Orchestration.Utilities;
using AWS.Deploy.ServerMode.Client;
using AWS.Deploy.Common.TypeHintData;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace AWS.Deploy.CLI.IntegrationTests.ServerMode
{
    public class GetApplyOptionSettings : IDisposable
    {
        private bool _isDisposed;
        private string _stackName;
        private readonly IServiceProvider _serviceProvider;

        private readonly string _awsRegion;
        private readonly TestAppManager _testAppManager;

        private readonly Mock<IAWSClientFactory> _mockAWSClientFactory;
        private readonly Mock<IAmazonCloudFormation> _mockCFClient;

        public GetApplyOptionSettings()
        {
            _mockAWSClientFactory = new Mock<IAWSClientFactory>();
            _mockCFClient = new Mock<IAmazonCloudFormation>();

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddCustomServices();
            serviceCollection.AddTestServices();

            _serviceProvider = serviceCollection.BuildServiceProvider();

            _awsRegion = "us-west-2";

            _testAppManager = new TestAppManager();
        }

        public TemplateMetadataReader GetTemplateMetadataReader(string templateBody)
        {
            var templateMetadataReader = new TemplateMetadataReader(_mockAWSClientFactory.Object);
            var cfResponse = new GetTemplateResponse();
            cfResponse.TemplateBody = templateBody;
            _mockAWSClientFactory.Setup(x => x.GetAWSClient<IAmazonCloudFormation>(It.IsAny<string>())).Returns(_mockCFClient.Object);
            _mockCFClient.Setup(x => x.GetTemplateAsync(It.IsAny<GetTemplateRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(cfResponse);
            return templateMetadataReader;
        }

        public Task<AWSCredentials> ResolveCredentials()
        {
            var testCredentials = FallbackCredentialsFactory.GetCredentials();
            return Task.FromResult<AWSCredentials>(testCredentials);
        }

        [Fact]
        public async Task GetAndApplyAppRunnerSettings_VPCConnector()
        {
            _stackName = $"ServerModeWebAppRunner{Guid.NewGuid().ToString().Split('-').Last()}";

            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppWithDockerFile", "WebAppWithDockerFile.csproj"));
            var portNumber = 4021;
            using var httpClient = ServerModeHttpClientFactory.ConstructHttpClient(ResolveCredentials);

            var serverCommand = new ServerModeCommand(_serviceProvider.GetRequiredService<IToolInteractiveService>(), portNumber, null, true);
            var cancelSource = new CancellationTokenSource();

            var serverTask = serverCommand.ExecuteAsync(cancelSource.Token);
            try
            {
                var baseUrl = $"http://localhost:{portNumber}/";
                var restClient = new RestAPIClient(baseUrl, httpClient);

                await WaitTillServerModeReady(restClient);

                var sessionId = await StartDeploymentSession(restClient, projectPath);

                var logOutput = new StringBuilder();
                await SetupSignalRConnection(baseUrl, sessionId, logOutput);

                var appRunnerRecommendation = await GetRecommendationsAndSelectAppRunner(restClient, sessionId);

                var vpcResources = await restClient.GetConfigSettingResourcesAsync(sessionId, "VPCConnector.VpcId");
                var subnetsResourcesEmpty = await restClient.GetConfigSettingResourcesAsync(sessionId, "VPCConnector.Subnets");
                var securityGroupsResourcesEmpty = await restClient.GetConfigSettingResourcesAsync(sessionId, "VPCConnector.SecurityGroups");
                Assert.NotEmpty(vpcResources.Resources);
                Assert.Empty(subnetsResourcesEmpty.Resources);
                Assert.Empty(securityGroupsResourcesEmpty.Resources);

                var vpcId = vpcResources.Resources.First().SystemName;
                await restClient.ApplyConfigSettingsAsync(sessionId, new ApplyConfigSettingsInput()
                {
                    UpdatedSettings = new Dictionary<string, string>()
                    {
                        {"VPCConnector.CreateNew", "true"},
                        {"VPCConnector.VpcId", vpcId}
                    }
                });

                var subnetsResources = await restClient.GetConfigSettingResourcesAsync(sessionId, "VPCConnector.Subnets");
                var securityGroupsResources = await restClient.GetConfigSettingResourcesAsync(sessionId, "VPCConnector.SecurityGroups");
                Assert.NotEmpty(subnetsResources.Resources);
                Assert.NotEmpty(securityGroupsResources.Resources);

                var subnet = subnetsResources.Resources.Last().SystemName;
                var securityGroup = securityGroupsResources.Resources.First().SystemName;
                var setConfigResult = await restClient.ApplyConfigSettingsAsync(sessionId, new ApplyConfigSettingsInput()
                {
                    UpdatedSettings = new Dictionary<string, string>()
                    {
                        {"VPCConnector.Subnets", JsonConvert.SerializeObject(new List<string>{subnet})},
                        {"VPCConnector.SecurityGroups", JsonConvert.SerializeObject(new List<string>{securityGroup})}
                    }
                });

                var generateCloudFormationTemplateResponse = await restClient.GenerateCloudFormationTemplateAsync(sessionId);

                var metadata = await GetAppSettingsFromCFTemplate(generateCloudFormationTemplateResponse.CloudFormationTemplate, _stackName);

                Assert.True(metadata.Settings.ContainsKey("VPCConnector"));
                var vpcConnector = JsonConvert.DeserializeObject<VPCConnectorTypeHintResponse>(metadata.Settings["VPCConnector"].ToString());
                Assert.True(vpcConnector.CreateNew);
                Assert.Equal(vpcId, vpcConnector.VpcId);
                Assert.Contains<string>(subnet, vpcConnector.Subnets);
                Assert.Contains<string>(securityGroup, vpcConnector.SecurityGroups);
            }
            finally
            {
                cancelSource.Cancel();
                _stackName = null;
            }
        }

        [Fact]
        public async Task GetAppRunnerConfigSettings_TypeHintData()
        {
            _stackName = $"ServerModeWebAppRunner{Guid.NewGuid().ToString().Split('-').Last()}";

            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppWithDockerFile", "WebAppWithDockerFile.csproj"));
            var portNumber = 4002;
            using var httpClient = ServerModeHttpClientFactory.ConstructHttpClient(ResolveCredentials);

            var serverCommand = new ServerModeCommand(_serviceProvider.GetRequiredService<IToolInteractiveService>(), portNumber, null, true);
            var cancelSource = new CancellationTokenSource();

            var serverTask = serverCommand.ExecuteAsync(cancelSource.Token);
            try
            {
                var baseUrl = $"http://localhost:{portNumber}/";
                var restClient = new RestAPIClient(baseUrl, httpClient);

                await WaitTillServerModeReady(restClient);

                var sessionId = await StartDeploymentSession(restClient, projectPath);

                var logOutput = new StringBuilder();
                await SetupSignalRConnection(baseUrl, sessionId, logOutput);

                await GetRecommendationsAndSelectAppRunner(restClient, sessionId);

                var configSettings = restClient.GetConfigSettingsAsync(sessionId);
                Assert.NotEmpty(configSettings.Result.OptionSettings);
                var iamRoleSetting = Assert.Single(configSettings.Result.OptionSettings, o => o.Id == "ApplicationIAMRole");
                Assert.NotEmpty(iamRoleSetting.TypeHintData);
                Assert.Equal("tasks.apprunner.amazonaws.com", iamRoleSetting.TypeHintData[nameof(IAMRoleTypeHintData.ServicePrincipal)]);
            }
            finally
            {
                cancelSource.Cancel();
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

        private async Task<CloudApplicationMetadata> GetAppSettingsFromCFTemplate(string cloudFormationTemplate, string stackName)
        {
            var templateMetadataReader = GetTemplateMetadataReader(cloudFormationTemplate);
            return await templateMetadataReader.LoadCloudApplicationMetadata(stackName);
        }

        private async Task<string> StartDeploymentSession(RestAPIClient restClient, string projectPath)
        {
            var startSessionOutput = await restClient.StartDeploymentSessionAsync(new StartDeploymentSessionInput
            {
                AwsRegion = _awsRegion,
                ProjectPath = projectPath
            });

            var sessionId = startSessionOutput.SessionId;
            Assert.NotNull(sessionId);
            return sessionId;
        }

        private static async Task SetupSignalRConnection(string baseUrl, string sessionId, StringBuilder logOutput)
        {
            var signalRClient = new DeploymentCommunicationClient(baseUrl);
            await signalRClient.JoinSession(sessionId);

            AWS.Deploy.CLI.IntegrationTests.ServerModeTests.RegisterSignalRMessageCallbacks(signalRClient, logOutput);
        }

        private async Task<RecommendationSummary> GetRecommendationsAndSelectAppRunner(RestAPIClient restClient, string sessionId)
        {
            var getRecommendationOutput = await restClient.GetRecommendationsAsync(sessionId);
            Assert.NotEmpty(getRecommendationOutput.Recommendations);

            var appRunnerRecommendation =
                getRecommendationOutput.Recommendations.FirstOrDefault(x => string.Equals(x.RecipeId, "AspNetAppAppRunner"));
            Assert.NotNull(appRunnerRecommendation);

            await restClient.SetDeploymentTargetAsync(sessionId,
                new SetDeploymentTargetInput
                {
                    NewDeploymentName = _stackName,
                    NewDeploymentRecipeId = appRunnerRecommendation.RecipeId
                });
            return appRunnerRecommendation;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            _isDisposed = true;
        }

        ~GetApplyOptionSettings()
        {
            Dispose(false);
        }
    }
}
