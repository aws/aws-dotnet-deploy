// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

// This is a generated file from the original deployment recipe. It contains properties for
// all of the settings defined in the recipe file. It is recommended to not modify this file in order
// to allow easy updates to the file when the original recipe that this project was created from has updates.
// This class is marked as a partial class. If you add new settings to the recipe file, those settings should be
// added to partial versions of this class outside of the Generated folder for example in the Configuration folder.

namespace AspNetAppElasticBeanstalkLinux.Configurations
{
    public partial class Configuration
    {
        /// <summary>
        /// The Identity and Access Management Role that provides AWS credentials to the application to access AWS services
        /// </summary>
        public IAMRoleConfiguration ApplicationIAMRole { get; set; }

        /// <summary>
        /// The type of environment for the Elastic Beanstalk application.
        /// </summary>
        public string EnvironmentType { get; set; } = Recipe.ENVIRONMENTTYPE_SINGLEINSTANCE;

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
        public string LoadBalancerType { get; set; } = Recipe.LOADBALANCERTYPE_APPLICATION;

        /// <summary>
        /// The EC2 Key Pair used for the Beanstalk Application.
        /// </summary>
        public string EC2KeyPair { get; set; }

        /// <summary>
        /// Specifies whether to enable or disable Managed Platform Updates.
        /// </summary>
        public ElasticBeanstalkManagedPlatformUpdatesConfiguration ElasticBeanstalkManagedPlatformUpdates { get; set; }

        /// A parameterless constructor is needed for <see cref="Microsoft.Extensions.Configuration.ConfigurationBuilder"/>
        /// or the classes will fail to initialize.
        /// The warnings are disabled since a parameterless constructor will allow non-nullable properties to be initialized with null values.
#nullable disable warnings
        public Configuration()
        {

        }
#nullable restore warnings

        public Configuration(
            IAMRoleConfiguration applicationIAMRole,
            string instanceType,
            string environmentName,
            BeanstalkApplicationConfiguration beanstalkApplication,
            string elasticBeanstalkPlatformArn,
            string ec2KeyPair,
            ElasticBeanstalkManagedPlatformUpdatesConfiguration elasticBeanstalkManagedPlatformUpdates,
            string environmentType = Recipe.ENVIRONMENTTYPE_SINGLEINSTANCE,
            string loadBalancerType = Recipe.LOADBALANCERTYPE_APPLICATION)
        {
            ApplicationIAMRole = applicationIAMRole;
            InstanceType = instanceType;
            EnvironmentName = environmentName;
            BeanstalkApplication = beanstalkApplication;
            ElasticBeanstalkPlatformArn = elasticBeanstalkPlatformArn;
            EC2KeyPair = ec2KeyPair;
            ElasticBeanstalkManagedPlatformUpdates = elasticBeanstalkManagedPlatformUpdates;
            EnvironmentType = environmentType;
            LoadBalancerType = loadBalancerType;
        }
    }
}
