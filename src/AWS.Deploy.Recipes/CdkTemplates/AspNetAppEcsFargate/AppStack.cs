using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.IAM;
using AWS.Deploy.Recipes.CDK.Common;
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

            IRole taskRole;
            if (settings.ApplicationIAMRole.CreateNew)
            {
                taskRole = new Role(this, "TaskRole", new RoleProps
                {
                    AssumedBy = new ServicePrincipal("ecs-tasks.amazonaws.com")
                });
            }
            else
            {
                taskRole = Role.FromRoleArn(this, "TaskRole", settings.ApplicationIAMRole.RoleArn, new FromRoleArnOptions {
                    Mutable = false
                });
            }

            var taskDefinition = new FargateTaskDefinition(this, "TaskDefinition", new FargateTaskDefinitionProps
            {
                TaskRole = taskRole,
                Cpu = settings.TaskCpu,
                MemoryLimitMiB = settings.TaskMemory
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
                })
            });

            container.AddPortMappings(new PortMapping
            {
                ContainerPort = 80,
                Protocol = Protocol.TCP
            });

            var ecsLoadBalancerAccessSecurityGroup = new SecurityGroup(this, "WebAccessSecurityGroup", new SecurityGroupProps
            {
                Vpc = vpc,
                SecurityGroupName = $"{recipeConfiguration.StackName}-ECSService"
            });

            var ecsServiceSecurityGroups = new List<ISecurityGroup>();
            ecsServiceSecurityGroups.Add(ecsLoadBalancerAccessSecurityGroup);

            if (!string.IsNullOrEmpty(settings.AdditionalECSServiceSecurityGroups))
            {
                var count = 1;
                foreach (var securityGroupId in settings.AdditionalECSServiceSecurityGroups.Split(','))
                {
                    ecsServiceSecurityGroups.Add(SecurityGroup.FromSecurityGroupId(this, $"AdditionalGroup-{count++}", securityGroupId.Trim(), new SecurityGroupImportOptions
                    {
                        Mutable = false
                    }));
                }
            }

            new ApplicationLoadBalancedFargateService(this, "FargateService", new ApplicationLoadBalancedFargateServiceProps
            {
                Cluster = cluster,
                TaskDefinition = taskDefinition,
                DesiredCount = settings.DesiredCount,
                ServiceName = settings.ECSServiceName,
                AssignPublicIp = settings.Vpc.IsDefault,
                SecurityGroups = ecsServiceSecurityGroups.ToArray()
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
