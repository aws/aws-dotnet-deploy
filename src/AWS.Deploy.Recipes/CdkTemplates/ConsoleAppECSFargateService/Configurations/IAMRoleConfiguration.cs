// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

namespace ConsoleAppEcsFargateService.Configurations
{
    public class IAMRoleConfiguration
    {
        /// <summary>
        /// If set, create a new anonymously named IAM role.
        /// </summary>
        public bool CreateNew { get; set; }

        /// <summary>
        /// If <see cref="CreateNew"/> is false,
        /// then use an existing IAM role by referencing through <see cref="RoleArn"/>
        /// </summary>
        public string RoleArn { get; set; }
    }
}
