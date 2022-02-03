// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Orchestration.DisplayedResources;

namespace AWS.Deploy.Orchestration.DeploymentCommands
{
    public class BeanstalkEnvironmentDeploymentCommand : IDeploymentCommand
    {
        public async Task ExecuteAsync(Orchestrator orchestrator, CloudApplication cloudApplication, Recommendation recommendation)
        {
            if (orchestrator._interactiveService == null)
                throw new InvalidOperationException($"{nameof(orchestrator._interactiveService)} is null as part of the orchestartor object");
            if (orchestrator._awsResourceQueryer == null)
                throw new InvalidOperationException($"{nameof(orchestrator._awsResourceQueryer)} is null as part of the orchestartor object");
            if (orchestrator._awsServiceHandler == null)
                throw new InvalidOperationException($"{nameof(orchestrator._awsServiceHandler)} is null as part of the orchestartor object");

            var deploymentPackage = recommendation.DeploymentBundle.DotnetPublishZipPath;
            var environmentName = cloudApplication.Name;
            var applicationName = (await orchestrator._awsResourceQueryer.ListOfElasticBeanstalkEnvironments())
                .Where(x => string.Equals(x.EnvironmentId, cloudApplication.UniqueIdentifier))
                .FirstOrDefault()?
                .ApplicationName;

            var s3Handler = orchestrator._awsServiceHandler.S3Handler;
            var elasticBeanstalkHandler = orchestrator._awsServiceHandler.ElasticBeanstalkHandler;

            if (string.IsNullOrEmpty(applicationName))
            {
                var message = $"Could not find any Elastic Beanstalk application that contains the following environment: {environmentName}";
                throw new AWSResourceNotFoundException(DeployToolErrorCode.FailedToFindElasticBeanstalkApplication, message);
            }

            orchestrator._interactiveService.LogMessageLine($"Initiating deployment to {cloudApplication.DisplayName}...");

            var versionLabel = $"v-{DateTime.Now.Ticks}";
            var s3location = await elasticBeanstalkHandler.CreateApplicationStorageLocationAsync(applicationName, versionLabel, deploymentPackage);
            await s3Handler.UploadToS3Async(s3location.S3Bucket, s3location.S3Key, deploymentPackage);
            await elasticBeanstalkHandler.CreateApplicationVersionAsync(applicationName, versionLabel, s3location);
            var environmentConfigurationSettings = elasticBeanstalkHandler.GetEnvironmentConfigurationSettings(recommendation);

            var success = await elasticBeanstalkHandler.UpdateEnvironmentAsync(applicationName, environmentName, versionLabel, environmentConfigurationSettings);

            if (success)
            {
                orchestrator._interactiveService.LogMessageLine($"The Elastic Beanstalk Environment {environmentName} has been successfully updated to the application version {versionLabel}" + Environment.NewLine);
            } 
            else
            {
                throw new ElasticBeanstalkException(DeployToolErrorCode.FailedToUpdateElasticBeanstalkEnvironment, "Failed to update the Elastic Beanstalk environment");
            }
        }
        public async Task<List<DisplayedResourceItem>> GetDeploymentOutputsAsync(IDisplayedResourcesHandler displayedResourcesHandler, CloudApplication cloudApplication, Recommendation recommendation)
        {
            var displayedResources = new List<DisplayedResourceItem>();
            var environment = await displayedResourcesHandler.AwsResourceQueryer.DescribeElasticBeanstalkEnvironment(cloudApplication.Name);
            var data = new Dictionary<string, string>() {{ "Endpoint", $"http://{environment.CNAME}/" }};
            var resourceDescription = "An AWS Elastic Beanstalk environment is a collection of AWS resources running an application version.";
            var resourceType = "Elastic Beanstalk Environment";
            displayedResources.Add(new DisplayedResourceItem(environment.EnvironmentName, resourceDescription, resourceType, data));
            return displayedResources;
        }
    }
}
