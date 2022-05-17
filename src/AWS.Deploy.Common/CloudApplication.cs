// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using AWS.Deploy.Common.Recipes;

namespace AWS.Deploy.Common
{
    /// <summary>
    /// Contains CloudFormation specific configurations
    /// </summary>
    public class CloudApplication
    {
        private readonly Dictionary<CloudApplicationResourceType, string> _resourceTypeMapping =
            new()
            {
                { CloudApplicationResourceType.CloudFormationStack, "CloudFormation Stack" },
                { CloudApplicationResourceType.BeanstalkEnvironment, "Elastic Beanstalk Environment" },
                { CloudApplicationResourceType.ElasticContainerRegistryImage, "ECR Repository" }
            };

        private readonly Dictionary<CloudApplicationResourceType, DeploymentTypes> _deploymentTypeMapping =
            new()
            {
                { CloudApplicationResourceType.CloudFormationStack, DeploymentTypes.CdkProject},
                { CloudApplicationResourceType.BeanstalkEnvironment, DeploymentTypes.BeanstalkEnvironment },
                { CloudApplicationResourceType.ElasticContainerRegistryImage, DeploymentTypes.ElasticContainerRegistryImage }
            };

        /// <summary>
        /// Name of the CloudApplication resource
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The unique Id to identify the CloudApplication.
        /// The ID is set to the StackId if the CloudApplication is an existing Cloudformation stack.
        /// The ID is set to the EnvironmentId if the CloudApplication is an existing Elastic Beanstalk environment.
        /// The ID is set to string.Empty for new CloudApplications.
        /// </summary>
        public string UniqueIdentifier { get; set; }

        /// <summary>
        /// The id of the AWS .NET deployment tool recipe used to create or re-deploy the cloud application.
        /// </summary>
        public string RecipeId { get; set; }

        /// <summary>
        /// Indicates the type of the AWS resource which serves as the deployment target.
        /// </summary>
        public CloudApplicationResourceType ResourceType { get; set; }

        /// <summary>
        /// Last updated time of CloudFormation stack
        /// </summary>
        public DateTime? LastUpdatedTime { get; set; }

        /// <summary>
        /// Indicates whether the Cloud Application has been redeployed by the current user.
        /// </summary>
        public bool UpdatedByCurrentUser { get; set; }

        /// <summary>
        /// This name is shown to the user when the CloudApplication is presented as an existing re-deployment target.
        /// </summary>
        public string DisplayName => $"{Name} ({_resourceTypeMapping[ResourceType]})";

        /// <summary>
        /// Display the name of the Cloud Application
        /// </summary>
        public override string ToString() => Name;

        /// <summary>
        /// Gets the deployment type of the recommendation that was used to deploy the cloud application.
        /// </summary>
        public DeploymentTypes DeploymentType => _deploymentTypeMapping[ResourceType];

        public CloudApplication(string name, string uniqueIdentifier, CloudApplicationResourceType resourceType, string recipeId, DateTime? lastUpdatedTime = null)
        {
            Name = name;
            UniqueIdentifier = uniqueIdentifier;
            ResourceType = resourceType;
            RecipeId = recipeId;
            LastUpdatedTime = lastUpdatedTime;
        }
    }
}
