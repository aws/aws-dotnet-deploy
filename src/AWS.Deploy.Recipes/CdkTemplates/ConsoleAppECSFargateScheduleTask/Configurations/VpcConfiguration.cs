// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

namespace ConsoleAppECSFargateScheduleTask.Configurations
{
    public class VpcConfiguration
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
