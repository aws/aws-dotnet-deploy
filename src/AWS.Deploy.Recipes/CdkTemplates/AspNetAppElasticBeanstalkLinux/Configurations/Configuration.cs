// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AspNetAppElasticBeanstalkLinux.Configurations
{
    public class Configuration
    {
        /// <summary>
        /// The Identity and Access Management Role that provides AWS credentials to the application to access AWS services
        /// </summary>
        public IAMRoleConfiguration ApplicationIAMRole { get; set; }

        /// <summary>
        /// The type of environment for the Elastic Beanstalk application.
        /// </summary>
        public string EnvironmentType { get; set; } = "SingleInstance";

        /// <summary>
        /// The EC2 instance type used for the EC2 instances created for the environment.
        /// </summary>
        public string InstanceType { get; set; }

        /// <summary>
        /// The Elastic Beanstalk environment name.
        /// </summary>
        public string EnvironmentName { get; set; }

        /// <summary>
        /// The Elastic Beanstalk application.
        /// </summary>
        public BeanstalkApplicationConfiguration BeanstalkApplication { get; set; }

        /// <summary>
        /// The name of an Elastic Beanstalk solution stack (platform version) to use with the environment.
        /// </summary>
        public string ElasticBeanstalkPlatformArn { get; set; }

        /// <summary>
        /// The type of load balancer for your environment.
        /// </summary>
        public string LoadBalancerType { get; set; } = "application";

        /// <summary>
        /// The EC2 Key Pair used for the Beanstalk Application.
        /// </summary>
        public string EC2KeyPair { get; set; }
    }
}
