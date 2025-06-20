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
using AWS.Deploy.Common.IO;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.Utilities;
using AWS.Deploy.ServerMode.Client;
using Moq;
using Xunit;

namespace AWS.Deploy.CLI.IntegrationTests.Utilities
{
    public static class ServerModeExtensions
    {
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
                getRecommendationOutput.Recommendations.First(x => string.Equals(x.RecipeId, recipeId));
            Assert.NotNull(beanstalkRecommendation);

            await restClient.SetDeploymentTargetAsync(sessionId,
                new SetDeploymentTargetInput
                {
                    NewDeploymentName = stackName,
                    NewDeploymentRecipeId = beanstalkRecommendation.RecipeId
                });
            return beanstalkRecommendation;
        }

        public static async Task<CloudApplicationMetadata> GetAppSettingsFromCFTemplate(Mock<IAWSClientFactory> mockAWSClientFactory, Mock<IAmazonCloudFormation> mockCFClient, string cloudFormationTemplate, string stackName, Mock<IDeployToolWorkspaceMetadata> deployToolWorkspaceMetadata, IFileManager fileManager)
        {
            var templateMetadataReader = GetTemplateMetadataReader(mockAWSClientFactory, mockCFClient, cloudFormationTemplate, deployToolWorkspaceMetadata, fileManager);
            return await templateMetadataReader.LoadCloudApplicationMetadata(stackName);
        }

        public static CloudFormationTemplateReader GetTemplateMetadataReader(Mock<IAWSClientFactory> mockAWSClientFactory, Mock<IAmazonCloudFormation> mockCFClient, string templateBody, Mock<IDeployToolWorkspaceMetadata> deployToolWorkspaceMetadata, IFileManager fileManager)
        {
            var templateMetadataReader = new CloudFormationTemplateReader(mockAWSClientFactory.Object, deployToolWorkspaceMetadata.Object, fileManager);
            var cfResponse = new GetTemplateResponse();
            cfResponse.TemplateBody = templateBody;
            mockAWSClientFactory.Setup(x => x.GetAWSClient<IAmazonCloudFormation>(It.IsAny<string>())).Returns(mockCFClient.Object);
            mockCFClient.Setup(x => x.GetTemplateAsync(It.IsAny<GetTemplateRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(cfResponse);
            return templateMetadataReader;
        }

        public static async Task<DeploymentStatus> WaitForDeployment(this RestAPIClient restApiClient, string sessionId)
        {
            // Do an initial delay to avoid a race condition of the status being checked before the deployment has kicked off.
            await Task.Delay(TimeSpan.FromSeconds(3));

            GetDeploymentStatusOutput output = null!;

            await Orchestration.Utilities.Helpers.WaitUntil(async () =>
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
    }
}
