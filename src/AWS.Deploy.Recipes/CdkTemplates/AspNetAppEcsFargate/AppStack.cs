// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.IAM;
using AWS.Deploy.Recipes.CDK.Common;
using AspNetAppEcsFargate.Configurations;
using Protocol = Amazon.CDK.AWS.ECS.Protocol;
using Amazon.CDK.AWS.ECR;
using System.Collections.Generic;

namespace AspNetAppEcsFargate
{
    public class AppStack : Stack
    {
        internal AppStack(Construct scope, RecipeConfiguration<Configuration> recipeConfiguration, IStackProps? props = null)
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

            ICluster cluster;
            if (settings.ECSCluster.CreateNew)
            {
                cluster = new Cluster(this, "Cluster", new ClusterProps
                {
                    Vpc = vpc,
                    ClusterName = settings.ECSCluster.NewClusterName
                });
            }
            else
            {
                cluster = Cluster.FromClusterAttributes(this, "Cluster", new ClusterAttributes
                {
                    ClusterArn = settings.ECSCluster.ClusterArn,
                    ClusterName = ECSFargateUtilities.GetClusterNameFromArn(settings.ECSCluster.ClusterArn),
                    SecurityGroups = new ISecurityGroup[0],
                    Vpc = vpc
                });
            }

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
                if (string.IsNullOrEmpty(settings.ApplicationIAMRole.RoleArn))
                    throw new InvalidOrMissingConfigurationException("The provided Application IAM Role ARN is null or empty.");

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

            if (string.IsNullOrEmpty(recipeConfiguration.ECRRepositoryName))
                throw new InvalidOrMissingConfigurationException("The provided ECR Repository Name is null or empty.");

            var ecrRepository = Repository.FromRepositoryName(this, "ECRRepository", recipeConfiguration.ECRRepositoryName);
            var container = taskDefinition.AddContainer("Container", new ContainerDefinitionOptions
            {
                Image = ContainerImage.FromEcrRepository(ecrRepository, recipeConfiguration.ECRImageTag)
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
    }
}
