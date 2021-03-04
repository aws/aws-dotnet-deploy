// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

namespace AspNetAppEcsFargate.Configurations
{
    public class Configuration
    {
        /// <summary>
        /// The Identity and Access Management Role that provides AWS credentials to the application to access AWS services.
        /// </summary>
        public IAMRoleConfiguration ApplicationIAMRole { get; set; }

        /// <summary>
        /// The desired number of ECS tasks to run for the service.
        /// </summary>
        public double DesiredCount { get; set; }

        /// <summary>
        /// The name of the ECS service running in the cluster.
        /// </summary>
        public string ECSServiceName { get; set; }

        /// <summary>
        /// The ECS cluster that will host the deployed application.
        /// </summary>
        public ECSClusterConfiguration ECSCluster { get; set; }

        /// <summary>
        /// Virtual Private Cloud to launch container instance into a virtual network.
        /// </summary>
        public VpcConfiguration Vpc { get; set; }

        /// <summary>
        /// Comma-delimited list of security groups assigned to the ECS service.
        /// </summary>
        public string AdditionalECSServiceSecurityGroups { get; set; }

        /// <inheritdoc cref="FargateTaskDefinitionProps.Cpu"/>
        public double? TaskCpu { get; set; }

        /// <inheritdoc cref="FargateTaskDefinitionProps.MemoryLimitMiB"/>
        public double? TaskMemory { get; set; }
    }
}
