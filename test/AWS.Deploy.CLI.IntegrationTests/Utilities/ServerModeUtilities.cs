// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.Runtime;
using AWS.Deploy.Common;
using AWS.Deploy.Orchestration.Utilities;
using AWS.Deploy.ServerMode.Client;
using Moq;
using Xunit;

namespace AWS.Deploy.CLI.IntegrationTests.Utilities
{
    public static class ServerModeExtensions
    {
        public static async Task WaitTillServerModeReady(this RestAPIClient restApiClient)
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

        public static async Task<string> StartDeploymentSession(this RestAPIClient restClient, string projectPath, string awsRegion)
        {
            var startSessionOutput = await restClient.StartDeploymentSessionAsync(new StartDeploymentSessionInput
            {
                AwsRegion = awsRegion,
                ProjectPath = projectPath
            });

            var sessionId = startSessionOutput.SessionId;
            Assert.NotNull(sessionId);
            return sessionId;
        }

        public static async Task SetupSignalRConnection(string baseUrl, string sessionId, StringBuilder logOutput)
        {
            var signalRClient = new DeploymentCommunicationClient(baseUrl);
            await signalRClient.JoinSession(sessionId);

            ServerModeTests.RegisterSignalRMessageCallbacks(signalRClient, logOutput);
        }

        public static async Task<RecommendationSummary> GetRecommendationsAndSetDeploymentTarget(this RestAPIClient restClient, string sessionId, string recipeId, string stackName)
        {
            var getRecommendationOutput = await restClient.GetRecommendationsAsync(sessionId);
            Assert.NotEmpty(getRecommendationOutput.Recommendations);

            var beanstalkRecommendation =
                getRecommendationOutput.Recommendations.FirstOrDefault(x => string.Equals(x.RecipeId, recipeId));
            Assert.NotNull(beanstalkRecommendation);

            await restClient.SetDeploymentTargetAsync(sessionId,
                new SetDeploymentTargetInput
                {
                    NewDeploymentName = stackName,
                    NewDeploymentRecipeId = beanstalkRecommendation.RecipeId
                });
            return beanstalkRecommendation;
        }

        public static async Task<CloudApplicationMetadata> GetAppSettingsFromCFTemplate(Mock<IAWSClientFactory> mockAWSClientFactory, Mock<IAmazonCloudFormation> mockCFClient, string cloudFormationTemplate, string stackName)
        {
            var templateMetadataReader = GetTemplateMetadataReader(mockAWSClientFactory, mockCFClient, cloudFormationTemplate);
            return await templateMetadataReader.LoadCloudApplicationMetadata(stackName);
        }

        public static TemplateMetadataReader GetTemplateMetadataReader(Mock<IAWSClientFactory> mockAWSClientFactory, Mock<IAmazonCloudFormation> mockCFClient, string templateBody)
        {
            var templateMetadataReader = new TemplateMetadataReader(mockAWSClientFactory.Object);
            var cfResponse = new GetTemplateResponse();
            cfResponse.TemplateBody = templateBody;
            mockAWSClientFactory.Setup(x => x.GetAWSClient<IAmazonCloudFormation>(It.IsAny<string>())).Returns(mockCFClient.Object);
            mockCFClient.Setup(x => x.GetTemplateAsync(It.IsAny<GetTemplateRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(cfResponse);
            return templateMetadataReader;
        }

        public static Task<AWSCredentials> ResolveCredentials()
        {
            var testCredentials = FallbackCredentialsFactory.GetCredentials();
            return Task.FromResult<AWSCredentials>(testCredentials);
        }
    }
}
