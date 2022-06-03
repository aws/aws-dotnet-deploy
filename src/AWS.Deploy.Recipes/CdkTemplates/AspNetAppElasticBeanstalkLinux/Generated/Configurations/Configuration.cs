// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

// This is a generated file from the original deployment recipe. It contains properties for
// all of the settings defined in the recipe file. It is recommended to not modify this file in order
// to allow easy updates to the file when the original recipe that this project was created from has updates.
// This class is marked as a partial class. If you add new settings to the recipe file, those settings should be
// added to partial versions of this class outside of the Generated folder for example in the Configuration folder.

using System.Collections.Generic;

namespace AspNetAppElasticBeanstalkLinux.Configurations
{
    public partial class Configuration
    {
        /// <summary>
        /// The Identity and Access Management Role that provides AWS credentials to the application to access AWS services
        /// </summary>
        public IAMRoleConfiguration ApplicationIAMRole { get; set; }

        /// <summary>
        /// A service role is the IAM role that Elastic Beanstalk assumes when calling other services on your behalf
        /// </summary>
        public IAMRoleConfiguration ServiceIAMRole { get; set; }

        /// <summary>
        /// The type of environment for the Elastic Beanstalk application.
        /// </summary>
        public string EnvironmentType { get; set; } = Recipe.ENVIRONMENTTYPE_SINGLEINSTANCE;

        /// <summary>
        /// The EC2 instance type used for the EC2 instances created for the environment.
        /// </summary>
        public string InstanceType { get; set; }

        /// <summary>
        /// The Elastic Beanstalk environment.
        /// </summary>
        public BeanstalkEnvironmentConfiguration BeanstalkEnvironment { get; set; }

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

        /// <summary>
        /// Specifies whether to enable or disable AWS X-Ray tracing support.
        /// </summary>
        public bool XRayTracingSupportEnabled { get; set; } = false;

        /// <summary>
        /// The reverse proxy to use. 
        /// </summary>
        public string ReverseProxy { get; set; } = Recipe.REVERSEPROXY_NGINX;
        
        /// <summary>
        /// Specifies whether to enable or disable enhanced health reporting.
        /// </summary>
        public string EnhancedHealthReporting { get; set; } = Recipe.ENHANCED_HEALTH_REPORTING;

        /// <summary>
        /// The health check URL to use.
        /// </summary>
        public string HealthCheckURL { get; set; }
        
        /// <summary>
        /// Specifies whether to enable or disable Rolling Updates.
        /// </summary>
        public ElasticBeanstalkRollingUpdatesConfiguration ElasticBeanstalkRollingUpdates { get; set; }

        /// <summary>
        /// The CName Prefix used for the Beanstalk Environment.
        /// </summary>
        public string CNamePrefix { get; set; }

        /// <summary>
        /// The environment variables that are set for the beanstalk environment.
        /// </summary>
        public Dictionary<string, string> ElasticBeanstalkEnvironmentVariables { get; set; } = new Dictionary<string, string> { };

        /// <summary>
        /// Virtual Private Cloud to launch container instance into a virtual network.
        /// </summary>
        public string VpcId { get; set; }

        /// <summary>
        /// A list of IDs of subnets that Elastic Beanstalk should use when it associates your environment with a custom Amazon VPC.
        /// Specify IDs of subnets of a single Amazon VPC.
        /// </summary>
        public SortedSet<string> Subnets { get; set; } = new SortedSet<string>();

        /// <summary>
        /// Lists the Amazon EC2 security groups to assign to the EC2 instances in the Auto Scaling group to define firewall rules for the instances.
        /// </summary>
        public SortedSet<string> SecurityGroups { get; set; } = new SortedSet<string>();

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
            IAMRoleConfiguration serviceIAMRole,
            string instanceType,
            BeanstalkEnvironmentConfiguration beanstalkEnvironment,
            BeanstalkApplicationConfiguration beanstalkApplication,
            string elasticBeanstalkPlatformArn,
            string ec2KeyPair,
            ElasticBeanstalkManagedPlatformUpdatesConfiguration elasticBeanstalkManagedPlatformUpdates,
            string healthCheckURL,
            ElasticBeanstalkRollingUpdatesConfiguration elasticBeanstalkRollingUpdates,
            string cnamePrefix,
            Dictionary<string, string> elasticBeanstalkEnvironmentVariables,
            string vpcId,
            SortedSet<string> subnets,
            SortedSet<string> securityGroups,
            string environmentType = Recipe.ENVIRONMENTTYPE_SINGLEINSTANCE,
            string loadBalancerType = Recipe.LOADBALANCERTYPE_APPLICATION,
            string reverseProxy = Recipe.REVERSEPROXY_NGINX,
            bool xrayTracingSupportEnabled = false,
            string enhancedHealthReporting = Recipe.ENHANCED_HEALTH_REPORTING)
        {
            ApplicationIAMRole = applicationIAMRole;
            ServiceIAMRole = serviceIAMRole;
            InstanceType = instanceType;
            BeanstalkEnvironment = beanstalkEnvironment;
            BeanstalkApplication = beanstalkApplication;
            ElasticBeanstalkPlatformArn = elasticBeanstalkPlatformArn;
            EC2KeyPair = ec2KeyPair;
            ElasticBeanstalkManagedPlatformUpdates = elasticBeanstalkManagedPlatformUpdates;
            ElasticBeanstalkRollingUpdates = elasticBeanstalkRollingUpdates;
            ElasticBeanstalkEnvironmentVariables = elasticBeanstalkEnvironmentVariables;
            VpcId = vpcId;
            Subnets = subnets;
            SecurityGroups = securityGroups;
            EnvironmentType = environmentType;
            LoadBalancerType = loadBalancerType;
            XRayTracingSupportEnabled = xrayTracingSupportEnabled;
            ReverseProxy = reverseProxy;
            EnhancedHealthReporting = enhancedHealthReporting;
            HealthCheckURL = healthCheckURL;
            CNamePrefix = cnamePrefix;
        }
    }
}
