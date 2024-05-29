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
using AWS.Deploy.Orchestration;
using AWS.Deploy.Common.IO;
using Newtonsoft.Json.Linq;
using AWS.Deploy.ServerMode.Client.Utilities;

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
        private readonly Mock<IDeployToolWorkspaceMetadata> _deployToolWorkspaceMetadata;
        private readonly IFileManager _fileManager;

        public GetApplyOptionSettings()
        {
            _mockAWSClientFactory = new Mock<IAWSClientFactory>();
            _mockCFClient = new Mock<IAmazonCloudFormation>();
            _deployToolWorkspaceMetadata = new Mock<IDeployToolWorkspaceMetadata>();
            _fileManager = new TestFileManager();

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
            using var httpClient = ServerModeHttpClientFactory.ConstructHttpClient(ServerModeUtilities.ResolveDefaultCredentials);

            var serverCommand = new ServerModeCommand(_serviceProvider.GetRequiredService<IToolInteractiveService>(), portNumber, null, true);
            var cancelSource = new CancellationTokenSource();

            var serverTask = serverCommand.ExecuteAsync(cancelSource.Token);
            try
            {
                var baseUrl = $"http://localhost:{portNumber}/";
                var restClient = new RestAPIClient(baseUrl, httpClient);

                await restClient.WaitUntilServerModeReady();

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
            using var httpClient = ServerModeHttpClientFactory.ConstructHttpClient(ServerModeUtilities.ResolveDefaultCredentials);

            var serverCommand = new ServerModeCommand(_serviceProvider.GetRequiredService<IToolInteractiveService>(), portNumber, null, true);
            var cancelSource = new CancellationTokenSource();

            var serverTask = serverCommand.ExecuteAsync(cancelSource.Token);
            try
            {
                var baseUrl = $"http://localhost:{portNumber}/";
                var restClient = new RestAPIClient(baseUrl, httpClient);

                await restClient.WaitUntilServerModeReady();

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
        public async Task ContainerPortSettings_PortWarning()
        {
            _stackName = $"ServerModeWebAppRunner{Guid.NewGuid().ToString().Split('-').Last()}";

            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppNet8WithCustomDockerFile", "WebAppNet8WithCustomDockerFile.csproj"));
            var portNumber = 4025;
            using var httpClient = ServerModeHttpClientFactory.ConstructHttpClient(ServerModeUtilities.ResolveDefaultCredentials);

            var serverCommand = new ServerModeCommand(_serviceProvider.GetRequiredService<IToolInteractiveService>(), portNumber, null, true);
            var cancelSource = new CancellationTokenSource();

            var serverTask = serverCommand.ExecuteAsync(cancelSource.Token);
            try
            {
                var baseUrl = $"http://localhost:{portNumber}/";
                var restClient = new RestAPIClient(baseUrl, httpClient);

                await restClient.WaitUntilServerModeReady();

                var sessionId = await restClient.StartDeploymentSession(projectPath, _awsRegion);

                var logOutput = new StringBuilder();
                await ServerModeExtensions.SetupSignalRConnection(baseUrl, sessionId, logOutput);

                var fargateRecommendation = await restClient.GetRecommendationsAndSetDeploymentTarget(sessionId, "AspNetAppEcsFargate", _stackName);

                var generateCloudFormationTemplateResponse = await restClient.GenerateCloudFormationTemplateAsync(sessionId);

                Assert.Contains("The HTTP port you have chosen in your deployment settings is different than the .NET HTTP port exposed in the container.", logOutput.ToString());
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
            using var httpClient = ServerModeHttpClientFactory.ConstructHttpClient(ServerModeUtilities.ResolveDefaultCredentials);

            var serverCommand = new ServerModeCommand(_serviceProvider.GetRequiredService<IToolInteractiveService>(), portNumber, null, true);
            var cancelSource = new CancellationTokenSource();

            var serverTask = serverCommand.ExecuteAsync(cancelSource.Token);
            try
            {
                var baseUrl = $"http://localhost:{portNumber}/";
                var restClient = new RestAPIClient(baseUrl, httpClient);

                await restClient.WaitUntilServerModeReady();

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

                var metadata = await ServerModeExtensions.GetAppSettingsFromCFTemplate(_mockAWSClientFactory, _mockCFClient, generateCloudFormationTemplateResponse.CloudFormationTemplate, _stackName, _deployToolWorkspaceMetadata, _fileManager);

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
            using var httpClient = ServerModeHttpClientFactory.ConstructHttpClient(ServerModeUtilities.ResolveDefaultCredentials);

            var serverCommand = new ServerModeCommand(_serviceProvider.GetRequiredService<IToolInteractiveService>(), portNumber, null, true);
            var cancelSource = new CancellationTokenSource();

            var serverTask = serverCommand.ExecuteAsync(cancelSource.Token);
            try
            {
                var baseUrl = $"http://localhost:{portNumber}/";
                var restClient = new RestAPIClient(baseUrl, httpClient);

                await restClient.WaitUntilServerModeReady();

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

        /// <summary>
        /// Tests that GetConfigSettingResourcesAsync for App Runner's
        /// VPC Connector child settings return TypeHintResourceColumns
        /// </summary>
        [Fact]
        public async Task GetConfigSettingResources_VpcConnectorOptions()
        {
            _stackName = $"ServerModeWebAppRunner{Guid.NewGuid().ToString().Split('-').Last()}";

            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppWithDockerFile", "WebAppWithDockerFile.csproj"));
            var portNumber = 4023;
            using var httpClient = ServerModeHttpClientFactory.ConstructHttpClient(ServerModeUtilities.ResolveDefaultCredentials);

            var serverCommand = new ServerModeCommand(_serviceProvider.GetRequiredService<IToolInteractiveService>(), portNumber, null, true);
            var cancelSource = new CancellationTokenSource();

            var serverTask = serverCommand.ExecuteAsync(cancelSource.Token);
            try
            {
                var baseUrl = $"http://localhost:{portNumber}/";
                var restClient = new RestAPIClient(baseUrl, httpClient);

                await restClient.WaitUntilServerModeReady();

                var sessionId = await restClient.StartDeploymentSession(projectPath, _awsRegion);

                await restClient.GetRecommendationsAndSetDeploymentTarget(sessionId, "AspNetAppAppRunner", _stackName);

                // Assert that the Subnets and SecurityGroups options are returning columns 
                var subnets = await restClient.GetConfigSettingResourcesAsync(sessionId, "VPCConnector.Subnets");
                Assert.Collection(subnets.Columns,
                    column => Assert.NotNull(column),   // Subnet Id
                    column => Assert.NotNull(column),   // VPC
                    column => Assert.NotNull(column));  // Availability Zone

                var securityGroups = await restClient.GetConfigSettingResourcesAsync(sessionId, "VPCConnector.SecurityGroups");
                Assert.Collection(securityGroups.Columns,
                    column => Assert.NotNull(column),   // Name
                    column => Assert.NotNull(column),   // Id
                    column => Assert.NotNull(column));  // VPC

                // This is using a real AWSResourceQueryer,
                // so not asserting on the rows for these two options
            }
            finally
            {
                cancelSource.Cancel();
                _stackName = null;
            }
        }

        /// <summary>
        /// Tests that the LoadBalancer.InternetFacing option setting correctly toggles the
        /// <see href="https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/aws-properties-ec2-elb.html#cfn-ec2-elb-scheme">load balancer scheme</see>.
        /// </summary>
        /// <param name="internetFacingValue">desired LoadBalancer.InternetFacing option setting value</param>
        /// <param name="expectedLoadBalancerScheme">Expected load balancer scheme in the generated CloudFormation template</param>
        [Theory]
        [InlineData("true", "internet-facing")]
        [InlineData("false", "internal")]
        public async Task GetAndApplyECSFargateSettings_LoadBalancerSchemeConfig(string internetFacingValue, string expectedLoadBalancerScheme)
        {
            _stackName = $"ServerModeWebECSFargate{Guid.NewGuid().ToString().Split('-').Last()}";

            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppWithDockerFile", "WebAppWithDockerFile.csproj"));
            var portNumber = 4024;
            using var httpClient = ServerModeHttpClientFactory.ConstructHttpClient(ServerModeUtilities.ResolveDefaultCredentials);

            // Running `cdk diff` to assert against the generated CloudFormation template
            // for this recipe takes longer than the default timeout
            httpClient.Timeout = new TimeSpan(0, 0, 120);

            var serverCommand = new ServerModeCommand(_serviceProvider.GetRequiredService<IToolInteractiveService>(), portNumber, null, true);
            var cancelSource = new CancellationTokenSource();

            var serverTask = serverCommand.ExecuteAsync(cancelSource.Token);
            try
            {
                var baseUrl = $"http://localhost:{portNumber}/";
                var restClient = new RestAPIClient(baseUrl, httpClient);

                await restClient.WaitUntilServerModeReady();

                var sessionId = await restClient.StartDeploymentSession(projectPath, _awsRegion);

                var logOutput = new StringBuilder();
                await ServerModeExtensions.SetupSignalRConnection(baseUrl, sessionId, logOutput);

                var recommendation = await restClient.GetRecommendationsAndSetDeploymentTarget(sessionId, "AspNetAppEcsFargate", _stackName);

                var response = await restClient.ApplyConfigSettingsAsync(sessionId, new ApplyConfigSettingsInput()
                {
                    UpdatedSettings = new Dictionary<string, string>()
                    {
                        {"LoadBalancer.InternetFacing", internetFacingValue}
                    }
                });

                var generateCloudFormationTemplateResponse = await restClient.GenerateCloudFormationTemplateAsync(sessionId);
                var cloudFormationTemplate = JObject.Parse(generateCloudFormationTemplateResponse.CloudFormationTemplate);

                // This should find the AWS::ElasticLoadBalancingV2::LoadBalancer resource in the CloudFormation JSON
                // based on its "Scheme" property, which is what "LoadBalancer.InternetFacing" ultimately drives.
                // If multiple resources end up with a Scheme property or the LoadBalancer is missing,
                // this test should fail because .Single() will throw an exception.
                var loadBalancerSchemeValue = cloudFormationTemplate.SelectTokens("Resources.*.Properties.Scheme").Single();

                Assert.Equal(expectedLoadBalancerScheme, loadBalancerSchemeValue.ToString());
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
