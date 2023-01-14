// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using Amazon.ElasticBeanstalk;
using Amazon.IdentityManagement;
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.CLI.IntegrationTests.Helpers;
using AWS.Deploy.CLI.IntegrationTests.Services;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common;
using AWS.Deploy.Orchestration.Utilities;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using AWS.Deploy.CLI.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Extensions;
using System.IO;
using System.Threading.Tasks;
using AWS.Deploy.CLI.Commands;
using AWS.Deploy.CLI.IntegrationTests.Utilities;
using AWS.Deploy.ServerMode.Client;
using System.Text;
using System.Threading;
using AWS.Deploy.ServerMode.Client.Utilities;

namespace AWS.Deploy.CLI.IntegrationTests.BeanstalkBackwardsCompatibilityTests
{
    [TestFixture]
    public class ExistingLinuxEnvironmentTests
    {
        private const string BEANSTALK_ENVIRONMENT_RECIPE_ID = "AspNetAppExistingBeanstalkEnvironment";

        public readonly App App;
        public readonly HttpHelper HttpHelper;
        public readonly IAWSResourceQueryer AWSResourceQueryer;
        public readonly TestAppManager TestAppManager;
        public readonly IDirectoryManager DirectoryManager;
        public readonly ICommandLineWrapper CommandLineWrapper;
        public readonly IZipFileManager ZipFileManager;
        public readonly IToolInteractiveService ToolInteractiveService;
        public readonly InMemoryInteractiveService InteractiveService;
        public readonly ElasticBeanstalkHelper EBHelper;
        public readonly IAMHelper IAMHelper;

        public readonly string ApplicationName;
        public readonly string EnvironmentName;
        public readonly string VersionLabel;
        public readonly string RoleName;
        public string EnvironmentId;

        public ExistingLinuxEnvironmentTests()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddCustomServices();
            serviceCollection.AddTestServices();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var awsClientFactory = serviceProvider.GetService<IAWSClientFactory>();
            awsClientFactory.ConfigureAWSOptions((options) =>
            {
                options.Region = Amazon.RegionEndpoint.USWest2;
            });

            App = serviceProvider.GetService<App>();
            Assert.NotNull(App);

            InteractiveService = serviceProvider.GetService<InMemoryInteractiveService>();
            Assert.NotNull(InteractiveService);

            ToolInteractiveService = serviceProvider.GetService<IToolInteractiveService>();

            AWSResourceQueryer = serviceProvider.GetService<IAWSResourceQueryer>();
            Assert.NotNull(AWSResourceQueryer);

            CommandLineWrapper = serviceProvider.GetService<ICommandLineWrapper>();
            Assert.NotNull(CommandLineWrapper);

            ZipFileManager = serviceProvider.GetService<IZipFileManager>();
            Assert.NotNull(ZipFileManager);

            DirectoryManager = serviceProvider.GetService<IDirectoryManager>();
            Assert.NotNull(DirectoryManager);

            HttpHelper = new HttpHelper(InteractiveService);
            TestAppManager = new TestAppManager();

            var suffix = Guid.NewGuid().ToString().Split('-').Last();
            ApplicationName = $"application{suffix}";
            EnvironmentName = $"environment{suffix}";
            VersionLabel = $"v-{suffix}";
            RoleName = $"aws-elasticbeanstalk-ec2-role{suffix}";

            EBHelper = new ElasticBeanstalkHelper(new AmazonElasticBeanstalkClient(Amazon.RegionEndpoint.USWest2), AWSResourceQueryer, ToolInteractiveService);
            IAMHelper = new IAMHelper(new AmazonIdentityManagementServiceClient(), AWSResourceQueryer, ToolInteractiveService);
        }

        [OneTimeSetUp]
        public async Task Initialize()
        {
            await IAMHelper.CreateRoleForBeanstalkEnvionmentDeployment(RoleName);

            var projectPath = TestAppManager.GetProjectPath(Path.Combine("testapps", "WebAppNoDockerFile", "WebAppNoDockerFile.csproj"));
            var publishDirectoryInfo = DirectoryManager.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
            var zipFilePath = $"{publishDirectoryInfo.FullName}.zip";

            var publishCommand =
                $"dotnet publish \"{projectPath}\"" +
                $" -o \"{publishDirectoryInfo}\"" +
                $" -c release";

            var result = await CommandLineWrapper.TryRunWithResult(publishCommand, streamOutputToInteractiveService: true);
            Assert.AreEqual(0, result.ExitCode);

            await ZipFileManager.CreateFromDirectory(publishDirectoryInfo.FullName, zipFilePath);

            await EBHelper.CreateApplicationAsync(ApplicationName);
            await EBHelper.CreateApplicationVersionAsync(ApplicationName, VersionLabel, zipFilePath);
            var success = await EBHelper.CreateEnvironmentAsync(ApplicationName, EnvironmentName, VersionLabel, BeanstalkPlatformType.Linux);
            Assert.True(success);

            var environmentDescription = await AWSResourceQueryer.DescribeElasticBeanstalkEnvironment(EnvironmentName);
            EnvironmentId = environmentDescription.EnvironmentId;

            // URL could take few more minutes to come live, therefore, we want to wait and keep trying for a specified timeout
            await HttpHelper.WaitUntilSuccessStatusCode(environmentDescription.CNAME, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5));
        }

        [Test]
        public async Task DeployToExistingBeanstalkEnvironment()
        {
            var projectPath = TestAppManager.GetProjectPath(Path.Combine("testapps", "WebAppNoDockerFile", "WebAppNoDockerFile.csproj"));
            var deployArgs = new[] { "deploy", "--project-path", projectPath, "--application-name", EnvironmentName, "--diagnostics", "--silent", "--region", "us-west-2" };
            Assert.AreEqual(CommandReturnCodes.SUCCESS, await App.Run(deployArgs));

            var environmentDescription = await AWSResourceQueryer.DescribeElasticBeanstalkEnvironment(EnvironmentName);

            // URL could take few more minutes to come live, therefore, we want to wait and keep trying for a specified timeout
            await HttpHelper.WaitUntilSuccessStatusCode(environmentDescription.CNAME, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5));

            var successMessagePrefix = $"The Elastic Beanstalk Environment {EnvironmentName} has been successfully updated";
            var deployStdOutput = InteractiveService.StdOutReader.ReadAllLines();
            var successMessage = deployStdOutput.First(line => line.Trim().StartsWith(successMessagePrefix));
            Assert.False(string.IsNullOrEmpty(successMessage));

            var expectedVersionLabel = successMessage.Split(" ").Last();
            Assert.True(await EBHelper.VerifyEnvironmentVersionLabel(EnvironmentName, expectedVersionLabel));
        }

        [Test]
        public async Task DeployToExistingBeanstalkEnvironmentSelfContained()
        {
            var projectPath = TestAppManager.GetProjectPath(Path.Combine("testapps", "WebAppNoDockerFile", "WebAppNoDockerFile.csproj"));
            var deployArgs = new[] { "deploy", "--project-path", projectPath, "--application-name", EnvironmentName, "--diagnostics", "--silent", "--region", "us-west-2", "--apply", "Existing-ElasticBeanStalkConfigFile-Linux-SelfContained.json" };
            Assert.AreEqual(CommandReturnCodes.SUCCESS, await App.Run(deployArgs));

            var environmentDescription = await AWSResourceQueryer.DescribeElasticBeanstalkEnvironment(EnvironmentName);

            // URL could take few more minutes to come live, therefore, we want to wait and keep trying for a specified timeout
            await HttpHelper.WaitUntilSuccessStatusCode(environmentDescription.CNAME, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5));

            var successMessagePrefix = $"The Elastic Beanstalk Environment {EnvironmentName} has been successfully updated";
            var deployStdOutput = InteractiveService.StdOutReader.ReadAllLines();
            var successMessage = deployStdOutput.First(line => line.Trim().StartsWith(successMessagePrefix));
            Assert.False(string.IsNullOrEmpty(successMessage));

            var expectedVersionLabel = successMessage.Split(" ").Last();
            Assert.True(await EBHelper.VerifyEnvironmentVersionLabel(EnvironmentName, expectedVersionLabel));
        }

        [Test]
        public async Task ServerModeDeployToExistingBeanstalkEnvironment()
        {
            var projectPath = TestAppManager.GetProjectPath(Path.Combine("testapps", "WebAppNoDockerFile", "WebAppNoDockerFile.csproj"));
            var portNumber = 4031;
            using var httpClient = ServerModeHttpClientFactory.ConstructHttpClient(ServerModeUtilities.ResolveDefaultCredentials);

            var serverCommand = new ServerModeCommand(ToolInteractiveService, portNumber, null, true);
            var cancelSource = new CancellationTokenSource();

            var serverTask = serverCommand.ExecuteAsync(cancelSource.Token);
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
                var existingDeployment = existingDeployments.ExistingDeployments.First(x => string.Equals(EnvironmentName, x.Name));

                Assert.AreEqual(EnvironmentName, existingDeployment.Name);
                Assert.AreEqual(BEANSTALK_ENVIRONMENT_RECIPE_ID, existingDeployment.RecipeId);
                Assert.Null(existingDeployment.BaseRecipeId);
                Assert.False(existingDeployment.IsPersistedDeploymentProject);
                Assert.AreEqual(EnvironmentId, existingDeployment.ExistingDeploymentId);
                Assert.AreEqual(DeploymentTypes.BeanstalkEnvironment, existingDeployment.DeploymentType);

                var signalRClient = new DeploymentCommunicationClient(baseUrl);
                await signalRClient.JoinSession(sessionId);

                var logOutput = new StringBuilder();
                AWS.Deploy.CLI.IntegrationTests.ServerModeTests.RegisterSignalRMessageCallbacks(signalRClient, logOutput);

                await restClient.SetDeploymentTargetAsync(sessionId, new SetDeploymentTargetInput
                {
                    ExistingDeploymentId = EnvironmentId
                });

                await restClient.StartDeploymentAsync(sessionId);

                await restClient.WaitForDeployment(sessionId);

                Assert.True(logOutput.Length > 0);
                var successMessagePrefix = $"The Elastic Beanstalk Environment {EnvironmentName} has been successfully updated";
                var deployStdOutput = logOutput.ToString().Split(Environment.NewLine);
                var successMessage = deployStdOutput.First(line => line.Trim().StartsWith(successMessagePrefix));
                Assert.False(string.IsNullOrEmpty(successMessage));

                var expectedVersionLabel = successMessage.Split(" ").Last();
                Assert.True(await EBHelper.VerifyEnvironmentVersionLabel(EnvironmentName, expectedVersionLabel));
            }
            finally
            {
                cancelSource.Cancel();
            }
        }

        [OneTimeTearDown]
        public async Task Cleanup()
        {
            var success = await EBHelper.DeleteApplication(ApplicationName, EnvironmentName);
            await IAMHelper.DeleteRoleAndInstanceProfileAfterBeanstalkEnvionmentDeployment(RoleName);
            Assert.True(success);
        }
    }
}
