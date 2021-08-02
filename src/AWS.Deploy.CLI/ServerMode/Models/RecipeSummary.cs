// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.CLI.ServerMode.Models
{
    public class RecipeSummary
    {
        public string Id { get; set; }

        public string Version { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string ShortDescription { get; set; }

        public string TargetService { get; set; }

        public string DeploymentType { get; set; }

        public string DeploymentBundle { get; set; }

        public RecipeSummary(string id, string version, string name, string description, string shortDescription, string targetService, string deploymentType, string deploymentBundle)
        {
            Id = id;
            Version = version;
            Name = name;
            Description = description;
            ShortDescription = shortDescription;
            TargetService = targetService;
            DeploymentType = deploymentType;
            DeploymentBundle = deploymentBundle;
        }
    }
}
