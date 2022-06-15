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
using AWS.Deploy.CLI.Commands;
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.CLI.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Extensions;
using AWS.Deploy.CLI.TypeHintResponses;
using AWS.Deploy.Common;
using AWS.Deploy.ServerMode.Client;
using AWS.Deploy.Common.TypeHintData;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using Xunit;
using AWS.Deploy.CLI.IntegrationTests.Utilities;

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

        [Fact]
        public async Task GetAndApplyAppRunnerSettings_RecipeValidatorsAreRun()
        {
            _stackName = $"ServerModeWebAppRunner{Guid.NewGuid().ToString().Split('-').Last()}";

            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppWithDockerFile", "WebAppWithDockerFile.csproj"));
            var portNumber = 4026;
            using var httpClient = ServerModeHttpClientFactory.ConstructHttpClient(ServerModeExtensions.ResolveCredentials);

            var serverCommand = new ServerModeCommand(_serviceProvider.GetRequiredService<IToolInteractiveService>(), portNumber, null, true);
            var cancelSource = new CancellationTokenSource();

            var serverTask = serverCommand.ExecuteAsync(cancelSource.Token);
            try
            {
                var baseUrl = $"http://localhost:{portNumber}/";
                var restClient = new RestAPIClient(baseUrl, httpClient);

                await restClient.WaitTillServerModeReady();

                var sessionId = await restClient.StartDeploymentSession(projectPath, _awsRegion);

                var logOutput = new StringBuilder();
                await ServerModeExtensions.SetupSignalRConnection(baseUrl, sessionId, logOutput);

                var fargateRecommendation = await restClient.GetRecommendationsAndSetDeploymentTarget(sessionId, "AspNetAppEcsFargate", _stackName);

                var applyConfigSettingsResponse = await restClient.ApplyConfigSettingsAsync(sessionId, new ApplyConfigSettingsInput()
                {
                    UpdatedSettings = new Dictionary<string, string>()
                    {
                        {"TaskCpu", "4096"}
                    }
                });
                Assert.Empty(applyConfigSettingsResponse.FailedConfigUpdates);

                var exceptionThrown = await Assert.ThrowsAsync<ApiException>(async () => await restClient.StartDeploymentAsync(sessionId));
                Assert.Contains("Cpu value 4096 is not compatible with memory value 512.", exceptionThrown.Response);
            }
            finally
            {
                cancelSource.Cancel();
                _stackName = null;
            }
        }

        [Fact]
        public async Task GetAndApplyAppRunnerSettings_FailedUpdatesReturnSettingId()
        {
            _stackName = $"ServerModeWebAppRunner{Guid.NewGuid().ToString().Split('-').Last()}";

            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppWithDockerFile", "WebAppWithDockerFile.csproj"));
            var portNumber = 4027;
            using var httpClient = ServerModeHttpClientFactory.ConstructHttpClient(ServerModeExtensions.ResolveCredentials);

            var serverCommand = new ServerModeCommand(_serviceProvider.GetRequiredService<IToolInteractiveService>(), portNumber, null, true);
            var cancelSource = new CancellationTokenSource();

            var serverTask = serverCommand.ExecuteAsync(cancelSource.Token);
            try
            {
                var baseUrl = $"http://localhost:{portNumber}/";
                var restClient = new RestAPIClient(baseUrl, httpClient);

                await restClient.WaitTillServerModeReady();

                var sessionId = await restClient.StartDeploymentSession(projectPath, _awsRegion);

                var logOutput = new StringBuilder();
                await ServerModeExtensions.SetupSignalRConnection(baseUrl, sessionId, logOutput);

                var fargateRecommendation = await restClient.GetRecommendationsAndSetDeploymentTarget(sessionId, "AspNetAppEcsFargate", _stackName);

                var applyConfigSettingsResponse = await restClient.ApplyConfigSettingsAsync(sessionId, new ApplyConfigSettingsInput()
                {
                    UpdatedSettings = new Dictionary<string, string>()
                    {
                        {"DesiredCount", "test"}
                    }
                });
                Assert.Single(applyConfigSettingsResponse.FailedConfigUpdates);
                Assert.Equal("DesiredCount", applyConfigSettingsResponse.FailedConfigUpdates.Keys.First());
            }
            finally
            {
                cancelSource.Cancel();
                _stackName = null;
            }
        }

        [Fact]
        public async Task GetAndApplyAppRunnerSettings_VPCConnector()
        {
            _stackName = $"ServerModeWebAppRunner{Guid.NewGuid().ToString().Split('-').Last()}";

            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppWithDockerFile", "WebAppWithDockerFile.csproj"));
            var portNumber = 4021;
            using var httpClient = ServerModeHttpClientFactory.ConstructHttpClient(ServerModeExtensions.ResolveCredentials);

            var serverCommand = new ServerModeCommand(_serviceProvider.GetRequiredService<IToolInteractiveService>(), portNumber, null, true);
            var cancelSource = new CancellationTokenSource();

            var serverTask = serverCommand.ExecuteAsync(cancelSource.Token);
            try
            {
                var baseUrl = $"http://localhost:{portNumber}/";
                var restClient = new RestAPIClient(baseUrl, httpClient);

                await restClient.WaitTillServerModeReady();

                var sessionId = await restClient.StartDeploymentSession(projectPath, _awsRegion);

                var logOutput = new StringBuilder();
                await ServerModeExtensions.SetupSignalRConnection(baseUrl, sessionId, logOutput);

                var appRunnerRecommendation = await restClient.GetRecommendationsAndSetDeploymentTarget(sessionId, "AspNetAppAppRunner", _stackName);

                var vpcResources = await restClient.GetConfigSettingResourcesAsync(sessionId, "VPCConnector.VpcId");
                var subnetsResourcesEmpty = await restClient.GetConfigSettingResourcesAsync(sessionId, "VPCConnector.Subnets");
                var securityGroupsResourcesEmpty = await restClient.GetConfigSettingResourcesAsync(sessionId, "VPCConnector.SecurityGroups");
                Assert.NotEmpty(vpcResources.Resources);
                Assert.NotEmpty(subnetsResourcesEmpty.Resources);
                Assert.NotEmpty(securityGroupsResourcesEmpty.Resources);

                var vpcId = vpcResources.Resources.First().SystemName;
                await restClient.ApplyConfigSettingsAsync(sessionId, new ApplyConfigSettingsInput()
                {
                    UpdatedSettings = new Dictionary<string, string>()
                    {
                        {"VPCConnector.UseVPCConnector", "true"},
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

                var metadata = await ServerModeExtensions.GetAppSettingsFromCFTemplate(_mockAWSClientFactory, _mockCFClient, generateCloudFormationTemplateResponse.CloudFormationTemplate, _stackName);

                Assert.True(metadata.Settings.ContainsKey("VPCConnector"));
                var vpcConnector = JsonConvert.DeserializeObject<VPCConnectorTypeHintResponse>(metadata.Settings["VPCConnector"].ToString());
                Assert.True(vpcConnector.UseVPCConnector);
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
            using var httpClient = ServerModeHttpClientFactory.ConstructHttpClient(ServerModeExtensions.ResolveCredentials);

            var serverCommand = new ServerModeCommand(_serviceProvider.GetRequiredService<IToolInteractiveService>(), portNumber, null, true);
            var cancelSource = new CancellationTokenSource();

            var serverTask = serverCommand.ExecuteAsync(cancelSource.Token);
            try
            {
                var baseUrl = $"http://localhost:{portNumber}/";
                var restClient = new RestAPIClient(baseUrl, httpClient);

                await restClient.WaitTillServerModeReady();

                var sessionId = await restClient.StartDeploymentSession(projectPath, _awsRegion);

                var logOutput = new StringBuilder();
                await ServerModeExtensions.SetupSignalRConnection(baseUrl, sessionId, logOutput);

                await restClient.GetRecommendationsAndSetDeploymentTarget(sessionId, "AspNetAppAppRunner", _stackName);

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
