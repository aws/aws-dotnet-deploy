// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

namespace ConsoleAppECSFargateScheduleTask.Configurations
{
    public class Configuration
    {
        /// <summary>
        /// The Identity and Access Management Role that provides AWS credentials to the application to access AWS services.
        /// </summary>
        public IAMRoleConfiguration ApplicationIAMRole { get; set; }

        /// <summary>
        /// The schedule or rate (frequency) that determines when CloudWatch Events runs the rule.
        /// </summary>
        public string Schedule { get; set; }

        /// <summary>
        /// The ECS cluster that will host the deployed application.
        /// </summary>
        public ECSClusterConfiguration ECSCluster { get; set; }

        /// <summary>
        /// Virtual Private Cloud to launch container instance into a virtual network.
        /// </summary>
        public VpcConfiguration Vpc { get; set; }

        /// <inheritdoc cref="FargateTaskDefinitionProps.Cpu"/>
        public double? TaskCpu { get; set; }

        /// <inheritdoc cref="FargateTaskDefinitionProps.MemoryLimitMiB"/>
        public double? TaskMemory { get; set; }

        /// A parameterless constructor is needed for <see cref="Microsoft.Extensions.Configuration.ConfigurationBuilder"/>
        /// or the classes will fail to initialize.
        /// The warnings are disabled since a parameterless constructor will allow non-nullable properties to be initialized with null values.
#nullable disable warnings
        public Configuration()
        {

        }
#nullable restore warnings

        public Configuration(
            IAMRoleConfiguration applicationIAMRole,
            string schedule,
            ECSClusterConfiguration ecsCluster,
            VpcConfiguration vpc
            )
        {
            ApplicationIAMRole = applicationIAMRole;
            Schedule = schedule;
            ECSCluster = ecsCluster;
            Vpc = vpc;
        }
    }
}
