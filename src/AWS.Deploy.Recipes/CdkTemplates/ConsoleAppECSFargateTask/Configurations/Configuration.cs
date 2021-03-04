// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

namespace ConsoleAppEcsFargateTask.Configurations
{
    public class Configuration
    {
        /// <summary>
        /// The file name of the Dockerfile.
        /// </summary>
        public string DockerfileName { get; set; } = "Dockerfile";

        /// <summary>
        /// The Identity and Access Management Role that provides AWS credentials to the application to access AWS services.
        /// </summary>
        public IAMRoleConfiguration ApplicationIAMRole { get; set; }

        /// <summary>
        /// The schedule or rate (frequency) that determines when CloudWatch Events runs the rule.
        /// </summary>
        public string Schedule { get; set; }

        /// <summary>
        /// The name of the ECS cluster.
        /// </summary>
        public string ClusterName { get; set; }

        /// <summary>
        /// Virtual Private Cloud to launch container instance into a virtual network.
        /// </summary>
        public VpcConfiguration Vpc { get; set; }

        /// <inheritdoc cref="FargateTaskDefinitionProps.Cpu"/>
        public double? TaskCpu { get; set; }

        /// <inheritdoc cref="FargateTaskDefinitionProps.MemoryLimitMiB"/>
        public double? TaskMemory { get; set; }
    }
}
