// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace ConsoleAppEcsFargateService.Configurations
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

        /// A parameterless constructor is needed for <see cref="Microsoft.Extensions.Configuration.ConfigurationBuilder"/>
        /// or the classes will fail to initialize.
        /// The warnings are disabled since a parameterless constructor will allow non-nullable properties to be initialized with null values.
#nullable disable warnings
        public ECSClusterConfiguration()
        {

        }
#nullable restore warnings

        public ECSClusterConfiguration(
            bool createNew,
            string clusterArn,
            string newClusterName)
        {
            CreateNew = createNew;
            ClusterArn = clusterArn;
            NewClusterName = newClusterName;
        }
    }
}
