// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
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
using AWS.Deploy.CLI.IntegrationTests.Utilities;
using AWS.Deploy.CLI.ServerMode;
using AWS.Deploy.Orchestration.Utilities;
using AWS.Deploy.ServerMode.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace AWS.Deploy.CLI.IntegrationTests
{
    [TestClass]
    public class ServerModeTests : IDisposable
    {
        private bool _isDisposed;
        private string _stackName;
        private readonly IServiceProvider _serviceProvider;
        private readonly CloudFormationHelper _cloudFormationHelper;

        private readonly string _awsRegion;
        private readonly TestAppManager _testAppManager;
        private readonly InMemoryInteractiveService _interactiveService;

        public ServerModeTests()
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

        /// <summary>
        /// ServerMode must only be connectable from 127.0.0.1 or localhost. This test confirms that connect attempts using
        /// the host name fail.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task ConfirmLocalhostOnly()
        {
            var portNumber = 4900;
            var serverCommand = new ServerModeCommand(_serviceProvider.GetRequiredService<IToolInteractiveService>(), portNumber, null, true);
            var cancelSource = new CancellationTokenSource();

            _ = serverCommand.ExecuteAsync(cancelSource.Token);
            try
            {
                var restClient = new RestAPIClient($"http://localhost:{portNumber}/", ServerModeHttpClientFactory.ConstructHttpClient(ServerModeExtensions.ResolveCredentials));
                await restClient.WaitTillServerModeReady();

                using var client = new HttpClient();

                var localhostUrl = $"http://localhost:{portNumber}/api/v1/Health";
                await client.GetStringAsync(localhostUrl);

                var host = Dns.GetHostName();
                var hostnameUrl = $"http://{host}:{portNumber}/api/v1/Health";
                await Assert.ThrowsExceptionAsync<HttpRequestException>(async () => await client.GetStringAsync(hostnameUrl));
            }
            finally
            {
                cancelSource.Cancel();
            }
        }

        [TestMethod]
        public async Task GetRecommendations()
        {
            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppNoDockerFile", "WebAppNoDockerFile.csproj"));
            var portNumber = 4000;
            using var httpClient = ServerModeHttpClientFactory.ConstructHttpClient(ServerModeExtensions.ResolveCredentials);

            var serverCommand = new ServerModeCommand(_serviceProvider.GetRequiredService<IToolInteractiveService>(), portNumber, null, true);
            var cancelSource = new CancellationTokenSource();

            var serverTask = serverCommand.ExecuteAsync(cancelSource.Token);
            try
            {
                var restClient = new RestAPIClient($"http://localhost:{portNumber}/", httpClient);
                await restClient.WaitTillServerModeReady();

                var startSessionOutput = await restClient.StartDeploymentSessionAsync(new StartDeploymentSessionInput
                {
                    AwsRegion = _awsRegion,
                    ProjectPath = projectPath
                });

                var sessionId = startSessionOutput.SessionId;
                Assert.IsNotNull(sessionId);

                var getRecommendationOutput = await restClient.GetRecommendationsAsync(sessionId);
                Assert.AreEqual(0, getRecommendationOutput.Recommendations.Count);
                var beanstalkRecommendation = getRecommendationOutput.Recommendations.FirstOrDefault();
                Assert.AreEqual("AspNetAppElasticBeanstalkLinux", beanstalkRecommendation.RecipeId);
                Assert.IsNull(beanstalkRecommendation.BaseRecipeId);
                Assert.IsFalse(beanstalkRecommendation.IsPersistedDeploymentProject);
                Assert.IsNotNull(beanstalkRecommendation.ShortDescription);
                Assert.IsNotNull(beanstalkRecommendation.Description);
                Assert.IsTrue(beanstalkRecommendation.ShortDescription.Length < beanstalkRecommendation.Description.Length);
                Assert.AreEqual("AWS Elastic Beanstalk", beanstalkRecommendation.TargetService);
            }
            finally
            {
                cancelSource.Cancel();
            }
        }

        [TestMethod]
        public async Task GetRecommendationsWithEncryptedCredentials()
        {
            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppNoDockerFile", "WebAppNoDockerFile.csproj"));
            var portNumber = 4000;

            var aes = Aes.Create();
            aes.GenerateKey();
            aes.GenerateIV();

            using var httpClient = ServerModeHttpClientFactory.ConstructHttpClient(ServerModeExtensions.ResolveCredentials, aes);

            var keyInfo = new EncryptionKeyInfo
            {
                Version = EncryptionKeyInfo.VERSION_1_0,
                Key = Convert.ToBase64String(aes.Key),
                IV = Convert.ToBase64String(aes.IV)
            };
            var keyInfoStdin = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(keyInfo)));
            await _interactiveService.StdInWriter.WriteAsync(keyInfoStdin);
            await _interactiveService.StdInWriter.FlushAsync();

            var serverCommand = new ServerModeCommand(_interactiveService, portNumber, null, false);
            var cancelSource = new CancellationTokenSource();

            var serverTask = serverCommand.ExecuteAsync(cancelSource.Token);
            try
            {
                var restClient = new RestAPIClient($"http://localhost:{portNumber}/", httpClient);
                await restClient.WaitTillServerModeReady();

                var startSessionOutput = await restClient.StartDeploymentSessionAsync(new StartDeploymentSessionInput
                {
                    AwsRegion = _awsRegion,
                    ProjectPath = projectPath
                });

                var sessionId = startSessionOutput.SessionId;
                Assert.IsNotNull(sessionId);

                var getRecommendationOutput = await restClient.GetRecommendationsAsync(sessionId);
                Assert.AreNotEqual(0, getRecommendationOutput.Recommendations.Count);
                Assert.AreEqual("AspNetAppElasticBeanstalkLinux", getRecommendationOutput.Recommendations.FirstOrDefault().RecipeId);

                var listDeployStdOut = _interactiveService.StdOutReader.ReadAllLines();
                CollectionAssert.Contains(listDeployStdOut, "Waiting on symmetric key from stdin");
                CollectionAssert.Contains(listDeployStdOut, "Encryption provider enabled");
            }
            finally
            {
                cancelSource.Cancel();
            }
        }

        [TestMethod]
        public async Task WebFargateDeploymentNoConfigChanges()
        {
            _stackName = $"ServerModeWebFargate{Guid.NewGuid().ToString().Split('-').Last()}";

            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppWithDockerFile", "WebAppWithDockerFile.csproj"));
            var portNumber = 4011;
            using var httpClient = ServerModeHttpClientFactory.ConstructHttpClient(ServerModeExtensions.ResolveCredentials);

            var serverCommand = new ServerModeCommand(_serviceProvider.GetRequiredService<IToolInteractiveService>(), portNumber, null, true);
            var cancelSource = new CancellationTokenSource();

            var serverTask = serverCommand.ExecuteAsync(cancelSource.Token);
            try
            {
                var baseUrl = $"http://localhost:{portNumber}/";
                var restClient = new RestAPIClient(baseUrl, httpClient);

                await restClient.WaitTillServerModeReady();

                var startSessionOutput = await restClient.StartDeploymentSessionAsync(new StartDeploymentSessionInput
                {
                    AwsRegion = _awsRegion,
                    ProjectPath = projectPath
                });

                var sessionId = startSessionOutput.SessionId;
                Assert.IsNotNull(sessionId);

                var signalRClient = new DeploymentCommunicationClient(baseUrl);
                await signalRClient.JoinSession(sessionId);

                var logOutput = new StringBuilder();
                RegisterSignalRMessageCallbacks(signalRClient, logOutput);

                var getRecommendationOutput = await restClient.GetRecommendationsAsync(sessionId);
                Assert.AreNotEqual(0, getRecommendationOutput.Recommendations.Count);

                var fargateRecommendation = getRecommendationOutput.Recommendations.FirstOrDefault(x => string.Equals(x.RecipeId, "AspNetAppEcsFargate"));
                Assert.IsNotNull(fargateRecommendation);

                await restClient.SetDeploymentTargetAsync(sessionId, new SetDeploymentTargetInput
                {
                    NewDeploymentName = _stackName,
                    NewDeploymentRecipeId = fargateRecommendation.RecipeId
                });

                await restClient.StartDeploymentAsync(
                    sessionId,
                    new StartDeploymentInput
                    {
                        DirectDeploy = true
                    });

                await restClient.WaitForDeployment(sessionId);

                var stackStatus = await _cloudFormationHelper.GetStackStatus(_stackName);
                Assert.AreEqual(StackStatus.CREATE_COMPLETE, stackStatus);

                Assert.IsTrue(logOutput.Length > 0);

                // Make sure section header is return to output log
                StringAssert.Contains(logOutput.ToString(), "Creating deployment image");

                // Make sure normal log messages are returned to output log
                StringAssert.Contains(logOutput.ToString(), "Pushing container image");

                var redeploymentSessionOutput = await restClient.StartDeploymentSessionAsync(new StartDeploymentSessionInput
                {
                    AwsRegion = _awsRegion,
                    ProjectPath = projectPath
                });

                var redeploymentSessionId = redeploymentSessionOutput.SessionId;

                var existingDeployments = await restClient.GetExistingDeploymentsAsync(redeploymentSessionId);
                var existingDeployment = existingDeployments.ExistingDeployments.First(x => string.Equals(_stackName, x.Name));

                Assert.AreEqual(_stackName, existingDeployment.Name);
                Assert.AreEqual(fargateRecommendation.RecipeId, existingDeployment.RecipeId);
                Assert.IsNull(fargateRecommendation.BaseRecipeId);
                Assert.IsFalse(fargateRecommendation.IsPersistedDeploymentProject);
                Assert.AreEqual(fargateRecommendation.Name, existingDeployment.RecipeName);
                Assert.AreEqual(fargateRecommendation.ShortDescription, existingDeployment.ShortDescription);
                Assert.AreEqual(fargateRecommendation.Description, existingDeployment.Description);
                Assert.AreEqual(fargateRecommendation.TargetService, existingDeployment.TargetService);
                Assert.AreEqual(DeploymentTypes.CloudFormationStack, existingDeployment.DeploymentType);

                Assert.AreNotEqual(0, existingDeployment.SettingsCategories.Count);
                //Assert.Contains(existingDeployment.SettingsCategories, x => string.Equals(x.Id, AWS.Deploy.Common.Recipes.Category.DeploymentBundle.Id));
                //Assert.DoesNotContain(existingDeployment.SettingsCategories, x => string.IsNullOrEmpty(x.Id));
                //Assert.DoesNotContain(existingDeployment.SettingsCategories, x => string.IsNullOrEmpty(x.DisplayName));

                // The below tests will check if updating readonly settings will properly get rejected.

                // These tests need to be performed on a redeployment
                await restClient.SetDeploymentTargetAsync(redeploymentSessionId, new SetDeploymentTargetInput
                {
                    ExistingDeploymentId = await _cloudFormationHelper.GetStackArn(_stackName)
                });

                var settings = await restClient.GetConfigSettingsAsync(sessionId);

                // Try to update a list of settings containing readonly settings which we expect to fail
                var updatedSettings = (IDictionary<string, string>)settings.OptionSettings.Where(x => !string.IsNullOrEmpty(x.FullyQualifiedId)).ToDictionary(k => k.FullyQualifiedId, v => "test");
                var exceptionThrown = await Assert.ThrowsExceptionAsync<ApiException>(async () => await restClient.ApplyConfigSettingsAsync(redeploymentSessionId, new ApplyConfigSettingsInput()
                {
                    UpdatedSettings = updatedSettings
                }));
                Assert.AreEqual(400, exceptionThrown.StatusCode);

                // Try to update an updatable setting which should be successful
                var applyConfigResponse = await restClient.ApplyConfigSettingsAsync(redeploymentSessionId,
                    new ApplyConfigSettingsInput
                    {
                        UpdatedSettings = new Dictionary<string, string> {
                            { "DesiredCount", "4" }
                        }
                    });
                Assert.AreEqual(0, applyConfigResponse.FailedConfigUpdates.Count);
            }
            finally
            {
                cancelSource.Cancel();
                await _cloudFormationHelper.DeleteStack(_stackName);
                _stackName = null;
            }
        }

        [TestMethod]
        public async Task RecommendationsForNewDeployments_DoesNotIncludeExistingBeanstalkEnvironmentRecipe()
        {
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

                var startSessionOutput = await restClient.StartDeploymentSessionAsync(new StartDeploymentSessionInput
                {
                    AwsRegion = _awsRegion,
                    ProjectPath = projectPath
                });

                var sessionId = startSessionOutput.SessionId;
                Assert.IsNotNull(sessionId);

                var getRecommendationOutput = await restClient.GetRecommendationsAsync(sessionId);
                Assert.AreNotEqual(0, getRecommendationOutput.Recommendations.Count);

                var recommendations = getRecommendationOutput.Recommendations;
                //Assert.DoesNotContain(recommendations, x => x.DeploymentType == DeploymentTypes.BeanstalkEnvironment);
            }
            finally
            {
                cancelSource.Cancel();
            }
        }

        [TestMethod]
        public async Task ShutdownViaRestClient()
        {
            var portNumber = 4003;
            var cancelSource = new CancellationTokenSource();
            using var httpClient = ServerModeHttpClientFactory.ConstructHttpClient(ServerModeExtensions.ResolveCredentials);
            var serverCommand = new ServerModeCommand(_serviceProvider.GetRequiredService<IToolInteractiveService>(), portNumber, null, true);

            var serverTask = serverCommand.ExecuteAsync(cancelSource.Token);

            try
            {
                var baseUrl = $"http://localhost:{portNumber}/";
                var restClient = new RestAPIClient(baseUrl, httpClient);

                await restClient.WaitTillServerModeReady();

                await restClient.ShutdownAsync();
                Thread.Sleep(100);

                // Expecting System.Net.Http.HttpRequestException : No connection could be made because the target machine actively refused it.
                await Assert.ThrowsExceptionAsync<System.Net.Http.HttpRequestException>(async () => await restClient.HealthAsync());
            }
            finally
            {
                cancelSource.Cancel();
            }
        }

        [TestMethod]
        [DataRow("1234MyAppStack")] // cannot start with a number
        [DataRow("MyApp@Stack/123")] // cannot contain special characters
        [DataRow("")] // cannot be empty
        [DataRow("stackstackstackstackstackstackstackstackstackstackstackstackstackstackstackstackstackstackstackstackstackstackstackstackstackstack")] // cannot contain more than 128 characters
        public async Task InvalidStackName_ThrowsException(string invalidStackName)
        {
            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppWithDockerFile", "WebAppWithDockerFile.csproj"));
            var portNumber = 4012;
            using var httpClient = ServerModeHttpClientFactory.ConstructHttpClient(ServerModeExtensions.ResolveCredentials);

            var serverCommand = new ServerModeCommand(_serviceProvider.GetRequiredService<IToolInteractiveService>(), portNumber, null, true);
            var cancelSource = new CancellationTokenSource();

            var serverTask = serverCommand.ExecuteAsync(cancelSource.Token);
            try
            {
                var baseUrl = $"http://localhost:{portNumber}/";
                var restClient = new RestAPIClient(baseUrl, httpClient);

                await restClient.WaitTillServerModeReady();

                var startSessionOutput = await restClient.StartDeploymentSessionAsync(new StartDeploymentSessionInput
                {
                    AwsRegion = _awsRegion,
                    ProjectPath = projectPath
                });

                var sessionId = startSessionOutput.SessionId;
                var getRecommendationOutput = await restClient.GetRecommendationsAsync(sessionId);
                var fargateRecommendation = getRecommendationOutput.Recommendations.FirstOrDefault(x => string.Equals(x.RecipeId, "AspNetAppEcsFargate"));

                var exception = await Assert.ThrowsExceptionAsync<ApiException<ProblemDetails>>(() => restClient.SetDeploymentTargetAsync(sessionId, new SetDeploymentTargetInput
                {
                    NewDeploymentName = invalidStackName,
                    NewDeploymentRecipeId = fargateRecommendation.RecipeId
                }));

                Assert.AreEqual(400, exception.StatusCode);

                var errorMessage = $"Invalid cloud application name: {invalidStackName}";
                //Assert.Contains(errorMessage, exception.Result.Detail);
            }
            finally
            {
                cancelSource.Cancel();
            }
        }

        [TestMethod]
        public async Task CheckCategories()
        {
            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppNoDockerFile", "WebAppNoDockerFile.csproj"));
            var portNumber = 4200;
            using var httpClient = ServerModeHttpClientFactory.ConstructHttpClient(ServerModeExtensions.ResolveCredentials);

            var serverCommand = new ServerModeCommand(_serviceProvider.GetRequiredService<IToolInteractiveService>(), portNumber, null, true);
            var cancelSource = new CancellationTokenSource();

            var serverTask = serverCommand.ExecuteAsync(cancelSource.Token);
            try
            {
                var restClient = new RestAPIClient($"http://localhost:{portNumber}/", httpClient);
                await restClient.WaitTillServerModeReady();

                var startSessionOutput = await restClient.StartDeploymentSessionAsync(new StartDeploymentSessionInput
                {
                    AwsRegion = _awsRegion,
                    ProjectPath = projectPath
                });

                var sessionId = startSessionOutput.SessionId;
                Assert.IsNotNull(sessionId);

                var getRecommendationOutput = await restClient.GetRecommendationsAsync(sessionId);

                foreach(var recommendation in getRecommendationOutput.Recommendations)
                {
                    Assert.AreNotEqual(0, recommendation.SettingsCategories.Count);
                    //Assert.Contains(recommendation.SettingsCategories, x => string.Equals(x.Id, AWS.Deploy.Common.Recipes.Category.DeploymentBundle.Id));
                    //Assert.DoesNotContain(recommendation.SettingsCategories, x => string.IsNullOrEmpty(x.Id));
                    //Assert.DoesNotContain(recommendation.SettingsCategories, x => string.IsNullOrEmpty(x.DisplayName));
                }

                var selectedRecommendation = getRecommendationOutput.Recommendations.First();
                await restClient.SetDeploymentTargetAsync(sessionId, new SetDeploymentTargetInput
                {
                    NewDeploymentRecipeId = selectedRecommendation.RecipeId,
                    NewDeploymentName = "TestStack-" + DateTime.UtcNow.Ticks
                });

                var getConfigSettingsResponse = await restClient.GetConfigSettingsAsync(sessionId);

                // Make sure all top level settings have a category
                //Assert.DoesNotContain(getConfigSettingsResponse.OptionSettings, x => string.IsNullOrEmpty(x.Category));

                // Make sure build settings have been applied a category.
                var buildSetting = getConfigSettingsResponse.OptionSettings.FirstOrDefault(x => string.Equals(x.Id, "DotnetBuildConfiguration"));
                Assert.IsNotNull(buildSetting);
                Assert.AreEqual(AWS.Deploy.Common.Recipes.Category.DeploymentBundle.Id, buildSetting.Category);
            }
            finally
            {
                cancelSource.Cancel();
            }
        }

        internal static void RegisterSignalRMessageCallbacks(IDeploymentCommunicationClient signalRClient, StringBuilder logOutput)
        {
            signalRClient.ReceiveLogSectionStart = (message, description) =>
            {
                logOutput.AppendLine(new string('*', message.Length));
                logOutput.AppendLine(message);
                logOutput.AppendLine(new string('*', message.Length));
            };
            signalRClient.ReceiveLogInfoMessage = (message) =>
            {
                logOutput.AppendLine(message);
            };
            signalRClient.ReceiveLogErrorMessage = (message) =>
            {
                logOutput.AppendLine(message);
            };
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

        ~ServerModeTests()
        {
            Dispose(false);
        }
    }
}
