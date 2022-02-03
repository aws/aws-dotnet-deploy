// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.Deploy.CLI.ServerMode.Models
{
    public class RecommendationSummary
    {
        private readonly Dictionary<Common.Recipes.DeploymentTypes, DeploymentTypes> _deploymentTargetsMapping = new()
        {
            { Common.Recipes.DeploymentTypes.CdkProject, DeploymentTypes.CloudFormationStack },
            { Common.Recipes.DeploymentTypes.BeanstalkEnvironment, DeploymentTypes.BeanstalkEnvironment }
        };

        public string RecipeId { get; set; }
        public string Name { get; set; }
        public string ShortDescription { get; set; }
        public string Description { get; set; }
        public string TargetService { get; set; }
        public DeploymentTypes DeploymentType { get; set; }

        public RecommendationSummary(
            string recipeId,
            string name,
            string shortDescription,
            string description,
            string targetService,
            Common.Recipes.DeploymentTypes deploymentType
        )
        {
            RecipeId = recipeId;
            Name = name;
            ShortDescription = shortDescription;
            Description = description;
            TargetService = targetService;

            if (!_deploymentTargetsMapping.ContainsKey(deploymentType))
            {
                var message = $"Failed to find a deployment target mapping for {nameof(Common.Recipes.DeploymentTypes)} {deploymentType}.";
                throw new FailedToFindDeploymentTargetsMappingException(message);
            }

            DeploymentType = _deploymentTargetsMapping[deploymentType];
        }
    }
}
