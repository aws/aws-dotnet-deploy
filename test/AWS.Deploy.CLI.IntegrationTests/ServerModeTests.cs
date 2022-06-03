// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
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
using AWS.Deploy.CLI.ServerMode;
using AWS.Deploy.Orchestration.Utilities;
using AWS.Deploy.ServerMode.Client;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;

namespace AWS.Deploy.CLI.IntegrationTests
{
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

        public Task<AWSCredentials> ResolveCredentials()
        {
            var testCredentials = FallbackCredentialsFactory.GetCredentials();
            return Task.FromResult<AWSCredentials>(testCredentials);
        }

        [Fact]
        public async Task GetRecommendations()
        {
            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppNoDockerFile", "WebAppNoDockerFile.csproj"));
            var portNumber = 4000;
            using var httpClient = ServerModeHttpClientFactory.ConstructHttpClient(ResolveCredentials);

            var serverCommand = new ServerModeCommand(_serviceProvider.GetRequiredService<IToolInteractiveService>(), portNumber, null, true);
            var cancelSource = new CancellationTokenSource();

            var serverTask = serverCommand.ExecuteAsync(cancelSource.Token);
            try
            {
                var restClient = new RestAPIClient($"http://localhost:{portNumber}/", httpClient);
                await WaitTillServerModeReady(restClient);

                var startSessionOutput = await restClient.StartDeploymentSessionAsync(new StartDeploymentSessionInput
                {
                    AwsRegion = _awsRegion,
                    ProjectPath = projectPath
                });

                var sessionId = startSessionOutput.SessionId;
                Assert.NotNull(sessionId);

                var getRecommendationOutput = await restClient.GetRecommendationsAsync(sessionId);
                Assert.NotEmpty(getRecommendationOutput.Recommendations);
                var beanstalkRecommendation = getRecommendationOutput.Recommendations.FirstOrDefault();
                Assert.Equal("AspNetAppElasticBeanstalkLinux", beanstalkRecommendation.RecipeId);
                Assert.Null(beanstalkRecommendation.BaseRecipeId);
                Assert.False(beanstalkRecommendation.IsPersistedDeploymentProject);
                Assert.NotNull(beanstalkRecommendation.ShortDescription);
                Assert.NotNull(beanstalkRecommendation.Description);
                Assert.True(beanstalkRecommendation.ShortDescription.Length < beanstalkRecommendation.Description.Length);
                Assert.Equal("AWS Elastic Beanstalk", beanstalkRecommendation.TargetService);
            }
            finally
            {
                cancelSource.Cancel();
            }
        }

        [Fact]
        public async Task GetRecommendationsWithEncryptedCredentials()
        {
            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppNoDockerFile", "WebAppNoDockerFile.csproj"));
            var portNumber = 4000;

            var aes = Aes.Create();
            aes.GenerateKey();
            aes.GenerateIV();

            using var httpClient = ServerModeHttpClientFactory.ConstructHttpClient(ResolveCredentials, aes);

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
                await WaitTillServerModeReady(restClient);

                var startSessionOutput = await restClient.StartDeploymentSessionAsync(new StartDeploymentSessionInput
                {
                    AwsRegion = _awsRegion,
                    ProjectPath = projectPath
                });

                var sessionId = startSessionOutput.SessionId;
                Assert.NotNull(sessionId);

                var getRecommendationOutput = await restClient.GetRecommendationsAsync(sessionId);
                Assert.NotEmpty(getRecommendationOutput.Recommendations);
                Assert.Equal("AspNetAppElasticBeanstalkLinux", getRecommendationOutput.Recommendations.FirstOrDefault().RecipeId);

                var listDeployStdOut = _interactiveService.StdOutReader.ReadAllLines();
                Assert.Contains("Waiting on symmetric key from stdin", listDeployStdOut);
                Assert.Contains("Encryption provider enabled", listDeployStdOut);
            }
            finally
            {
                cancelSource.Cancel();
            }
        }

        [Fact]
        public async Task WebFargateDeploymentNoConfigChanges()
        {
            _stackName = $"ServerModeWebFargate{Guid.NewGuid().ToString().Split('-').Last()}";

            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppWithDockerFile", "WebAppWithDockerFile.csproj"));
            var portNumber = 4011;
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
                RegisterSignalRMessageCallbacks(signalRClient, logOutput);

                var getRecommendationOutput = await restClient.GetRecommendationsAsync(sessionId);
                Assert.NotEmpty(getRecommendationOutput.Recommendations);

                var fargateRecommendation = getRecommendationOutput.Recommendations.FirstOrDefault(x => string.Equals(x.RecipeId, "AspNetAppEcsFargate"));
                Assert.NotNull(fargateRecommendation);

                await restClient.SetDeploymentTargetAsync(sessionId, new SetDeploymentTargetInput
                {
                    NewDeploymentName = _stackName,
                    NewDeploymentRecipeId = fargateRecommendation.RecipeId
                });

                await restClient.StartDeploymentAsync(sessionId);

                await WaitForDeployment(restClient, sessionId);

                var stackStatus = await _cloudFormationHelper.GetStackStatus(_stackName);
                Assert.Equal(StackStatus.CREATE_COMPLETE, stackStatus);

                Assert.True(logOutput.Length > 0);

                // Make sure section header is return to output log
                Assert.Contains("Creating deployment image", logOutput.ToString());

                // Make sure normal log messages are returned to output log
                Assert.Contains("Pushing container image", logOutput.ToString());

                var redeploymentSessionOutput = await restClient.StartDeploymentSessionAsync(new StartDeploymentSessionInput
                {
                    AwsRegion = _awsRegion,
                    ProjectPath = projectPath
                });

                var redeploymentSessionId = redeploymentSessionOutput.SessionId;

                var existingDeployments = await restClient.GetExistingDeploymentsAsync(redeploymentSessionId);
                var existingDeployment = existingDeployments.ExistingDeployments.First(x => string.Equals(_stackName, x.Name));

                Assert.Equal(_stackName, existingDeployment.Name);
                Assert.Equal(fargateRecommendation.RecipeId, existingDeployment.RecipeId);
                Assert.Null(fargateRecommendation.BaseRecipeId);
                Assert.False(fargateRecommendation.IsPersistedDeploymentProject);
                Assert.Equal(fargateRecommendation.Name, existingDeployment.RecipeName);
                Assert.Equal(fargateRecommendation.ShortDescription, existingDeployment.ShortDescription);
                Assert.Equal(fargateRecommendation.Description, existingDeployment.Description);
                Assert.Equal(fargateRecommendation.TargetService, existingDeployment.TargetService);
                Assert.Equal(DeploymentTypes.CloudFormationStack, existingDeployment.DeploymentType);

                Assert.NotEmpty(existingDeployment.SettingsCategories);
                Assert.Contains(existingDeployment.SettingsCategories, x => string.Equals(x.Id, AWS.Deploy.Common.Recipes.Category.DeploymentBundle.Id));
                Assert.DoesNotContain(existingDeployment.SettingsCategories, x => string.IsNullOrEmpty(x.Id));
                Assert.DoesNotContain(existingDeployment.SettingsCategories, x => string.IsNullOrEmpty(x.DisplayName));
            }
            finally
            {
                cancelSource.Cancel();
                await _cloudFormationHelper.DeleteStack(_stackName);
                _stackName = null;
            }
        }

        [Fact]
        public async Task RecommendationsForNewDeployments_DoesNotIncludeExistingBeanstalkEnvironmentRecipe()
        {
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

                var startSessionOutput = await restClient.StartDeploymentSessionAsync(new StartDeploymentSessionInput
                {
                    AwsRegion = _awsRegion,
                    ProjectPath = projectPath
                });

                var sessionId = startSessionOutput.SessionId;
                Assert.NotNull(sessionId);

                var getRecommendationOutput = await restClient.GetRecommendationsAsync(sessionId);
                Assert.NotEmpty(getRecommendationOutput.Recommendations);

                var recommendations = getRecommendationOutput.Recommendations;
                Assert.DoesNotContain(recommendations, x => x.DeploymentType == DeploymentTypes.BeanstalkEnvironment);
            }
            finally
            {
                cancelSource.Cancel();
            }
        }

        [Fact]
        public async Task ShutdownViaRestClient()
        {
            var portNumber = 4003;
            var cancelSource = new CancellationTokenSource();
            using var httpClient = ServerModeHttpClientFactory.ConstructHttpClient(ResolveCredentials);
            var serverCommand = new ServerModeCommand(_serviceProvider.GetRequiredService<IToolInteractiveService>(), portNumber, null, true);

            var serverTask = serverCommand.ExecuteAsync(cancelSource.Token);

            try
            {
                var baseUrl = $"http://localhost:{portNumber}/";
                var restClient = new RestAPIClient(baseUrl, httpClient);

                await WaitTillServerModeReady(restClient);

                await restClient.ShutdownAsync();
                Thread.Sleep(100);

                // Expecting System.Net.Http.HttpRequestException : No connection could be made because the target machine actively refused it.
                await Assert.ThrowsAsync<System.Net.Http.HttpRequestException>(async () => await restClient.HealthAsync());
            }
            finally
            {
                cancelSource.Cancel();
            }
        }

        [Theory]
        [InlineData("1234MyAppStack")] // cannot start with a number
        [InlineData("MyApp@Stack/123")] // cannot contain special characters
        [InlineData("")] // cannot be empty
        [InlineData("stackstackstackstackstackstackstackstackstackstackstackstackstackstackstackstackstackstackstackstackstackstackstackstackstackstack")] // cannot contain more than 128 characters
        public async Task InvalidStackName_ThrowsException(string invalidStackName)
        {
            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppWithDockerFile", "WebAppWithDockerFile.csproj"));
            var portNumber = 4012;
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
                var getRecommendationOutput = await restClient.GetRecommendationsAsync(sessionId);
                var fargateRecommendation = getRecommendationOutput.Recommendations.FirstOrDefault(x => string.Equals(x.RecipeId, "AspNetAppEcsFargate"));

                var exception = await Assert.ThrowsAsync<ApiException<ProblemDetails>>(() => restClient.SetDeploymentTargetAsync(sessionId, new SetDeploymentTargetInput
                {
                    NewDeploymentName = invalidStackName,
                    NewDeploymentRecipeId = fargateRecommendation.RecipeId
                }));

                Assert.Equal(400, exception.StatusCode);

                var errorMessage = $"Invalid cloud application name: {invalidStackName}";
                Assert.Contains(errorMessage, exception.Result.Detail);
            }
            finally
            {
                cancelSource.Cancel();
            }
        }

        [Fact]
        public async Task CheckCategories()
        {
            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppNoDockerFile", "WebAppNoDockerFile.csproj"));
            var portNumber = 4200;
            using var httpClient = ServerModeHttpClientFactory.ConstructHttpClient(ResolveCredentials);

            var serverCommand = new ServerModeCommand(_serviceProvider.GetRequiredService<IToolInteractiveService>(), portNumber, null, true);
            var cancelSource = new CancellationTokenSource();

            var serverTask = serverCommand.ExecuteAsync(cancelSource.Token);
            try
            {
                var restClient = new RestAPIClient($"http://localhost:{portNumber}/", httpClient);
                await WaitTillServerModeReady(restClient);

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
                    Assert.NotEmpty(recommendation.SettingsCategories);
                    Assert.Contains(recommendation.SettingsCategories, x => string.Equals(x.Id, AWS.Deploy.Common.Recipes.Category.DeploymentBundle.Id));
                    Assert.DoesNotContain(recommendation.SettingsCategories, x => string.IsNullOrEmpty(x.Id));
                    Assert.DoesNotContain(recommendation.SettingsCategories, x => string.IsNullOrEmpty(x.DisplayName));
                }

                var selectedRecommendation = getRecommendationOutput.Recommendations.First();
                await restClient.SetDeploymentTargetAsync(sessionId, new SetDeploymentTargetInput
                {
                    NewDeploymentRecipeId = selectedRecommendation.RecipeId,
                    NewDeploymentName = "TestStack-" + DateTime.UtcNow.Ticks
                });

                var getConfigSettingsResponse = await restClient.GetConfigSettingsAsync(sessionId);

                // Make sure all top level settings have a category
                Assert.DoesNotContain(getConfigSettingsResponse.OptionSettings, x => string.IsNullOrEmpty(x.Category));

                // Make sure build settings have been applied a category.
                var buildSetting = getConfigSettingsResponse.OptionSettings.FirstOrDefault(x => string.Equals(x.Id, "DotnetBuildConfiguration"));
                Assert.NotNull(buildSetting);
                Assert.Equal(AWS.Deploy.Common.Recipes.Category.DeploymentBundle.Id, buildSetting.Category);
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

        private async Task<DeploymentStatus> WaitForDeployment(RestAPIClient restApiClient, string sessionId)
        {
            // Do an initial delay to avoid a race condition of the status being checked before the deployment has kicked off.
            await Task.Delay(TimeSpan.FromSeconds(3));

            GetDeploymentStatusOutput output = null;

            await WaitUntilHelper.WaitUntil(async () =>
            {
                output = (await restApiClient.GetDeploymentStatusAsync(sessionId));

                return output.Status != DeploymentStatus.Executing;
            }, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(15));

            if (output.Exception != null)
            {
                throw new Exception("Error waiting on stack status: " + output.Exception.Message);
            }

            return output.Status;
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

        ~ServerModeTests()
        {
            Dispose(false);
        }
    }
}
