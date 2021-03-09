// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.IAM;
using AWS.Deploy.Recipes.CDK.Common;
using ConsoleAppEcsFargateService.Configurations;
using Amazon.CDK.AWS.ECR;
using System.Collections.Generic;

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

            var fargateServiceProps = new FargateServiceProps
            {
                Cluster = cluster,
                TaskDefinition = taskDefinition,
                AssignPublicIp = settings.Vpc.IsDefault,
                DesiredCount = settings.DesiredCount
            };


            if (!string.IsNullOrEmpty(settings.ECSServiceSecurityGroups))
            {
                var ecsServiceSecurityGroups = new List<ISecurityGroup>();
                var count = 1;
                foreach (var securityGroupId in settings.ECSServiceSecurityGroups.Split(','))
                {
                    ecsServiceSecurityGroups.Add(SecurityGroup.FromSecurityGroupId(this, $"AdditionalGroup-{count++}", securityGroupId.Trim(), new SecurityGroupImportOptions
                    {
                        Mutable = false
                    }));
                }

                fargateServiceProps.SecurityGroups = ecsServiceSecurityGroups.ToArray();
            }

            new FargateService(this, "FargateService", fargateServiceProps);
        }
    }
}
