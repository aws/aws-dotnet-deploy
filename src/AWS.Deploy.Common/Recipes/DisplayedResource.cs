// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.Common.Recipes
{
    public class DisplayedResource
    {
        /// <summary>
        /// The CloudFormation ID that represents a resource.
        /// </summary>
        public string LogicalId { get; set; }

        /// <summary>
        /// The Description gives context to the metadata of the CloudFormation resource.
        /// </summary>
        public string Description { get; set; }

        public DisplayedResource(string logicalId, string description)
        {
            LogicalId = logicalId;
            Description = description;
        }
    }
}
