// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AWS.Deploy.CLI.Commands;
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.CLI.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Utilities;
using AWS.Deploy.ServerMode.Client;
using AWS.Deploy.ServerMode.Client.Utilities;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace AWS.Deploy.CLI.IntegrationTests.ServerMode
{
    [TestFixture]
    public class DependencyValidationOptionSettings
    {
        private IServiceProvider _serviceProvider;

        private string _awsRegion;
        private TestAppManager _testAppManager;

        [SetUp]
        public void Initialize()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddCustomServices(ServiceLifetime.Scoped);
            serviceCollection.AddTestServices();

            _serviceProvider = serviceCollection.BuildServiceProvider();

            _awsRegion = "us-west-2";

            _testAppManager = new TestAppManager();
        }

        [Test]
        public async Task DependentOptionSettingsGetInvalidated()
        {
            var stackName = $"ServerModeWebAppRunner{Guid.NewGuid().ToString().Split('-').Last()}";

            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppNoDockerFile", "WebAppNoDockerFile.csproj"));
            var portNumber = 4022;
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

                var beanstalkRecommendation = await restClient.GetRecommendationsAndSetDeploymentTarget(sessionId, "AspNetAppElasticBeanstalkLinux", stackName);

                var applicationIAMRole = (await restClient.GetConfigSettingsAsync(sessionId)).OptionSettings.First(x => x.Id.Equals("ApplicationIAMRole"));
                Assert.AreEqual(ValidationStatus.Valid, applicationIAMRole.Validation.ValidationStatus);
                Assert.True(string.IsNullOrEmpty(applicationIAMRole.Validation.ValidationMessage));
                Assert.Null(applicationIAMRole.Validation.InvalidValue);
                var validCreateNew = applicationIAMRole.ChildOptionSettings.First(x => x.Id.Equals("CreateNew"));
                Assert.AreEqual(ValidationStatus.Valid, validCreateNew.Validation.ValidationStatus);
                Assert.True(string.IsNullOrEmpty(validCreateNew.Validation.ValidationMessage));
                Assert.Null(validCreateNew.Validation.InvalidValue);
                var validRoleArn = applicationIAMRole.ChildOptionSettings.First(x => x.Id.Equals("RoleArn"));
                Assert.AreEqual(ValidationStatus.Valid, validRoleArn.Validation.ValidationStatus);
                Assert.True(string.IsNullOrEmpty(validRoleArn.Validation.ValidationMessage));
                Assert.Null(validRoleArn.Validation.InvalidValue);

                var applyConfigResponse = await restClient.ApplyConfigSettingsAsync(sessionId, new ApplyConfigSettingsInput()
                {
                    UpdatedSettings = new Dictionary<string, string>()
                    {
                        {"ApplicationIAMRole.CreateNew", "false"}
                    }
                });

                applicationIAMRole = (await restClient.GetConfigSettingsAsync(sessionId)).OptionSettings.First(x => x.Id.Equals("ApplicationIAMRole"));
                Assert.AreEqual(ValidationStatus.Valid, applicationIAMRole.Validation.ValidationStatus);
                Assert.True(string.IsNullOrEmpty(applicationIAMRole.Validation.ValidationMessage));
                Assert.Null(applicationIAMRole.Validation.InvalidValue);
                validCreateNew = applicationIAMRole.ChildOptionSettings.First(x => x.Id.Equals("CreateNew"));
                Assert.AreEqual(ValidationStatus.Valid, validCreateNew.Validation.ValidationStatus);
                Assert.True(string.IsNullOrEmpty(validCreateNew.Validation.ValidationMessage));
                Assert.Null(validCreateNew.Validation.InvalidValue);
                validRoleArn = applicationIAMRole.ChildOptionSettings.First(x => x.Id.Equals("RoleArn"));
                Assert.AreEqual(ValidationStatus.Invalid, validRoleArn.Validation.ValidationStatus);
                Assert.AreEqual("Invalid IAM Role ARN. The ARN should contain the arn:[PARTITION]:iam namespace, followed by the account ID, and then the resource path. For example - arn:aws:iam::123456789012:role/S3Access is a valid IAM Role ARN. For more information visit https://docs.aws.amazon.com/IAM/latest/UserGuide/reference_identifiers.html#identifiers-arns",
                    validRoleArn.Validation.ValidationMessage);
                Assert.NotNull(validRoleArn.Validation.InvalidValue);
                Assert.True(string.IsNullOrEmpty(validRoleArn.Validation.InvalidValue.ToString()));

                applyConfigResponse = await restClient.ApplyConfigSettingsAsync(sessionId, new ApplyConfigSettingsInput()
                {
                    UpdatedSettings = new Dictionary<string, string>()
                    {
                        {"ApplicationIAMRole.CreateNew", "true"}
                    }
                });

                applicationIAMRole = (await restClient.GetConfigSettingsAsync(sessionId)).OptionSettings.First(x => x.Id.Equals("ApplicationIAMRole"));
                Assert.AreEqual(ValidationStatus.Valid, applicationIAMRole.Validation.ValidationStatus);
                Assert.True(string.IsNullOrEmpty(applicationIAMRole.Validation.ValidationMessage));
                Assert.Null(applicationIAMRole.Validation.InvalidValue);
                validCreateNew = applicationIAMRole.ChildOptionSettings.First(x => x.Id.Equals("CreateNew"));
                Assert.AreEqual(ValidationStatus.Valid, validCreateNew.Validation.ValidationStatus);
                Assert.True(string.IsNullOrEmpty(validCreateNew.Validation.ValidationMessage));
                Assert.Null(validCreateNew.Validation.InvalidValue);
                validRoleArn = applicationIAMRole.ChildOptionSettings.First(x => x.Id.Equals("RoleArn"));
                Assert.AreEqual(ValidationStatus.Valid, validRoleArn.Validation.ValidationStatus);
                Assert.True(string.IsNullOrEmpty(validRoleArn.Validation.ValidationMessage));
                Assert.Null(validRoleArn.Validation.InvalidValue);
            }
            finally
            {
                cancelSource.Cancel();
            }
        }

        [Test]
        public async Task SettingInvalidValue()
        {
            var stackName = $"ServerModeWebAppRunner{Guid.NewGuid().ToString().Split('-').Last()}";

            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "WebAppNoDockerFile", "WebAppNoDockerFile.csproj"));
            var portNumber = 4022;
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

                var beanstalkRecommendation = await restClient.GetRecommendationsAndSetDeploymentTarget(sessionId, "AspNetAppElasticBeanstalkLinux", stackName);

                var applicationIAMRole = (await restClient.GetConfigSettingsAsync(sessionId)).OptionSettings.First(x => x.Id.Equals("ApplicationIAMRole"));
                Assert.Null(applicationIAMRole.Validation.InvalidValue);
                var validCreateNew = applicationIAMRole.ChildOptionSettings.First(x => x.Id.Equals("CreateNew"));
                Assert.Null(validCreateNew.Validation.InvalidValue);
                var validRoleArn = applicationIAMRole.ChildOptionSettings.First(x => x.Id.Equals("RoleArn"));
                Assert.Null(validRoleArn.Validation.InvalidValue);

                var applyConfigResponse = await restClient.ApplyConfigSettingsAsync(sessionId, new ApplyConfigSettingsInput()
                {
                    UpdatedSettings = new Dictionary<string, string>()
                    {
                        {"ApplicationIAMRole.CreateNew", "false"},
                        {"ApplicationIAMRole.RoleArn", "fakeArn"}
                    }
                });

                applicationIAMRole = (await restClient.GetConfigSettingsAsync(sessionId)).OptionSettings.First(x => x.Id.Equals("ApplicationIAMRole"));
                Assert.Null(applicationIAMRole.Validation.InvalidValue);
                validCreateNew = applicationIAMRole.ChildOptionSettings.First(x => x.Id.Equals("CreateNew"));
                Assert.Null(validCreateNew.Validation.InvalidValue);
                validRoleArn = applicationIAMRole.ChildOptionSettings.First(x => x.Id.Equals("RoleArn"));
                Assert.NotNull(validRoleArn.Validation.InvalidValue);
                Assert.AreEqual("fakeArn", validRoleArn.Validation.InvalidValue);

                applyConfigResponse = await restClient.ApplyConfigSettingsAsync(sessionId, new ApplyConfigSettingsInput()
                {
                    UpdatedSettings = new Dictionary<string, string>()
                    {
                        {"ApplicationIAMRole.CreateNew", "true"}
                    }
                });

                applicationIAMRole = (await restClient.GetConfigSettingsAsync(sessionId)).OptionSettings.First(x => x.Id.Equals("ApplicationIAMRole"));
                Assert.Null(applicationIAMRole.Validation.InvalidValue);
                validCreateNew = applicationIAMRole.ChildOptionSettings.First(x => x.Id.Equals("CreateNew"));
                Assert.Null(validCreateNew.Validation.InvalidValue);
                validRoleArn = applicationIAMRole.ChildOptionSettings.First(x => x.Id.Equals("RoleArn"));
                Assert.Null(validRoleArn.Validation.InvalidValue);

                applyConfigResponse = await restClient.ApplyConfigSettingsAsync(sessionId, new ApplyConfigSettingsInput()
                {
                    UpdatedSettings = new Dictionary<string, string>()
                    {
                        {"ApplicationIAMRole.RoleArn", "fakeArn"}
                    }
                });

                applicationIAMRole = (await restClient.GetConfigSettingsAsync(sessionId)).OptionSettings.First(x => x.Id.Equals("ApplicationIAMRole"));
                Assert.Null(applicationIAMRole.Validation.InvalidValue);
                validCreateNew = applicationIAMRole.ChildOptionSettings.First(x => x.Id.Equals("CreateNew"));
                Assert.Null(validCreateNew.Validation.InvalidValue);
                validRoleArn = applicationIAMRole.ChildOptionSettings.First(x => x.Id.Equals("RoleArn"));
                Assert.Null(validRoleArn.Validation.InvalidValue);
            }
            finally
            {
                cancelSource.Cancel();
            }
        }
    }
}
