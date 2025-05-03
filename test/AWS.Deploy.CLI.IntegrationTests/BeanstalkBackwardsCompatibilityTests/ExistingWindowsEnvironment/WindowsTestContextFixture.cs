// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.ElasticBeanstalk;
using Amazon.IdentityManagement;
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.CLI.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Helpers;
using AWS.Deploy.CLI.IntegrationTests.Services;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Orchestration.ServiceHandlers;
using AWS.Deploy.Orchestration.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AWS.Deploy.CLI.IntegrationTests.BeanstalkBackwardsCompatibilityTests.ExistingWindowsEnvironment
{
    /// <summary>
    /// The goal of this class is to be used as shared context between a collection of tests.
    /// More info could be found here https://xunit.net/docs/shared-context
    /// </summary>
    public class WindowsTestContextFixture : IAsyncLifetime
    {
        public readonly HttpHelper HttpHelper;
        public readonly IAWSResourceQueryer AWSResourceQueryer;
        public readonly TestAppManager TestAppManager;
        public readonly IDirectoryManager DirectoryManager;
        public readonly ICommandLineWrapper CommandLineWrapper;
        public readonly IZipFileManager ZipFileManager;
        public readonly IToolInteractiveService ToolInteractiveService;
        public readonly IElasticBeanstalkHandler ElasticBeanstalkHandler;
        public readonly InMemoryInteractiveService InteractiveService;
        public readonly ElasticBeanstalkHelper EBHelper;
        public readonly IAMHelper IAMHelper;
        public readonly IServiceCollection ServiceCollection;

        public readonly string ApplicationName;
        public readonly string EnvironmentName;
        public readonly string VersionLabel;
        public readonly string RoleName;
        public string EnvironmentId;

        public WindowsTestContextFixture()
        {
            ServiceCollection = new ServiceCollection();

            ServiceCollection.AddCustomServices();
            ServiceCollection.AddTestServices();

            var serviceProvider = ServiceCollection.BuildServiceProvider();
            var awsClientFactory = serviceProvider.GetService<IAWSClientFactory>();
            awsClientFactory.ConfigureAWSOptions((options) =>
            {
                options.Region = Amazon.RegionEndpoint.USWest2;
            });

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

            ElasticBeanstalkHandler = serviceProvider.GetService<IElasticBeanstalkHandler>();
            Assert.NotNull(ElasticBeanstalkHandler);

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

        public async Task InitializeAsync()
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
            Assert.Equal(0, result.ExitCode);

            await ZipFileManager.CreateFromDirectory(publishDirectoryInfo.FullName, zipFilePath);

            SetupWindowsDeploymentManifest(zipFilePath);

            await EBHelper.CreateApplicationAsync(ApplicationName);
            await EBHelper.CreateApplicationVersionAsync(ApplicationName, VersionLabel, zipFilePath);
            var success = await EBHelper.CreateEnvironmentAsync(ApplicationName, EnvironmentName, "net8.0", VersionLabel, BeanstalkPlatformType.Windows, RoleName);
            Assert.True(success);

            var environmentDescription = await AWSResourceQueryer.DescribeElasticBeanstalkEnvironment(EnvironmentName);
            EnvironmentId = environmentDescription.EnvironmentId;

            // URL could take few more minutes to come live, therefore, we want to wait and keep trying for a specified timeout
            await HttpHelper.WaitUntilSuccessStatusCode(environmentDescription.CNAME, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5));
        }

        public void SetupWindowsDeploymentManifest(string dotnetZipFilePath)
        {
            const string MANIFEST_FILENAME = "aws-windows-deployment-manifest.json";

            var jsonStream = new MemoryStream();
            using (var jsonWriter = new Utf8JsonWriter(jsonStream))
            {
                jsonWriter.WriteStartObject();
                jsonWriter.WritePropertyName("manifestVersion");
                jsonWriter.WriteNumberValue(1);

                jsonWriter.WriteStartObject("deployments");
                jsonWriter.WriteStartArray("aspNetCoreWeb");

                jsonWriter.WriteStartObject();
                jsonWriter.WritePropertyName("name");
                jsonWriter.WriteStringValue("MainApp");

                jsonWriter.WriteStartObject("parameters");
                jsonWriter.WritePropertyName("appBundle");
                jsonWriter.WriteStringValue(".");
                jsonWriter.WritePropertyName("iisWebSite");
                jsonWriter.WriteStringValue("Default Web Site");
                jsonWriter.WritePropertyName("iisPath");
                jsonWriter.WriteStringValue("/");
                jsonWriter.WriteEndObject();

                jsonWriter.WriteEndObject();

                jsonWriter.WriteEndArray();
                jsonWriter.WriteEndObject();
                jsonWriter.WriteEndObject();
            }

            using (var zipArchive = ZipFile.Open(dotnetZipFilePath, ZipArchiveMode.Update))
            {
                var zipEntry = zipArchive.CreateEntry(MANIFEST_FILENAME);
                using var zipEntryStream = zipEntry.Open();
                jsonStream.Position = 0;
                jsonStream.CopyTo(zipEntryStream);

            }
        }

        public async Task DisposeAsync()
        {
            var success = await EBHelper.DeleteApplication(ApplicationName, EnvironmentName);
            await IAMHelper.DeleteRoleAndInstanceProfileAfterBeanstalkEnvionmentDeployment(RoleName);
            Assert.True(success);
        }
    }
}
