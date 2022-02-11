// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.ElasticBeanstalk;
using Amazon.IdentityManagement;
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.CLI.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Helpers;
using AWS.Deploy.CLI.IntegrationTests.Services;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Orchestration.Data;
using AWS.Deploy.Orchestration.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AWS.Deploy.CLI.IntegrationTests.BeanstalkBackwardsCompatibilityTests
{
    public class TestContextFixture : IAsyncLifetime
    {
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
        public string EnvironmentId;

        public TestContextFixture()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddCustomServices();
            serviceCollection.AddTestServices();

            var serviceProvider = serviceCollection.BuildServiceProvider();

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

            EBHelper = new ElasticBeanstalkHelper(new AmazonElasticBeanstalkClient(), AWSResourceQueryer, ToolInteractiveService);
            IAMHelper = new IAMHelper(new AmazonIdentityManagementServiceClient(), AWSResourceQueryer, ToolInteractiveService);
        }

        public async Task InitializeAsync()
        {
            await IAMHelper.CreateRoleForBeanstalkEnvionmentDeployment("aws-elasticbeanstalk-ec2-role");

            var projectPath = TestAppManager.GetProjectPath(Path.Combine("testapps", "WebAppNoDockerFile", "WebAppNoDockerFile.csproj"));
            var publishDirectoryInfo = DirectoryManager.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
            var zipFilePath = $"{publishDirectoryInfo.FullName}.zip";

            var publishCommand =
                $"dotnet publish \"{projectPath}\"" +
                $" -o \"{publishDirectoryInfo}\"" +
                $" -c release";

            var result = await CommandLineWrapper.TryRunWithResult(publishCommand, streamOutputToInteractiveService: true);
            Assert.Equal(0, result.ExitCode);

            await ZipFileManager.CreateFromDirectory(publishDirectoryInfo.FullName, zipFilePath);

            await EBHelper.CreateApplicationAsync(ApplicationName);
            await EBHelper.CreateApplicationVersionAsync(ApplicationName, VersionLabel, zipFilePath);
            var success = await EBHelper.CreateEnvironmentAsync(ApplicationName, EnvironmentName, VersionLabel);
            Assert.True(success);

            var environmentDescription = await AWSResourceQueryer.DescribeElasticBeanstalkEnvironment(EnvironmentName);
            EnvironmentId = environmentDescription.EnvironmentId;

            // URL could take few more minutes to come live, therefore, we want to wait and keep trying for a specified timeout
            await HttpHelper.WaitUntilSuccessStatusCode(environmentDescription.CNAME, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5));
        }

        public async Task DisposeAsync()
        {
            var success = await EBHelper.DeleteApplication(ApplicationName, EnvironmentName);
            Assert.True(success);
        }
    }
}
