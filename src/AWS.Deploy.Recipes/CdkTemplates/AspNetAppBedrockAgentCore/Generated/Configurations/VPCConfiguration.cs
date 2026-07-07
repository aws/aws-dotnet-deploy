// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace AspNetAppBedrockAgentCore.Configurations
{
    public partial class VPCConfiguration
    {
        /// <summary>
        /// Whether to place the AgentCore Runtime in a VPC.
        /// When false, PUBLIC networking is used.
        /// </summary>
        public bool UseVPC { get; set; }

        /// <summary>
        /// Whether to create a new VPC.
        /// </summary>
        public bool CreateNew { get; set; }

        /// <summary>
        /// The existing VPC ID to use. Only used when CreateNew is false.
        /// </summary>
        public string VpcId { get; set; } = "";

        /// <summary>
        /// Security groups to assign to the AgentCore Runtime.
        /// </summary>
        public SortedSet<string> SecurityGroups { get; set; } = new SortedSet<string>();
    }
}
