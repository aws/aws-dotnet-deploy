// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

// This is a generated file from the original deployment recipe. It contains properties for
// all of the settings defined in the recipe file. It is recommended to not modify this file in order
// to allow easy updates to the file when the original recipe that this project was created from has updates.
// This class is marked as a partial class. If you add new settings to the recipe file, those settings should be
// added to partial versions of this class outside of the Generated folder for example in the Configuration folder.

using System.Collections.Generic;

namespace AspNetAppAppRunner.Configurations
{
    public partial class VPCConnectorConfiguration
    {
        /// <summary>
        /// If set, creates a new VPC Connector to connect to the AppRunner service.
        /// </summary>
        public bool CreateNew { get; set; }

        /// <summary>
        /// The VPC Connector to use for the App Runner service.
        /// </summary>
        public string? VpcConnectorId { get; set; }

        /// <summary>
        /// The VPC ID to use for the App Runner service.
        /// </summary>
        public string? VpcId { get; set; }

        /// <summary>
        /// A list of IDs of subnets that App Runner should use when it associates your service with a custom Amazon VPC.
        /// Specify IDs of subnets of a single Amazon VPC. App Runner determines the Amazon VPC from the subnets you specify.
        /// </summary>
        public SortedSet<string> Subnets { get; set; } = new SortedSet<string>();

        /// <summary>
        /// A list of IDs of security groups that App Runner should use for access to AWS resources under the specified subnets.
        /// If not specified, App Runner uses the default security group of the Amazon VPC.
        /// The default security group allows all outbound traffic.
        /// </summary>
        public SortedSet<string> SecurityGroups { get; set; } = new SortedSet<string>();
    }
}
