// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

// This is a generated file from the original deployment recipe. It contains properties for
// all of the settings defined in the recipe file. It is recommended to not modify this file in order
// to allow easy updates to the file when the original recipe that this project was created from has updates.
// This class is marked as a partial class. If you add new settings to the recipe file, those settings should be
// added to partial versions of this class outside of the Generated folder for example in the Configuration folder.

using System.Collections.Generic;

namespace AspNetAppBedrockAgentCore.Configurations
{
    public partial class Configuration
    {
        /// <summary>
        /// The name for the Bedrock AgentCore Runtime agent.
        /// </summary>
        public string RuntimeName { get; set; }

        /// <summary>
        /// AgentCore Memory configuration.
        /// </summary>
        public MemoryConfiguration AgentCoreMemory { get; set; }

        /// <summary>
        /// IAM role configuration for the AgentCore Runtime.
        /// </summary>
        public IAMRoleConfiguration RuntimeIAMRole { get; set; }

        /// <summary>
        /// HTTP request header names to pass through to the agent container.
        /// When empty, no RequestHeaderConfiguration is set on the runtime.
        /// </summary>
        public SortedSet<string> RequestHeaders { get; set; } = new SortedSet<string>();

        /// <summary>
        /// Environment variables to pass to the AgentCore Runtime container.
        /// </summary>
        public Dictionary<string, string> AgentCoreEnvironmentVariables { get; set; } = new Dictionary<string, string>();

#nullable disable warnings
        public Configuration()
        {
        }
#nullable restore warnings
    }
}
