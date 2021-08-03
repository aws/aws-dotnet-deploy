// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace AWS.Deploy.CLI.ServerMode.Models
{
    public class DisplayedResourceSummary
    {
        /// <summary>
        /// The Physical ID that represents a CloudFormation resource
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The description of the resource that is defined in the recipe definition
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The CloudFormation resource type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The Key Value pair of additional data unique to this resource type
        /// </summary>
        public Dictionary<string, string> Data { get; set; }

        public DisplayedResourceSummary(string id, string description, string type, Dictionary<string, string> data)
        {
            Id = id;
            Description = description;
            Type = type;
            Data = data;
        }
    }
}
