// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.IAM;
using AWS.Deploy.Recipes.CDK.Common;
using System.IO;
using System.Collections.Generic;
using Amazon.CDK.AWS.Logs;
using ConsoleAppECSFargateScheduleTask.Configurations;
using Protocol = Amazon.CDK.AWS.ECS.Protocol;
using Schedule = Amazon.CDK.AWS.ApplicationAutoScaling.Schedule;

namespace ConsoleAppECSFargateScheduleTask
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

            var logging = new AwsLogDriver(new AwsLogDriverProps
            {
                StreamPrefix = recipeConfiguration.StackName
            });

            var ecrRepository = Repository.FromRepositoryName(this, "ECRRepository", recipeConfiguration.ECRRepositoryName);
            taskDefinition.AddContainer("Container", new ContainerDefinitionOptions
            {
                Image = ContainerImage.FromEcrRepository(ecrRepository, recipeConfiguration.ECRImageTag),
                Logging = logging
            });

            SubnetSelection subnetSelection = null;
            if (settings.Vpc.IsDefault)
            {
                subnetSelection = new SubnetSelection
                {
                    SubnetType = SubnetType.PUBLIC
                };
            }

            new ScheduledFargateTask(this, "FargateService", new ScheduledFargateTaskProps
            {
                Cluster = cluster,
                Schedule = Schedule.Expression(settings.Schedule),
                Vpc = vpc,
                ScheduledFargateTaskDefinitionOptions = new ScheduledFargateTaskDefinitionOptions
                {
                    TaskDefinition = taskDefinition
                },
                SubnetSelection = subnetSelection
            });
        }
    }
}
