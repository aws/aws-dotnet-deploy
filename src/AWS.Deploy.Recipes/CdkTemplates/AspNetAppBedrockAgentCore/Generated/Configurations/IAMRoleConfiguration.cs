// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AspNetAppBedrockAgentCore.Configurations
{
    public partial class IAMRoleConfiguration
    {
        /// <summary>
        /// Whether to create a new IAM role or use an existing one.
        /// </summary>
        public bool CreateNew { get; set; }

        /// <summary>
        /// The ARN of an existing IAM role to use. Only used when CreateNew is false.
        /// </summary>
        public string? RoleArn { get; set; }
    }
}
