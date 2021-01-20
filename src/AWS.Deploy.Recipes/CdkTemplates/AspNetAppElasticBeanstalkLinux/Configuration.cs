using System.Collections.Specialized;
using Amazon.CDK.AWS.S3;
using Microsoft.Extensions.Configuration;

namespace AspNetAppElasticBeanstalkLinux
{
    public class Configuration
    {
        /// <summary>
        /// The name of the CloudFormation Stack to create or update.
        /// </summary>
        public string StackName { get; set; }
        
        /// <summary>
        /// The path of csproj file to be deployed.
        /// </summary>
        public string ProjectPath { get; set; }

        /// <summary>
        /// The path of archive file to be deployed.
        /// </summary>
        public string AssetPath { get; set; }

        /// <summary>
        /// The Identity and Access Management Role that provides AWS credentials to the application to access AWS services
        /// </summary>
        public string ApplicationIAMRole { get; set; }

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
        /// The Elastic Beanstalk environment name.
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Latest 64bit Amazon Linux 2 running .NET Core.
        /// </summary>
        public string SolutionStackName { get; set; }

        /// <summary>
        /// The type of load balancer for your environment.
        /// </summary>
        public string LoadBalancerType { get; set; } = "application";
    }
}
