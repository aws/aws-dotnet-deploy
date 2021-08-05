// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

// This is a generated file from the original deployment recipe. It contains properties for
// all of the settings defined in the recipe file. It is recommended to not modify this file in order
// to allow easy updates to the file when the original recipe that this project was created from has updates.
// This class is marked as a partial class. If you add new settings to the recipe file, those settings should be
// added to partial versions of this class outside of the Generated folder for example in the Configuration folder.

namespace AspNetAppEcsFargate.Configurations
{
    public partial class ECSClusterConfiguration
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
