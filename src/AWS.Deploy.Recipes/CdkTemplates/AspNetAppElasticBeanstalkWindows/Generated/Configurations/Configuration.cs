// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

// This is a generated file from the original deployment recipe. It contains properties for
// all of the settings defined in the recipe file. It is recommended to not modify this file in order
// to allow easy updates to the file when the original recipe that this project was created from has updates.
// This class is marked as a partial class. If you add new settings to the recipe file, those settings should be
// added to partial versions of this class outside of the Generated folder for example in the Configuration folder.

using System.Collections.Generic;

namespace AspNetAppElasticBeanstalkWindows.Configurations
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
        public string EnvironmentName { get; set; }

        /// <summary>
        /// The Elastic Beanstalk application.
        /// </summary>
        public BeanstalkApplicationConfiguration BeanstalkApplication { get; set; }

        /// <summary>
        /// Control of IMDS v1 accessibility.
        /// </summary>
        public string IMDSv1Access { get; set; } = Recipe.IMDS_V1_DEFAULT;

        /// <summary>
        /// The name of an Elastic Beanstalk solution stack (platform version) to use with the environment.
        /// </summary>
        public string ElasticBeanstalkPlatformArn { get; set; }

        /// <summary>
        /// The type of load balancer for your environment.
        /// </summary>
        public string LoadBalancerType { get; set; } = Recipe.LOADBALANCERTYPE_APPLICATION;

        /// <summary>
        /// Whether the load balancer is visibile to the public internet ("public") or only to the connected VPC ("internal").
        /// </summary>
        public string LoadBalancerScheme { get; set; } = Recipe.LOADBALANCERSCHEME_PUBLIC;

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
        /// Specifies the IIS WebSite.
        /// </summary>
        public string IISWebSite { get; set; } = "Default Web Site";

        /// <summary>
        /// Specifies the IIS application path.
        /// </summary>
        public string IISAppPath { get; set; } = "/";

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
        public VPCConfiguration VPC { get; set; }

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
            string environmentName,
            BeanstalkApplicationConfiguration beanstalkApplication,
            string elasticBeanstalkPlatformArn,
            string ec2KeyPair,
            ElasticBeanstalkManagedPlatformUpdatesConfiguration elasticBeanstalkManagedPlatformUpdates,
            string healthCheckURL,
            ElasticBeanstalkRollingUpdatesConfiguration elasticBeanstalkRollingUpdates,
            string cnamePrefix,
            Dictionary<string, string> elasticBeanstalkEnvironmentVariables,
            VPCConfiguration vpc,
            string environmentType = Recipe.ENVIRONMENTTYPE_SINGLEINSTANCE,
            string loadBalancerType = Recipe.LOADBALANCERTYPE_APPLICATION,
            bool xrayTracingSupportEnabled = false,
            string enhancedHealthReporting = Recipe.ENHANCED_HEALTH_REPORTING,
            string loadBalancerScheme = Recipe.LOADBALANCERSCHEME_PUBLIC)
        {
            ApplicationIAMRole = applicationIAMRole;
            ServiceIAMRole = serviceIAMRole;
            InstanceType = instanceType;
            EnvironmentName = environmentName;
            BeanstalkApplication = beanstalkApplication;
            ElasticBeanstalkPlatformArn = elasticBeanstalkPlatformArn;
            EC2KeyPair = ec2KeyPair;
            ElasticBeanstalkManagedPlatformUpdates = elasticBeanstalkManagedPlatformUpdates;
            ElasticBeanstalkRollingUpdates = elasticBeanstalkRollingUpdates;
            ElasticBeanstalkEnvironmentVariables = elasticBeanstalkEnvironmentVariables;
            VPC = vpc;
            EnvironmentType = environmentType;
            LoadBalancerType = loadBalancerType;
            LoadBalancerScheme = loadBalancerScheme;
            XRayTracingSupportEnabled = xrayTracingSupportEnabled;
            EnhancedHealthReporting = enhancedHealthReporting;
            HealthCheckURL = healthCheckURL;
            CNamePrefix = cnamePrefix;
        }
    }
}
