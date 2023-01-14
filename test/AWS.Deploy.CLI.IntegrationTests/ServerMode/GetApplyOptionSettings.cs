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
using AWS.Deploy.CLI.IntegrationTests.Utilities;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Common.IO;
using Newtonsoft.Json.Linq;
using AWS.Deploy.ServerMode.Client.Utilities;
using NUnit.Framework;

namespace AWS.Deploy.CLI.IntegrationTests.ServerMode
{
    [TestFixture]
    public class GetApplyOptionSettings
    {
        private IServiceProvider _serviceProvider;

        private string _awsRegion;
        private TestAppManager _testAppManager;

        private Mock<IAWSClientFactory> _mockAWSClientFactory;
        private Mock<IAmazonCloudFormation> _mockCFClient;
        private Mock<IDeployToolWorkspaceMetadata> _deployToolWorkspaceMetadata;
        private IFileManager _fileManager;

        [SetUp]
        public void Initialize()
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

        [Test]
        public async Task GetAndApplyAppRunnerSettings_RecipeValidatorsAreRun()
        {
            var stackName = $"ServerModeWebAppRunner{Guid.NewGuid().ToString().Split('-').Last()}";

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

                var fargateRecommendation = await restClient.GetRecommendationsAndSetDeploymentTarget(sessionId, "AspNetAppEcsFargate", stackName);

                var applyConfigSettingsResponse = await restClient.ApplyConfigSettingsAsync(sessionId, new ApplyConfigSettingsInput()
                {
                    UpdatedSettings = new Dictionary<string, string>()
                    {
                        {"TaskCpu", "4096"}
                    }
                });
                Assert.IsEmpty(applyConfigSettingsResponse.FailedConfigUpdates);

                var exceptionThrown = Assert.ThrowsAsync<ApiException>(async () => await restClient.StartDeploymentAsync(sessionId));
                StringAssert.Contains("Cpu value 4096 is not compatible with memory value 512.", exceptionThrown.Response);
            }
            finally
            {
                cancelSource.Cancel();
            }
        }

        [Test]
        public async Task GetAndApplyAppRunnerSettings_FailedUpdatesReturnSettingId()
        {
            var stackName = $"ServerModeWebAppRunner{Guid.NewGuid().ToString().Split('-').Last()}";

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

                var fargateRecommendation = await restClient.GetRecommendationsAndSetDeploymentTarget(sessionId, "AspNetAppEcsFargate", stackName);

                var applyConfigSettingsResponse = await restClient.ApplyConfigSettingsAsync(sessionId, new ApplyConfigSettingsInput()
                {
                    UpdatedSettings = new Dictionary<string, string>()
                    {
                        {"DesiredCount", "test"}
                    }
                });
                Assert.AreEqual(1, applyConfigSettingsResponse.FailedConfigUpdates.Count);
                Assert.AreEqual("DesiredCount", applyConfigSettingsResponse.FailedConfigUpdates.Keys.First());
            }
            finally
            {
                cancelSource.Cancel();
            }
        }

        [Test]
        public async Task GetAndApplyAppRunnerSettings_VPCConnector()
        {
            var stackName = $"ServerModeWebAppRunner{Guid.NewGuid().ToString().Split('-').Last()}";

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

                var appRunnerRecommendation = await restClient.GetRecommendationsAndSetDeploymentTarget(sessionId, "AspNetAppAppRunner", stackName);

                var vpcResources = await restClient.GetConfigSettingResourcesAsync(sessionId, "VPCConnector.VpcId");
                var subnetsResourcesEmpty = await restClient.GetConfigSettingResourcesAsync(sessionId, "VPCConnector.Subnets");
                var securityGroupsResourcesEmpty = await restClient.GetConfigSettingResourcesAsync(sessionId, "VPCConnector.SecurityGroups");
                Assert.IsNotEmpty(vpcResources.Resources);
                Assert.IsNotEmpty(subnetsResourcesEmpty.Resources);
                Assert.IsNotEmpty(securityGroupsResourcesEmpty.Resources);

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
                Assert.IsNotEmpty(subnetsResources.Resources);
                Assert.IsNotEmpty(securityGroupsResources.Resources);

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

                var metadata = await ServerModeExtensions.GetAppSettingsFromCFTemplate(_mockAWSClientFactory, _mockCFClient, generateCloudFormationTemplateResponse.CloudFormationTemplate, stackName, _deployToolWorkspaceMetadata, _fileManager);

                Assert.True(metadata.Settings.ContainsKey("VPCConnector"));
                var vpcConnector = JsonConvert.DeserializeObject<VPCConnectorTypeHintResponse>(metadata.Settings["VPCConnector"].ToString());
                Assert.True(vpcConnector.UseVPCConnector);
                Assert.True(vpcConnector.CreateNew);
                Assert.AreEqual(vpcId, vpcConnector.VpcId);
                CollectionAssert.Contains(vpcConnector.Subnets, subnet);
                CollectionAssert.Contains(vpcConnector.SecurityGroups, securityGroup);
            }
            finally
            {
                cancelSource.Cancel();
            }
        }

        [Test]
        public async Task GetAppRunnerConfigSettings_TypeHintData()
        {
            var stackName = $"ServerModeWebAppRunner{Guid.NewGuid().ToString().Split('-').Last()}";

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

                await restClient.GetRecommendationsAndSetDeploymentTarget(sessionId, "AspNetAppAppRunner", stackName);

                var configSettings = restClient.GetConfigSettingsAsync(sessionId);
                Assert.IsNotEmpty(configSettings.Result.OptionSettings);
                var iamRoleSetting = configSettings.Result.OptionSettings.FirstOrDefault(o => o.Id == "ApplicationIAMRole");
                Assert.NotNull(iamRoleSetting);
                Assert.IsNotEmpty(iamRoleSetting.TypeHintData);
                Assert.AreEqual("tasks.apprunner.amazonaws.com", iamRoleSetting.TypeHintData[nameof(IAMRoleTypeHintData.ServicePrincipal)]);
            }
            finally
            {
                cancelSource.Cancel();
            }
        }

        /// <summary>
        /// Tests that GetConfigSettingResourcesAsync for App Runner's
        /// VPC Connector child settings return TypeHintResourceColumns
        /// </summary>
        [Test]
        public async Task GetConfigSettingResources_VpcConnectorOptions()
        {
            var stackName = $"ServerModeWebAppRunner{Guid.NewGuid().ToString().Split('-').Last()}";

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

                await restClient.GetRecommendationsAndSetDeploymentTarget(sessionId, "AspNetAppAppRunner", stackName);

                // Assert that the Subnets and SecurityGroups options are returning columns 
                var subnets = await restClient.GetConfigSettingResourcesAsync(sessionId, "VPCConnector.Subnets");
                Assert.That(subnets.Columns, Has.Exactly(3).Items); // Subnet Id, VPC, Availability Zone
                Assert.That(subnets.Columns, Is.All.Not.Null);

                var securityGroups = await restClient.GetConfigSettingResourcesAsync(sessionId, "VPCConnector.SecurityGroups");
                Assert.That(securityGroups.Columns, Has.Exactly(3).Items); // Name, Id, VPC
                Assert.That(securityGroups.Columns, Is.All.Not.Null);

                // This is using a real AWSResourceQueryer,
                // so not asserting on the rows for these two options
            }
            finally
            {
                cancelSource.Cancel();
            }
        }

        /// <summary>
        /// Tests that the LoadBalancer.InternetFacing option setting correctly toggles the
        /// <see href="https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/aws-properties-ec2-elb.html#cfn-ec2-elb-scheme">load balancer scheme</see>.
        /// </summary>
        /// <param name="internetFacingValue">desired LoadBalancer.InternetFacing option setting value</param>
        /// <param name="expectedLoadBalancerScheme">Expected load balancer scheme in the generated CloudFormation template</param>
        [Test]
        [TestCase("true", "internet-facing", 4024)]
        [TestCase("false", "internal", 4025)]
        public async Task GetAndApplyECSFargateSettings_LoadBalancerSchemeConfig(string internetFacingValue, string expectedLoadBalancerScheme, int portNumber)
        {
            var stackName = $"ServerModeWebECSFargate{Guid.NewGuid().ToString().Split('-').Last()}";

            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppWithDockerFile", "WebAppWithDockerFile.csproj"));
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

                var recommendation = await restClient.GetRecommendationsAndSetDeploymentTarget(sessionId, "AspNetAppEcsFargate", stackName);

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

                Assert.AreEqual(expectedLoadBalancerScheme, loadBalancerSchemeValue.ToString());
            }
            finally
            {
                cancelSource.Cancel();
            }
        }
    }
}
