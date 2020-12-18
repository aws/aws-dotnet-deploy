using System.IO;
using Microsoft.Extensions.Configuration;

namespace AspNetAppEcsFargate
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
        /// The path of sln file to be deployed.
        /// </summary>
        public string ProjectSolutionPath { get; set; }

        /// <summary>
        /// The path of directory that contains the Dockerfile.
        /// </summary>
        public string DockerfileDirectory { get; set; }

        /// <summary>
        /// The file name of the Dockerfile.
        /// </summary>
        public string DockerfileName { get; set; } = "Dockerfile";

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
    }
}
