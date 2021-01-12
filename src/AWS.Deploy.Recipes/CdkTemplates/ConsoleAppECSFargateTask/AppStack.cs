using System.IO;
using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.IAM;
using Schedule = Amazon.CDK.AWS.ApplicationAutoScaling.Schedule;

namespace ConsoleAppEcsFargateTask
{
    public class AppStack : Stack
    {
        internal AppStack(Construct scope, string id, Configuration configuration, IStackProps props = null) : base(scope, id, props)
        {
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


#if (UseExistingECSCluster)
            var cluster = Cluster.FromClusterAttributes(this, "Cluster", new ClusterAttributes
            {
                ClusterArn = configuration.ExistingClusterArn,
                // ClusterName is required field, but is ignored
                ClusterName = ""
                SecurityGroups = new ISecurityGroup[0],
                Vpc = vpc
            });
#else
            var cluster = new Cluster(this, "Cluster", new ClusterProps
            {
                Vpc = vpc,
                ClusterName = configuration.NewClusterName
            });
#endif

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
                Cpu = configuration.CpuLimit,
                MemoryLimitMiB = configuration.MemoryLimit
            });

            var logging = new AwsLogDriver(new AwsLogDriverProps
            {
                StreamPrefix = configuration.StackName
            });

            var dockerExecutionDirectory = new FileInfo(configuration.DockerfileDirectory).FullName;
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
                Logging = logging,
                Environment = configuration.EnvironmentVariables
            });

            container.AddPortMappings(configuration.PortMappings);

            new ScheduledFargateTask(this, "FargateService", new ScheduledFargateTaskProps
            {
                Cluster = cluster,
                DesiredTaskCount = configuration.DesiredTaskCount,
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
