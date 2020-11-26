using System.Collections.Specialized;
using Amazon.CDK.AWS.S3;
using Microsoft.Extensions.Configuration;

namespace ASPNETCoreElasticBeanstalkLinux
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

        public string EnvironmentType { get; set; }

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

        public Configuration(IConfiguration root)
        {
            StackName = root[nameof(StackName)];
            ProjectPath = root[nameof(ProjectPath)];
            ApplicationName = root[nameof(ApplicationName)];
            EnvironmentName = root[nameof(EnvironmentName)];
            InstanceType = root[nameof(InstanceType)];
            EnvironmentType = root[nameof(EnvironmentType)];
            ApplicationIAMRole = root[nameof(ApplicationIAMRole)];
        }
    }
}