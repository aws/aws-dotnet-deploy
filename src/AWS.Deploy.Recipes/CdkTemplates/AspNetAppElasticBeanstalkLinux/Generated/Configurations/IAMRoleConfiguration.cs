// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

// This is a generated file from the original deployment recipe. It contains properties for
// all of the settings defined in the recipe file. It is recommended to not modify this file in order
// to allow easy updates to the file when the original recipe that this project was created from has updates.
// This class is marked as a partial class. If you add new settings to the recipe file, those settings should be
// added to partial versions of this class outside of the Generated folder for example in the Configuration folder.

namespace AspNetAppElasticBeanstalkLinux.Configurations
{
    public partial class IAMRoleConfiguration
    {
        /// <summary>
        /// If set, create a new anonymously named IAM role.
        /// </summary>
        public bool CreateNew { get; set; }

        /// <summary>
        /// If <see cref="CreateNew"/> is false,
        /// then use an existing IAM role by referencing through <see cref="RoleArn"/>
        /// </summary>
        public string? RoleArn { get; set; }
    }
}
