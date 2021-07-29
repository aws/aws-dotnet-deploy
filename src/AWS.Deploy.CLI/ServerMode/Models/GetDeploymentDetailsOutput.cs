// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace AWS.Deploy.CLI.ServerMode.Models
{
    public class GetDeploymentDetailsOutput
    {
        /// <summary>
        /// The CloudFormation Stack ID
        /// </summary>
        public string StackId { get; set; }

        /// <summary>
        /// The list of displayed resources based on the recipe definition
        /// </summary>
        public List<DisplayedResourceSummary> DisplayedResources { get; set; }

        public GetDeploymentDetailsOutput(string stackId, List<DisplayedResourceSummary> displayedResources)
        {
            StackId = stackId;
            DisplayedResources = displayedResources;
        }
    }
}
