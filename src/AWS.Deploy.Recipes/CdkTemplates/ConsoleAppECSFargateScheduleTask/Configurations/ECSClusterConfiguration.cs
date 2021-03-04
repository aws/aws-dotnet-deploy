// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace ConsoleAppECSFargateScheduleTask.Configurations
{
    public class ECSClusterConfiguration
    {
        /// <summary>
        /// Indicates whether to create a new ECS Cluster or use and existing one
        /// </summary>
        public bool CreateNew { get; set; }

        /// <summary>
        /// If <see cref="CreateNew" /> is false,
        /// then use an existing ECS Cluster by referencing through <see cref="ClusterArn"/>
        /// </summary>
        public string ClusterArn { get; set; }

        /// <summary>
        /// If <see cref="CreateNew" /> is true,
        /// then create a new ECS Cluster with the name <see cref="NewClusterName"/>
        /// </summary>
        public string NewClusterName { get; set; }
    }
}
