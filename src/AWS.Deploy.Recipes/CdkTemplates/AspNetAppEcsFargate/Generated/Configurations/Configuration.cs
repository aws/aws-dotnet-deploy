// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

// This is a generated file from the original deployment recipe. It contains properties for
// all of the settings defined in the recipe file. It is recommended to not modify this file in order
// to allow easy updates to the file when the original recipe that this project was created from has updates.
// This class is marked as a partial class. If you add new settings to the recipe file, those settings should be
// added to partial versions of this class outside of the Generated folder for example in the Configuration folder.

using System.Collections.Generic;

namespace AspNetAppEcsFargate.Configurations
{
    public partial class Configuration
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
        /// List of security groups assigned to the ECS service.
        /// </summary>
        public SortedSet<string> AdditionalECSServiceSecurityGroups { get; set; } = new SortedSet<string>();

        /// <summary>
        /// The amount of CPU to allocate to the Fargate task
        /// </summary>
        public double? TaskCpu { get; set; }

        /// <summary>
        /// The amount of memory to allocate to the Fargate task
        /// </summary>
        public double? TaskMemory { get; set; }

        public LoadBalancerConfiguration LoadBalancer { get; set; }

        public AutoScalingConfiguration AutoScaling { get; set; }

        /// <summary>
        /// The environment variables that are set for the ECS environment.
        /// </summary>
        public Dictionary<string, string> ECSEnvironmentVariables { get; set; } = new Dictionary<string, string> { };

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
            string ecsServiceName,
            ECSClusterConfiguration ecsCluster,
            VpcConfiguration vpc,
            SortedSet<string> additionalECSServiceSecurityGroups,
            LoadBalancerConfiguration loadBalancer,
            AutoScalingConfiguration autoScaling,
            Dictionary<string, string> ecsEnvironmentVariables
            )
        {
            ApplicationIAMRole = applicationIAMRole;
            ECSServiceName = ecsServiceName;
            ECSCluster = ecsCluster;
            Vpc = vpc;
            AdditionalECSServiceSecurityGroups = additionalECSServiceSecurityGroups;
            LoadBalancer = loadBalancer;
            AutoScaling = autoScaling;
            ECSEnvironmentVariables = ecsEnvironmentVariables;
        }
    }
}
