using Amazon.CDK;
using Amazon.CDK.AWS.AutoScaling;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.IAM;
using System.IO;
using System.Collections.Generic;
using Protocol = Amazon.CDK.AWS.ECS.Protocol;
using Schedule = Amazon.CDK.AWS.ApplicationAutoScaling.Schedule;

namespace ConsoleAppEcsFargateTask
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

#if (UseExistingVPC)
            var vpc = Vpc.FromLookup(this, "Vpc", new VpcLookupOptions
            {
    #if (UseDefaultVPC)
                IsDefault = true
    #else
                VpcId = "VPC-Placeholder"
    #endif
            });
#else
            var vpc = new Vpc(this, "Vpc", new VpcProps
            {
                MaxAzs = 2
            });
#endif

            var cluster = new Cluster(this, "Cluster", new ClusterProps
            {
                Vpc = vpc,
                ClusterName = configuration.ClusterName
            });

            var executionRole = new Role(this, "ExecutionRole", new RoleProps
            {
                AssumedBy = new ServicePrincipal("ecs-tasks.amazonaws.com"),
                RoleName = configuration.ApplicationIAMRole,
                ManagedPolicies = new[]
                {
                    ManagedPolicy.FromAwsManagedPolicyName("service-role/AmazonECSTaskExecutionRolePolicy"),
                }
            });

            var taskDefinition = new FargateTaskDefinition(this, "TaskDefinition", new FargateTaskDefinitionProps
            {
                ExecutionRole = executionRole,
            });

            var logging = new AwsLogDriver(new AwsLogDriverProps
            {
                StreamPrefix = configuration.StackName
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
                }),
                Logging = logging
            });

            new ScheduledFargateTask(this, "FargateService", new ScheduledFargateTaskProps
            {
                Cluster = cluster,
                Schedule = Schedule.Expression(configuration.Schedule),
                Vpc = vpc,
                ScheduledFargateTaskDefinitionOptions = new ScheduledFargateTaskDefinitionOptions
                {
                    TaskDefinition = taskDefinition
                }
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
