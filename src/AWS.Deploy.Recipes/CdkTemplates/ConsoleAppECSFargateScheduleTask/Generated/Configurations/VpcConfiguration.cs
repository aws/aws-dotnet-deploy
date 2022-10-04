// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

// This is a generated file from the original deployment recipe. It contains properties for
// all of the settings defined in the recipe file. It is recommended to not modify this file in order
// to allow easy updates to the file when the original recipe that this project was created from has updates.
// This class is marked as a partial class. If you add new settings to the recipe file, those settings should be
// added to partial versions of this class outside of the Generated folder for example in the Configuration folder.

using System.Collections.Generic;

namespace ConsoleAppECSFargateScheduleTask.Configurations
{
    public partial class VpcConfiguration
    {
        /// <summary>
        /// If set, use default VPC
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// If set and <see cref="CreateNew"/> is false, create a new VPC
        /// </summary>
        public bool CreateNew { get; set; }

        /// <summary>
        /// If <see cref="IsDefault" /> is false and <see cref="CreateNew" /> is false,
        /// then use an existing VPC by referencing through <see cref="VpcId"/>
        /// </summary>
        public string VpcId { get; set; }

        /// <summary>
        /// A list of IDs of the subnets that will be associated with the Fargate service.
        /// Specify IDs of subnets from a single Amazon VPC.
        /// </summary>
        public SortedSet<string> Subnets { get; set; } = new SortedSet<string>();

        /// A parameterless constructor is needed for <see cref="Microsoft.Extensions.Configuration.ConfigurationBuilder"/>
        /// or the classes will fail to initialize.
        /// The warnings are disabled since a parameterless constructor will allow non-nullable properties to be initialized with null values.
#nullable disable warnings
        public VpcConfiguration()
        {

        }
#nullable restore warnings

        public VpcConfiguration(
            bool isDefault,
            bool createNew,
            string vpcId)
        {
            IsDefault = isDefault;
            CreateNew = createNew;
            VpcId = vpcId;
        }
    }
}
