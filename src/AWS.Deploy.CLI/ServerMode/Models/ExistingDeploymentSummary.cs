// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.Common;

namespace AWS.Deploy.CLI.ServerMode.Models
{
    public class ExistingDeploymentSummary
    {
        private readonly Dictionary<CloudApplicationResourceType, DeploymentTypes> _deploymentTargetsMapping = new()
        {
            { CloudApplicationResourceType.CloudFormationStack, DeploymentTypes.CloudFormationStack },
            { CloudApplicationResourceType.BeanstalkEnvironment, DeploymentTypes.BeanstalkEnvironment },
            { CloudApplicationResourceType.ElasticContainerRegistryImage, DeploymentTypes.ElasticContainerRegistryImage}
        };

        public string Name { get; set; }

        public string? BaseRecipeId { get; set; }

        public string RecipeId { get; set; }

        public string RecipeName { get; set; }

        public bool IsPersistedDeploymentProject { get; set; }

        public string ShortDescription { get; set; }

        public string Description { get; set; }

        public string TargetService { get; set; }

        public DateTime? LastUpdatedTime { get; set; }

        public bool UpdatedByCurrentUser { get; set; }

        public DeploymentTypes DeploymentType { get; set; }

        public string ExistingDeploymentId { get; set; }

        public ExistingDeploymentSummary(
            string name,
            string? baseRecipeId,
            string recipeId,
            string recipeName,
            bool isPersistedDeploymentProject,
            string shortDescription,
            string description,
            string targetService,
            DateTime? lastUpdatedTime,
            bool updatedByCurrentUser,
            CloudApplicationResourceType resourceType,
            string uniqueIdentifier
        )
        {
            Name = name;
            BaseRecipeId = baseRecipeId;
            RecipeId = recipeId;
            RecipeName = recipeName;
            IsPersistedDeploymentProject = isPersistedDeploymentProject;
            ShortDescription = shortDescription;
            Description = description;
            TargetService = targetService;
            LastUpdatedTime = lastUpdatedTime;
            UpdatedByCurrentUser = updatedByCurrentUser;
            ExistingDeploymentId = uniqueIdentifier;

            if (!_deploymentTargetsMapping.ContainsKey(resourceType))
            {
                var message = $"Failed to find a deployment target mapping for {nameof(CloudApplicationResourceType)} {resourceType}.";
                throw new FailedToFindDeploymentTargetsMappingException(message);
            }

            DeploymentType = _deploymentTargetsMapping[resourceType];  
        }
    }
}
