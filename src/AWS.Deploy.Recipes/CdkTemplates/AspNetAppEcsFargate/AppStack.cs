using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.IAM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AspNetAppEcsFargate.Configurations;
using Protocol = Amazon.CDK.AWS.ECS.Protocol;

namespace AspNetAppEcsFargate
{
    public class AppStack : Stack
    {
        /// <summary>
        /// Tag key of the CloudFormation stack
        /// used to uniquely identify a stack that is deployed by aws-dotnet-deploy
        /// </summary>
        private const string STACK_TAG_KEY = "StackTagKey-Placeholder";

        internal AppStack(Construct scope, string id, Configuration configuration, IStackProps props = null) : base(scope, id, props)
        {
            Tags.SetTag(STACK_TAG_KEY, "true");

            IVpc vpc;
            if (configuration.Vpc.IsDefault)
            {
                vpc = Vpc.FromLookup(this, "Vpc", new VpcLookupOptions
                {
                    IsDefault = true
                });
            }
            else if (configuration.Vpc.CreateNew)
            {
                vpc = new Vpc(this, "Vpc", new VpcProps
                {
                    MaxAzs = 2
                });
            }
            else
            {
                vpc = Vpc.FromLookup(this, "Vpc", new VpcLookupOptions
                {
                    VpcId = configuration.Vpc.VpcId
                });
            }

            var cluster = new Cluster(this, "Cluster", new ClusterProps
            {
                Vpc = vpc,
                ClusterName = configuration.ClusterName
            });

            IRole executionRole;
            if (configuration.ApplicationIAMRole.CreateNew)
            {
                executionRole = new Role(this, "ExecutionRole", new RoleProps
                {
                    AssumedBy = new ServicePrincipal("ecs-tasks.amazonaws.com"),
                    ManagedPolicies = new[]
                    {
                        ManagedPolicy.FromAwsManagedPolicyName("service-role/AmazonECSTaskExecutionRolePolicy"),
                    }
                });
            }
            else
            {
                executionRole = Role.FromRoleArn(this, "ExecutionRole", configuration.ApplicationIAMRole.RoleArn, new FromRoleArnOptions {
                    Mutable = false
                });
            }

            var taskDefinition = new FargateTaskDefinition(this, "TaskDefinition", new FargateTaskDefinitionProps
            {
                ExecutionRole = executionRole,
            });

            var dockerExecutionDirectory = @"DockerExecutionDirectory-Placeholder";
            if (string.IsNullOrEmpty(dockerExecutionDirectory))
            {
                if (string.IsNullOrEmpty(configuration.ProjectSolutionPath))
                {
                    dockerExecutionDirectory = new FileInfo(configuration.DockerfileDirectory).FullName;
                }
                else
                {
                    dockerExecutionDirectory = new FileInfo(configuration.ProjectSolutionPath).Directory.FullName;
                }
            }
            var relativePath = Path.GetRelativePath(dockerExecutionDirectory, configuration.DockerfileDirectory);
            var container = taskDefinition.AddContainer("Container", new ContainerDefinitionOptions
            {
                Image = ContainerImage.FromAsset(dockerExecutionDirectory, new AssetImageProps
                {
                    File = Path.Combine(relativePath, configuration.DockerfileName),
#if (AddDockerBuildArgs)
                    BuildArgs = GetDockerBuildArgs("DockerBuildArgs-Placeholder")
#endif
                })
            });

            container.AddPortMappings(new PortMapping
            {
                ContainerPort = 80,
                Protocol = Protocol.TCP
            });

            new ApplicationLoadBalancedFargateService(this, "FargateService", new ApplicationLoadBalancedFargateServiceProps
            {
                Cluster = cluster,
                TaskDefinition = taskDefinition,
                DesiredCount = configuration.DesiredCount,
                ServiceName = configuration.ECSServiceName
            });
        }

#if (AddDockerBuildArgs)
        private Dictionary<string, string> GetDockerBuildArgs(string buildArgsString)
        {
            return buildArgsString
                .Split(',')
                .Where(x => x.Contains("="))
                .ToDictionary(
                    k => k.Split('=')[0],
                    v => v.Split('=')[1]
                );
        }
#endif
    }
}
