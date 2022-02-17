// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace AWS.Deploy.CLI.ServerMode.Models
{
    public class GetDeploymentDetailsOutput
    {
        /// <summary>
        /// The CloudApplication Name
        /// </summary>
        public string CloudApplicationName { get; set; }

        /// <summary>
        /// The list of displayed resources based on the recipe definition
        /// </summary>
        public List<DisplayedResourceSummary> DisplayedResources { get; set; }

        public GetDeploymentDetailsOutput(string cloudApplicationName, List<DisplayedResourceSummary> displayedResources)
        {
            CloudApplicationName = cloudApplicationName;
            DisplayedResources = displayedResources;
        }
    }
}
