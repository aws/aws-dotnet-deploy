// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;

namespace AspNetAppElasticBeanstalkWindows.Configurations
{
    public partial class VPCConfiguration
    {
        /// <summary>
        /// If set, the deployment will use a VPC to connect to the Elastic Beanstalk service.
        /// </summary>
        public bool UseVPC { get; set; }

        /// <summary>
        /// The VPC ID to use for the Elastic Beanstalk service.
        /// </summary>
        public string? VpcId { get; set; }

        /// <summary>
        /// A list of IDs of subnets that Elastic Beanstalk should use when it associates your environment with a custom Amazon VPC.
        /// Specify IDs of subnets of a single Amazon VPC.
        /// </summary>
        public SortedSet<string> Subnets { get; set; } = new SortedSet<string>();

        /// <summary>
        /// Lists the Amazon EC2 security groups to assign to the EC2 instances in the Auto Scaling group to define firewall rules for the instances.
        /// </summary>
        public SortedSet<string> SecurityGroups { get; set; } = new SortedSet<string>();
    }
}
