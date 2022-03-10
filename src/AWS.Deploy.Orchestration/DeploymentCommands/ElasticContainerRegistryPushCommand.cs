// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Orchestration.DisplayedResources;

namespace AWS.Deploy.Orchestration.DeploymentCommands
{
    public class ElasticContainerRegistryPushCommand : IDeploymentCommand
    {
        // This method does not have a body because an actual deployment is not being performed.
        // The container images are already pushed to ECR in the orchestrator.CreateContainerDeploymentBundle(..) method.
        public Task ExecuteAsync(Orchestrator orchestrator, CloudApplication cloudApplication, Recommendation recommendation)
        {
            return Task.CompletedTask;
        }

        public async Task<List<DisplayedResourceItem>> GetDeploymentOutputsAsync(IDisplayedResourcesHandler displayedResourcesHandler, CloudApplication cloudApplication, Recommendation recommendation)
        {
            var displayedResources = new List<DisplayedResourceItem>();

            var repositoryName = recommendation.DeploymentBundle.ECRRepositoryName;
            var imageTag = recommendation.DeploymentBundle.ECRImageTag;
            var repository = await displayedResourcesHandler.AwsResourceQueryer.DescribeECRRepository(repositoryName);
            var data = new Dictionary<string, string>() {
                { "Repository URI", repository.RepositoryUri },
                { "Image URI", $"{repository.RepositoryUri}:{imageTag}"}
            };

            var resourceDescription = "Amazon Elastic Container Registry is a collection of repositories that can store tagged container images";
            var resourceType = "Elastic Container Registry Repository";
            displayedResources.Add(new DisplayedResourceItem(repository.RepositoryName, resourceDescription, resourceType, data));
            return displayedResources;
        }
    }
}
