using Amazon.CDK;
using Amazon.CDK.AWS.AutoScaling;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.IAM;
using AWS.Deploy.Recipes.CDK.Common;
using System.IO;
using System.Collections.Generic;
using ConsoleAppEcsFargateService.Configurations;
using Protocol = Amazon.CDK.AWS.ECS.Protocol;

namespace ConsoleAppEcsFargateService
{
    public class AppStack : Stack
    {
        internal AppStack(Construct scope, RecipeConfiguration<Configuration> recipeConfiguration, IStackProps props = null)
            : base(scope, recipeConfiguration.StackName, props)
        {
            var settings = recipeConfiguration.Settings;

            IVpc vpc;
            if (settings.Vpc.IsDefault)
            {
                vpc = Vpc.FromLookup(this, "Vpc", new VpcLookupOptions
                {
                    IsDefault = true
                });
            }
            else if (settings.Vpc.CreateNew)
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
                    VpcId = settings.Vpc.VpcId
                });
            }

            var cluster = new Cluster(this, "Cluster", new ClusterProps
            {
                Vpc = vpc,
                ClusterName = settings.ClusterName
            });

            IRole executionRole;
            if (settings.ApplicationIAMRole.CreateNew)
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
                executionRole = Role.FromRoleArn(this, "ExecutionRole", settings.ApplicationIAMRole.RoleArn, new FromRoleArnOptions {
                    Mutable = false
                });
            }

            var taskDefinition = new FargateTaskDefinition(this, "TaskDefinition", new FargateTaskDefinitionProps
            {
                ExecutionRole = executionRole,
            });

            var logging = new AwsLogDriver(new AwsLogDriverProps
            {
                StreamPrefix = recipeConfiguration.StackName
            });

            var dockerExecutionDirectory = @"DockerExecutionDirectory-Placeholder";
            if (string.IsNullOrEmpty(dockerExecutionDirectory))
            {
                if (string.IsNullOrEmpty(recipeConfiguration.ProjectSolutionPath))
                {
                    dockerExecutionDirectory = new FileInfo(recipeConfiguration.DockerfileDirectory).FullName;
                }
                else
                {
                    dockerExecutionDirectory = new FileInfo(recipeConfiguration.ProjectSolutionPath).Directory.FullName;
                }
            }
            var relativePath = Path.GetRelativePath(dockerExecutionDirectory, recipeConfiguration.DockerfileDirectory);
            var container = taskDefinition.AddContainer("Container", new ContainerDefinitionOptions
            {
                Image = ContainerImage.FromAsset(dockerExecutionDirectory, new AssetImageProps
                {
                    File = Path.Combine(relativePath, settings.DockerfileName),
#if (AddDockerBuildArgs)
                    BuildArgs = GetDockerBuildArgs("DockerBuildArgs-Placeholder")
#endif
                }),
                Logging = logging
            });

            new FargateService(this, "FargateService", new FargateServiceProps
            {
                Cluster = cluster,
                TaskDefinition = taskDefinition,
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
