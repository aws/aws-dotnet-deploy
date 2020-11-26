using System.IO;
using Microsoft.Extensions.Configuration;

namespace ASPNETCoreECSFargate
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
        /// The path of directory that contains the Dockerfile.
        /// </summary>
        public string DockerfileDirectory { get; set; }

        /// <summary>
        /// The Identity and Access Management Role that provides AWS credentials to the application to access AWS services.
        /// </summary>
        public string ApplicationIAMRole { get; set; }

        /// <summary>
        /// The desired number of ECS tasks to run for the service.
        /// </summary>
        public double DesiredCount { get; set; }

        /// <summary>
        /// The name of the ECS service running in the cluster.
        /// </summary>
        public string ECSServiceName { get; set; }

        /// <summary>
        /// The name of the ECS cluster.
        /// </summary>
        public string ClusterName { get; set; }

        public Configuration(IConfiguration root)
        {
            StackName = root[nameof(StackName)];
            ProjectPath = root[nameof(ProjectPath)];
            ClusterName = root[nameof(ClusterName)];
            ECSServiceName = root[nameof(ECSServiceName)];
            DesiredCount = double.Parse(root[nameof(DesiredCount)]);
            ApplicationIAMRole = root[nameof(ApplicationIAMRole)];
            var projectFileInfo = new FileInfo(ProjectPath);
            DockerfileDirectory = projectFileInfo.Directory.FullName;
        }
    }
}