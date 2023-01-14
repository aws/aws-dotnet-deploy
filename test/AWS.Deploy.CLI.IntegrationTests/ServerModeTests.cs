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
using AWS.Deploy.CLI.Commands;
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.CLI.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Helpers;
using AWS.Deploy.CLI.IntegrationTests.Services;
using AWS.Deploy.CLI.IntegrationTests.Utilities;
using AWS.Deploy.CLI.ServerMode;
using AWS.Deploy.ServerMode.Client;
using AWS.Deploy.ServerMode.Client.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NUnit.Framework;

namespace AWS.Deploy.CLI.IntegrationTests
{
    [TestFixture]
    public class ServerModeTests
    {
        private string _stackName;
        private IServiceProvider _serviceProvider;
        private CloudFormationHelper _cloudFormationHelper;

        private string _awsRegion;
        private TestAppManager _testAppManager;
        private InMemoryInteractiveService _interactiveService;

        [SetUp]
        public void Initialize()
        {
            _interactiveService = new InMemoryInteractiveService();

            var cloudFormationClient = new AmazonCloudFormationClient(Amazon.RegionEndpoint.USWest2);
            _cloudFormationHelper = new CloudFormationHelper(cloudFormationClient);

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddCustomServices(ServiceLifetime.Scoped);
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
        [Test]
        public async Task ConfirmLocalhostOnly()
        {
            var portNumber = 4900;
            var serverCommand = new ServerModeCommand(_serviceProvider.GetRequiredService<IToolInteractiveService>(), portNumber, null, true);
            var cancelSource = new CancellationTokenSource();

            _ = serverCommand.ExecuteAsync(cancelSource.Token);
            try
            {
                var restClient = new RestAPIClient($"http://localhost:{portNumber}/", ServerModeHttpClientFactory.ConstructHttpClient(ServerModeUtilities.ResolveDefaultCredentials));
                await restClient.WaitUntilServerModeReady();

                using var client = new HttpClient();

                var localhostUrl = $"http://localhost:{portNumber}/api/v1/Health";
                await client.GetStringAsync(localhostUrl);

                var host = Dns.GetHostName();
                var hostnameUrl = $"http://{host}:{portNumber}/api/v1/Health";
                Assert.ThrowsAsync<HttpRequestException>(async () => await client.GetStringAsync(hostnameUrl));
            }
            finally
            {
                cancelSource.Cancel();
            }
        }

        [Test]
        public async Task GetRecommendations()
        {
            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppNoDockerFile", "WebAppNoDockerFile.csproj"));
            var portNumber = 4000;
            using var httpClient = ServerModeHttpClientFactory.ConstructHttpClient(ServerModeUtilities.ResolveDefaultCredentials);

            var serverCommand = new ServerModeCommand(_serviceProvider.GetRequiredService<IToolInteractiveService>(), portNumber, null, true);
            var cancelSource = new CancellationTokenSource();

            var serverTask = serverCommand.ExecuteAsync(cancelSource.Token);
            try
            {
                var restClient = new RestAPIClient($"http://localhost:{portNumber}/", httpClient);
                await restClient.WaitUntilServerModeReady();

                var startSessionOutput = await restClient.StartDeploymentSessionAsync(new StartDeploymentSessionInput
                {
                    AwsRegion = _awsRegion,
                    ProjectPath = projectPath
                });

                var sessionId = startSessionOutput.SessionId;
                Assert.NotNull(sessionId);

                var getRecommendationOutput = await restClient.GetRecommendationsAsync(sessionId);
                Assert.IsNotEmpty(getRecommendationOutput.Recommendations);
                var beanstalkRecommendation = getRecommendationOutput.Recommendations.FirstOrDefault();
                Assert.AreEqual("AspNetAppElasticBeanstalkLinux", beanstalkRecommendation.RecipeId);
                Assert.Null(beanstalkRecommendation.BaseRecipeId);
                Assert.False(beanstalkRecommendation.IsPersistedDeploymentProject);
                Assert.NotNull(beanstalkRecommendation.ShortDescription);
                Assert.NotNull(beanstalkRecommendation.Description);
                Assert.True(beanstalkRecommendation.ShortDescription.Length < beanstalkRecommendation.Description.Length);
                Assert.AreEqual("AWS Elastic Beanstalk", beanstalkRecommendation.TargetService);
            }
            finally
            {
                cancelSource.Cancel();
            }
        }

        [Test]
        public async Task GetRecommendationsWithEncryptedCredentials()
        {
            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppNoDockerFile", "WebAppNoDockerFile.csproj"));
            var portNumber = 4000;

            var aes = Aes.Create();
            aes.GenerateKey();
            aes.GenerateIV();

            using var httpClient = ServerModeHttpClientFactory.ConstructHttpClient(ServerModeUtilities.ResolveDefaultCredentials, aes);

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
                await restClient.WaitUntilServerModeReady();

                var startSessionOutput = await restClient.StartDeploymentSessionAsync(new StartDeploymentSessionInput
                {
                    AwsRegion = _awsRegion,
                    ProjectPath = projectPath
                });

                var sessionId = startSessionOutput.SessionId;
                Assert.NotNull(sessionId);

                var getRecommendationOutput = await restClient.GetRecommendationsAsync(sessionId);
                Assert.IsNotEmpty(getRecommendationOutput.Recommendations);
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

        [Test]
        public async Task WebFargateDeploymentNoConfigChanges()
        {
            _stackName = $"ServerModeWebFargate{Guid.NewGuid().ToString().Split('-').Last()}";

            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppWithDockerFile", "WebAppWithDockerFile.csproj"));
            var portNumber = 4011;
            using var httpClient = ServerModeHttpClientFactory.ConstructHttpClient(ServerModeUtilities.ResolveDefaultCredentials);

            var serverCommand = new ServerModeCommand(_serviceProvider.GetRequiredService<IToolInteractiveService>(), portNumber, null, true);
            var cancelSource = new CancellationTokenSource();

            var serverTask = serverCommand.ExecuteAsync(cancelSource.Token);
            try
            {
                var baseUrl = $"http://localhost:{portNumber}/";
                var restClient = new RestAPIClient(baseUrl, httpClient);

                await restClient.WaitUntilServerModeReady();

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
                RegisterSignalRMessageCallbacks(signalRClient, logOutput);

                var getRecommendationOutput = await restClient.GetRecommendationsAsync(sessionId);
                Assert.IsNotEmpty(getRecommendationOutput.Recommendations);

                var fargateRecommendation = getRecommendationOutput.Recommendations.FirstOrDefault(x => string.Equals(x.RecipeId, "AspNetAppEcsFargate"));
                Assert.NotNull(fargateRecommendation);

                await restClient.SetDeploymentTargetAsync(sessionId, new SetDeploymentTargetInput
                {
                    NewDeploymentName = _stackName,
                    NewDeploymentRecipeId = fargateRecommendation.RecipeId
                });

                await restClient.StartDeploymentAsync(sessionId);

                await restClient.WaitForDeployment(sessionId);

                var stackStatus = await _cloudFormationHelper.GetStackStatus(_stackName);
                Assert.AreEqual(StackStatus.CREATE_COMPLETE, stackStatus);

                Assert.True(logOutput.Length > 0);

                // Make sure section header is return to output log
                StringAssert.Contains("Creating deployment image", logOutput.ToString());

                // Make sure normal log messages are returned to output log
                StringAssert.Contains("Pushing container image", logOutput.ToString());

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
                Assert.Null(fargateRecommendation.BaseRecipeId);
                Assert.False(fargateRecommendation.IsPersistedDeploymentProject);
                Assert.AreEqual(fargateRecommendation.Name, existingDeployment.RecipeName);
                Assert.AreEqual(fargateRecommendation.ShortDescription, existingDeployment.ShortDescription);
                Assert.AreEqual(fargateRecommendation.Description, existingDeployment.Description);
                Assert.AreEqual(fargateRecommendation.TargetService, existingDeployment.TargetService);
                Assert.AreEqual(DeploymentTypes.CloudFormationStack, existingDeployment.DeploymentType);

                Assert.IsNotEmpty(existingDeployment.SettingsCategories);
                Assert.That(existingDeployment.SettingsCategories, Has.Exactly(1).Matches<CategorySummary>(x => string.Equals(x.Id, AWS.Deploy.Common.Recipes.Category.DeploymentBundle.Id)));
                Assert.That(existingDeployment.SettingsCategories, Has.None.Matches<CategorySummary>(x => string.IsNullOrEmpty(x.Id)));
                Assert.That(existingDeployment.SettingsCategories, Has.None.Matches<CategorySummary>(x => string.IsNullOrEmpty(x.DisplayName)));

                // The below tests will check if updating readonly settings will properly get rejected.

                // These tests need to be performed on a redeployment
                await restClient.SetDeploymentTargetAsync(redeploymentSessionId, new SetDeploymentTargetInput
                {
                    ExistingDeploymentId = await _cloudFormationHelper.GetStackArn(_stackName)
                });

                var settings = await restClient.GetConfigSettingsAsync(sessionId);

                // Try to update a list of settings containing readonly settings which we expect to fail
                var updatedSettings = (IDictionary<string, string>)settings.OptionSettings.Where(x => !string.IsNullOrEmpty(x.FullyQualifiedId)).ToDictionary(k => k.FullyQualifiedId, v => "test");
                var exceptionThrown = Assert.ThrowsAsync<ApiException>(async () => await restClient.ApplyConfigSettingsAsync(redeploymentSessionId, new ApplyConfigSettingsInput()
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
                Assert.IsEmpty(applyConfigResponse.FailedConfigUpdates);
            }
            finally
            {
                cancelSource.Cancel();
                await _cloudFormationHelper.DeleteStack(_stackName);
                _stackName = null;
            }
        }

        [Test]
        public async Task RecommendationsForNewDeployments_DoesNotIncludeExistingBeanstalkEnvironmentRecipe()
        {
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

                var startSessionOutput = await restClient.StartDeploymentSessionAsync(new StartDeploymentSessionInput
                {
                    AwsRegion = _awsRegion,
                    ProjectPath = projectPath
                });

                var sessionId = startSessionOutput.SessionId;
                Assert.NotNull(sessionId);

                var getRecommendationOutput = await restClient.GetRecommendationsAsync(sessionId);
                Assert.IsNotEmpty(getRecommendationOutput.Recommendations);

                var recommendations = getRecommendationOutput.Recommendations;

                Assert.That(recommendations, Has.None.Matches<RecommendationSummary>(x => x.DeploymentType == DeploymentTypes.BeanstalkEnvironment));
            }
            finally
            {
                cancelSource.Cancel();
            }
        }

        [Test]
        public async Task ShutdownViaRestClient()
        {
            var portNumber = 4003;
            var cancelSource = new CancellationTokenSource();
            using var httpClient = ServerModeHttpClientFactory.ConstructHttpClient(ServerModeUtilities.ResolveDefaultCredentials);
            var serverCommand = new ServerModeCommand(_serviceProvider.GetRequiredService<IToolInteractiveService>(), portNumber, null, true);

            var serverTask = serverCommand.ExecuteAsync(cancelSource.Token);

            try
            {
                var baseUrl = $"http://localhost:{portNumber}/";
                var restClient = new RestAPIClient(baseUrl, httpClient);

                await restClient.WaitUntilServerModeReady();

                await restClient.ShutdownAsync();
                Thread.Sleep(100);

                // Expecting System.Net.Http.HttpRequestException : No connection could be made because the target machine actively refused it.
                Assert.ThrowsAsync<System.Net.Http.HttpRequestException>(async () => await restClient.HealthAsync());
            }
            finally
            {
                cancelSource.Cancel();
            }
        }

        [Test]
        [TestCase("1234MyAppStack")] // cannot start with a number
        [TestCase("MyApp@Stack/123")] // cannot contain special characters
        [TestCase("")] // cannot be empty
        [TestCase("stackstackstackstackstackstackstackstackstackstackstackstackstackstackstackstackstackstackstackstackstackstackstackstackstackstack")] // cannot contain more than 128 characters
        public async Task InvalidStackName_ThrowsException(string invalidStackName)
        {
            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppWithDockerFile", "WebAppWithDockerFile.csproj"));
            var portNumber = 4012;
            using var httpClient = ServerModeHttpClientFactory.ConstructHttpClient(ServerModeUtilities.ResolveDefaultCredentials);

            var serverCommand = new ServerModeCommand(_serviceProvider.GetRequiredService<IToolInteractiveService>(), portNumber, null, true);
            var cancelSource = new CancellationTokenSource();

            var serverTask = serverCommand.ExecuteAsync(cancelSource.Token);
            try
            {
                var baseUrl = $"http://localhost:{portNumber}/";
                var restClient = new RestAPIClient(baseUrl, httpClient);

                await restClient.WaitUntilServerModeReady();

                var startSessionOutput = await restClient.StartDeploymentSessionAsync(new StartDeploymentSessionInput
                {
                    AwsRegion = _awsRegion,
                    ProjectPath = projectPath
                });

                var sessionId = startSessionOutput.SessionId;
                var getRecommendationOutput = await restClient.GetRecommendationsAsync(sessionId);
                var fargateRecommendation = getRecommendationOutput.Recommendations.FirstOrDefault(x => string.Equals(x.RecipeId, "AspNetAppEcsFargate"));

                var exception = Assert.ThrowsAsync<ApiException<ProblemDetails>>(() => restClient.SetDeploymentTargetAsync(sessionId, new SetDeploymentTargetInput
                {
                    NewDeploymentName = invalidStackName,
                    NewDeploymentRecipeId = fargateRecommendation.RecipeId
                }));

                Assert.AreEqual(400, exception.StatusCode);

                var errorMessage = $"Invalid cloud application name: {invalidStackName}";
                StringAssert.Contains(errorMessage, exception.Result.Detail);
            }
            finally
            {
                cancelSource.Cancel();
            }
        }

        [Test]
        public async Task CheckCategories()
        {
            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppNoDockerFile", "WebAppNoDockerFile.csproj"));
            var portNumber = 4200;
            using var httpClient = ServerModeHttpClientFactory.ConstructHttpClient(ServerModeUtilities.ResolveDefaultCredentials);

            var serverCommand = new ServerModeCommand(_serviceProvider.GetRequiredService<IToolInteractiveService>(), portNumber, null, true);
            var cancelSource = new CancellationTokenSource();

            var serverTask = serverCommand.ExecuteAsync(cancelSource.Token);
            try
            {
                var restClient = new RestAPIClient($"http://localhost:{portNumber}/", httpClient);
                await restClient.WaitUntilServerModeReady();

                var startSessionOutput = await restClient.StartDeploymentSessionAsync(new StartDeploymentSessionInput
                {
                    AwsRegion = _awsRegion,
                    ProjectPath = projectPath
                });

                var sessionId = startSessionOutput.SessionId;
                Assert.NotNull(sessionId);

                var getRecommendationOutput = await restClient.GetRecommendationsAsync(sessionId);

                foreach(var recommendation in getRecommendationOutput.Recommendations)
                {
                    Assert.IsNotEmpty(recommendation.SettingsCategories);
                    Assert.That(recommendation.SettingsCategories, Has.Exactly(1).Matches<CategorySummary>(x => string.Equals(x.Id, AWS.Deploy.Common.Recipes.Category.DeploymentBundle.Id)));
                    Assert.That(recommendation.SettingsCategories, Has.None.Matches<CategorySummary>(x => string.IsNullOrEmpty(x.Id)));
                    Assert.That(recommendation.SettingsCategories, Has.None.Matches<CategorySummary>(x => string.IsNullOrEmpty(x.DisplayName)));
                }

                var selectedRecommendation = getRecommendationOutput.Recommendations.First();
                await restClient.SetDeploymentTargetAsync(sessionId, new SetDeploymentTargetInput
                {
                    NewDeploymentRecipeId = selectedRecommendation.RecipeId,
                    NewDeploymentName = "TestStack-" + DateTime.UtcNow.Ticks
                });

                var getConfigSettingsResponse = await restClient.GetConfigSettingsAsync(sessionId);

                // Make sure all top level settings have a category
                Assert.That(getConfigSettingsResponse.OptionSettings, Has.None.Matches<OptionSettingItemSummary>(x => string.IsNullOrEmpty(x.Category)));

                // Make sure build settings have been applied a category.
                var buildSetting = getConfigSettingsResponse.OptionSettings.FirstOrDefault(x => string.Equals(x.Id, "DotnetBuildConfiguration"));
                Assert.NotNull(buildSetting);
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

        [TearDown]
        public async Task Cleanup()
        {
            if (!string.IsNullOrEmpty(_stackName))
            {
                var isStackDeleted = await _cloudFormationHelper.IsStackDeleted(_stackName);
                if (!isStackDeleted)
                {
                    await _cloudFormationHelper.DeleteStack(_stackName);
                }

                _interactiveService.ReadStdOutStartToEnd();
            }
        }
    }
}
