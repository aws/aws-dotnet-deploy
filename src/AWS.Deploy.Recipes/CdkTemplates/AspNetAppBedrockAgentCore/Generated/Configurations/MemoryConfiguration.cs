// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AspNetAppBedrockAgentCore.Configurations
{
    public partial class MemoryConfiguration
    {
        /// <summary>
        /// Whether to create a new AgentCore Memory resource.
        /// </summary>
        public bool CreateNew { get; set; }

        /// <summary>
        /// The ID of an existing AgentCore Memory to use. Only used when CreateNew is false.
        /// </summary>
        public string? MemoryId { get; set; }
    }
}
