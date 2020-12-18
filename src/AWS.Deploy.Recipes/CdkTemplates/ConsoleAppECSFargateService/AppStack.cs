using Amazon.CDK;
using Amazon.CDK.AWS.AutoScaling;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.IAM;
using System.IO;
using Protocol = Amazon.CDK.AWS.ECS.Protocol;

namespace ConsoleAppEcsFargateService
{
    public class AppStack : Stack
    {
        internal AppStack(Construct scope, string id, Configuration configuration, IStackProps props = null) : base(scope, id, props)
        {
            var vpc = new Vpc(this, "Vpc", new VpcProps
            {
                MaxAzs = 2
            });

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

            var dockerExecutionDirectory = string.Empty;
            if (string.IsNullOrEmpty(configuration.ProjectSolutionPath))
            {
                dockerExecutionDirectory = new FileInfo(configuration.DockerfileDirectory).FullName;
            }
            else
            {
                dockerExecutionDirectory = new FileInfo(configuration.ProjectSolutionPath).Directory.FullName;
            }
            var relativePath = Path.GetRelativePath(dockerExecutionDirectory, configuration.DockerfileDirectory);
            var container = taskDefinition.AddContainer("Container", new ContainerDefinitionOptions
            {
                Image = ContainerImage.FromAsset(dockerExecutionDirectory, new AssetImageProps
                {
                    File = Path.Combine(relativePath, configuration.DockerfileName)
                }),
                Logging = logging
            });

            new FargateService(this, "FargateService", new FargateServiceProps
            {
                Cluster = cluster,
                TaskDefinition = taskDefinition,
            });
        }
    }
}
