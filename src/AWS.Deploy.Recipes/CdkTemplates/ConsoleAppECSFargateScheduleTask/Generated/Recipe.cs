// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;

using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.IAM;
using AWS.Deploy.Recipes.CDK.Common;

using ConsoleAppECSFargateScheduleTask.Configurations;

using Protocol = Amazon.CDK.AWS.ECS.Protocol;
using Schedule = Amazon.CDK.AWS.ApplicationAutoScaling.Schedule;
using System.Linq;
using Constructs;

// This is a generated file from the original deployment recipe. It is recommended to not modify this file in order
// to allow easy updates to the file when the original recipe that this project was created from has updates.
// To customize the CDK constructs created in this file you should use the AppStack.CustomizeCDKProps() method.

namespace ConsoleAppECSFargateScheduleTask
{
    using static AWS.Deploy.Recipes.CDK.Common.CDKRecipeCustomizer<Recipe>;

    public class Recipe : Construct
    {
        public IRole? AppIAMTaskRole { get; private set; }

        public IVpc? AppVpc { get; private set; }

        public ICluster? EcsCluster { get; private set; }

        public ContainerDefinitionOptions? AppContainerDefinition { get; private set; }

        public AwsLogDriver? AppLogging { get; private set; }

        public FargateTaskDefinition? AppTaskDefinition { get; private set; }

        public ScheduledFargateTask? ScheduleTask { get; private set; }

        public Recipe(Construct scope, IRecipeProps<Configuration> props)
            // The "Recipe" construct ID will be used as part of the CloudFormation logical ID. If the value is changed this will
            // change the expected values for the "DisplayedResources" in the corresponding recipe file.
            : base(scope, "Recipe")
        {
            var settings = props.Settings;

            ConfigureIAM(settings);
            ConfigureVpc(settings);
            ConfigureTaskDefinition(props);
            ConfigureCluster(settings);
            ConfigureScheduledTask(settings);
        }

        private void ConfigureIAM(Configuration settings)
        {
            if (settings.ApplicationIAMRole.CreateNew)
            {
                AppIAMTaskRole = new Role(this, nameof(AppIAMTaskRole), InvokeCustomizeCDKPropsEvent(nameof(AppIAMTaskRole), this, new RoleProps
                {
                    AssumedBy = new ServicePrincipal("ecs-tasks.amazonaws.com")
                }));
            }
            else
            {
                if (string.IsNullOrEmpty(settings.ApplicationIAMRole.RoleArn))
                    throw new InvalidOrMissingConfigurationException("The provided Application IAM Role ARN is null or empty.");

                AppIAMTaskRole = Role.FromRoleArn(this, nameof(AppIAMTaskRole), settings.ApplicationIAMRole.RoleArn, new FromRoleArnOptions
                {
                    Mutable = false
                });
            }
        }

        private void ConfigureVpc(Configuration settings)
        {
            if (settings.Vpc.IsDefault)
            {
                AppVpc = Vpc.FromLookup(this, nameof(AppVpc), new VpcLookupOptions
                {
                    IsDefault = true
                });
            }
            else if (settings.Vpc.CreateNew)
            {
                AppVpc = new Vpc(this, nameof(AppVpc), InvokeCustomizeCDKPropsEvent(nameof(AppVpc), this, new VpcProps
                {
                    MaxAzs = 2
                }));
            }
            else
            {
                AppVpc = Vpc.FromLookup(this, nameof(AppVpc), new VpcLookupOptions
                {
                    VpcId = settings.Vpc.VpcId
                });
            }
        }

        private void ConfigureTaskDefinition(IRecipeProps<Configuration> props)
        {
            var settings = props.Settings;

            AppTaskDefinition = new FargateTaskDefinition(this, nameof(AppTaskDefinition), InvokeCustomizeCDKPropsEvent(nameof(AppTaskDefinition), this, new FargateTaskDefinitionProps
            {
                TaskRole = AppIAMTaskRole,
                Cpu = settings.TaskCpu,
                MemoryLimitMiB = settings.TaskMemory
            }));

            AppLogging = new AwsLogDriver(InvokeCustomizeCDKPropsEvent(nameof(AppLogging), this, new AwsLogDriverProps
            {
                StreamPrefix = props.StackName
            }));

            if (string.IsNullOrEmpty(props.ECRRepositoryName))
                throw new InvalidOrMissingConfigurationException("The provided ECR Repository Name is null or empty.");

            var ecrRepository = Repository.FromRepositoryName(this, "ECRRepository", props.ECRRepositoryName);
            AppContainerDefinition = new ContainerDefinitionOptions
            {
                Image = ContainerImage.FromEcrRepository(ecrRepository, props.ECRImageTag),
                Logging = AppLogging,
                Environment = settings.ECSEnvironmentVariables
            };

            AppTaskDefinition.AddContainer(nameof(AppContainerDefinition), InvokeCustomizeCDKPropsEvent(nameof(AppContainerDefinition), this, AppContainerDefinition));
        }

        private void ConfigureCluster(Configuration settings)
        {
            if (AppVpc == null)
                throw new InvalidOperationException($"{nameof(AppVpc)} has not been set. The {nameof(ConfigureVpc)} method should be called before {nameof(ConfigureCluster)}");

            if (settings.ECSCluster.CreateNew)
            {
                EcsCluster = new Cluster(this, nameof(EcsCluster), InvokeCustomizeCDKPropsEvent(nameof(EcsCluster), this, new ClusterProps
                {
                    Vpc = AppVpc,
                    ClusterName = settings.ECSCluster.NewClusterName
                }));
            }
            else
            {
                EcsCluster = Cluster.FromClusterAttributes(this, nameof(EcsCluster), new ClusterAttributes
                {
                    ClusterArn = settings.ECSCluster.ClusterArn,
                    ClusterName = ECSFargateUtilities.GetClusterNameFromArn(settings.ECSCluster.ClusterArn),
                    SecurityGroups = new ISecurityGroup[0],
                    Vpc = AppVpc
                });
            }
        }

        private void ConfigureScheduledTask(Configuration settings)
        {
            if (AppTaskDefinition == null)
                throw new InvalidOperationException($"{nameof(AppTaskDefinition)} has not been set. The {nameof(ConfigureTaskDefinition)} method should be called before {nameof(ConfigureScheduledTask)}");

            var subnetSelection = new SubnetSelection();
            if (settings.Vpc.Subnets.Any())
            {
                var count = 0;
                subnetSelection.Subnets = new ISubnet[settings.Vpc.Subnets.Count];

                foreach (var subnetName in settings.Vpc.Subnets)
                {
                    subnetSelection.Subnets[count] = Subnet.FromSubnetId(this, $"SelectedSubnet-{count + 1}", subnetName.Trim());
                    count++;
                }
            }
            else if (settings.Vpc.IsDefault)
            {
                subnetSelection = new SubnetSelection
                {
                    SubnetType = SubnetType.PUBLIC
                };
            }

            ScheduleTask = new ScheduledFargateTask(this, nameof(ScheduleTask), InvokeCustomizeCDKPropsEvent(nameof(ScheduleTask), this, new ScheduledFargateTaskProps
            {
                Cluster = EcsCluster,
                Schedule = Schedule.Expression(settings.Schedule),
                Vpc = AppVpc,
                ScheduledFargateTaskDefinitionOptions = new ScheduledFargateTaskDefinitionOptions
                {
                    TaskDefinition = AppTaskDefinition
                },
                SubnetSelection = subnetSelection
            }));
        }
    }
}
