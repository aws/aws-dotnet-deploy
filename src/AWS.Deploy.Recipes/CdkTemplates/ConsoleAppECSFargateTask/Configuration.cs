using System.Collections.Generic;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;

namespace ConsoleAppEcsFargateTask
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
        /// The schedule or rate (frequency) that determines when CloudWatch Events runs the rule.
        /// </summary>
        public string Schedule { get; set; }

        /// <inheritdoc cref="ClusterProps.ClusterName"/>
        /// <remarks>
        /// This is only consumed if the deployment is configured
        /// to use a new cluster.  Otherwise, <see cref="ExistingClusterArn"/>
        /// will be used.
        /// </remarks>
        public string NewClusterName { get; set; }

        /// <inheritdoc cref="ClusterAttributes.ClusterName"/>
        /// <remarks>
        /// This is only consumed if the deployment is configured
        /// to use an existing cluster.  Otherwise, <see cref="NewClusterName"/>
        /// will be used.
        /// </remarks>
        public string ExistingClusterArn { get; set; }

        /// <inheritdoc cref="FargateTaskDefinitionProps.Cpu"/>
        public double? CpuLimit { get; set; }
        
        /// <inheritdoc cref="FargateTaskDefinitionProps.MemoryLimitMiB"/>
        public double? MemoryLimit { get; set; }
        
        /// <inheritdoc cref="ScheduledTaskBase.DesiredTaskCount"/>
        public double? DesiredTaskCount { get; set; }

        /// <inheritdoc cref="ContainerDefinition.PortMappings"/>
        public PortMapping[] PortMappings { get; set; } = new PortMapping[0];

        /// <inheritdoc cref="ContainerDefinitionOptions.Environment"/>
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();
    }
}
