using System;
using System.Collections.Generic;

using Amazon.CDK;
using Amazon.CDK.AWS.ApplicationAutoScaling;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Amazon.CDK.AWS.IAM;
using AWS.Deploy.Recipes.CDK.Common;
using AspNetAppEcsFargate.Configurations;
using Amazon.CDK.AWS.ECR;
using System.Linq;
using Constructs;


// This is a generated file from the original deployment recipe. It is recommended to not modify this file in order
// to allow easy updates to the file when the original recipe that this project was created from has updates.
// To customize the CDK constructs created in this file you should use the AppStack.CustomizeCDKProps() method.

namespace AspNetAppEcsFargate
{
    using static AWS.Deploy.Recipes.CDK.Common.CDKRecipeCustomizer<Recipe>;

    public class Recipe : Construct
    {
        public IVpc? AppVpc { get; private set; }
        public ICluster? EcsCluster { get; private set; }
        public IRole? AppIAMTaskRole { get; private set; }
        public TaskDefinition? AppTaskDefinition { get; private set; }
        public IRepository? EcrRepository { get; private set; }
        public ContainerDefinition? AppContainerDefinition { get; private set; }
        public SecurityGroup? WebAccessSecurityGroup { get; private set; }
        public IList<ISecurityGroup>? EcsServiceSecurityGroups { get; private set; }
        public FargateService? AppFargateService { get; private set; }
        public IApplicationLoadBalancer? ServiceLoadBalancer { get; private set; }
        public IApplicationListener? LoadBalancerListener { get; private set; }
        public ApplicationTargetGroup? ServiceTargetGroup { get; private set; }
        public AwsLogDriver? AppLogging { get; private set; }

        public ScalableTaskCount? AutoScalingConfiguration { get; private set; }

        public string AutoScaleTypeCpuType { get; } = "AutoScaleTypeCpuType";
        public string AutoScaleTypeRequestType { get; } = "AutoScaleTypeRequestType";
        public string AutoScaleTypeMemoryType { get; } = "AutoScaleTypeMemoryType";

        public string AddTargetGroup { get; } = "AddTargetGroup";

        public Recipe(Construct scope, IRecipeProps<Configuration> props)
            // The "Recipe" construct ID will be used as part of the CloudFormation logical ID. If the value is changed this will
            // change the expected values for the "DisplayedResources" in the corresponding recipe file.
            : base(scope, "Recipe")
        {
            var settings = props.Settings;

            ConfigureVpc(settings);
            ConfigureApplicationIAMRole(settings);
            ConfigureECSClusterAndService(props);
            ConfigureLoadBalancer(settings);
            ConfigureAutoScaling(settings);
        }

        private void ConfigureVpc(Configuration settings)
        {
            if (settings.Vpc.IsDefault)
            {
                AppVpc = Vpc.FromLookup(this, nameof(AppVpc), InvokeCustomizeCDKPropsEvent(nameof(AppVpc), this, new VpcLookupOptions
                {
                    IsDefault = true
                }));
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
                AppVpc = Vpc.FromLookup(this, nameof(AppVpc), InvokeCustomizeCDKPropsEvent(nameof(AppVpc), this, new VpcLookupOptions
                {
                    VpcId = settings.Vpc.VpcId
                }));
            }
        }

        private void ConfigureApplicationIAMRole(Configuration settings)
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

                AppIAMTaskRole = Role.FromRoleArn(this, nameof(AppIAMTaskRole), settings.ApplicationIAMRole.RoleArn, InvokeCustomizeCDKPropsEvent(nameof(AppIAMTaskRole), this, new FromRoleArnOptions
                {
                    Mutable = false
                }));
            }
        }

        private void ConfigureECSClusterAndService(IRecipeProps<Configuration> recipeConfiguration)
        {
            if (AppVpc == null)
                throw new InvalidOperationException($"{nameof(AppVpc)} has not been set. The {nameof(ConfigureVpc)} method should be called before {nameof(ConfigureECSClusterAndService)}");

            var settings = recipeConfiguration.Settings;
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
                EcsCluster = Cluster.FromClusterAttributes(this, nameof(EcsCluster), InvokeCustomizeCDKPropsEvent(nameof(EcsCluster), this, new ClusterAttributes
                {
                    ClusterArn = settings.ECSCluster.ClusterArn,
                    ClusterName = ECSFargateUtilities.GetClusterNameFromArn(settings.ECSCluster.ClusterArn),
                    SecurityGroups = new ISecurityGroup[0],
                    Vpc = AppVpc
                }));
            }

            AppTaskDefinition = new FargateTaskDefinition(this, nameof(AppTaskDefinition), InvokeCustomizeCDKPropsEvent(nameof(AppTaskDefinition), this, new FargateTaskDefinitionProps
            {
                TaskRole = AppIAMTaskRole,
                Cpu = settings.TaskCpu,
                MemoryLimitMiB = settings.TaskMemory
            }));

            AppLogging = new AwsLogDriver(InvokeCustomizeCDKPropsEvent(nameof(AppLogging), this, new AwsLogDriverProps
            {
                StreamPrefix = recipeConfiguration.StackName
            }));

            if (string.IsNullOrEmpty(recipeConfiguration.ECRRepositoryName))
                throw new InvalidOrMissingConfigurationException("The provided ECR Repository Name is null or empty.");

            EcrRepository = Repository.FromRepositoryName(this, nameof(EcrRepository), recipeConfiguration.ECRRepositoryName);
            AppContainerDefinition = AppTaskDefinition.AddContainer(nameof(AppContainerDefinition), InvokeCustomizeCDKPropsEvent(nameof(AppContainerDefinition), this, new ContainerDefinitionOptions
            {
                Image = ContainerImage.FromEcrRepository(EcrRepository, recipeConfiguration.ECRImageTag),
                Logging = AppLogging
            }));

            AppContainerDefinition.AddPortMappings(new PortMapping
            {
                ContainerPort = 80,
                Protocol = Amazon.CDK.AWS.ECS.Protocol.TCP
            });

            WebAccessSecurityGroup = new SecurityGroup(this, nameof(WebAccessSecurityGroup), InvokeCustomizeCDKPropsEvent(nameof(WebAccessSecurityGroup), this, new SecurityGroupProps
            {
                Vpc = AppVpc,
                SecurityGroupName = $"{recipeConfiguration.StackName}-ECSService"
            }));

            EcsServiceSecurityGroups = new List<ISecurityGroup>();
            EcsServiceSecurityGroups.Add(WebAccessSecurityGroup);

            if (!string.IsNullOrEmpty(settings.AdditionalECSServiceSecurityGroups))
            {
                var count = 1;
                foreach (var securityGroupId in settings.AdditionalECSServiceSecurityGroups.Split(','))
                {
                    EcsServiceSecurityGroups.Add(SecurityGroup.FromSecurityGroupId(this, $"AdditionalGroup-{count++}", securityGroupId.Trim(), new SecurityGroupImportOptions
                    {
                        Mutable = false
                    }));
                }
            }

            AppFargateService = new FargateService(this, nameof(AppFargateService), InvokeCustomizeCDKPropsEvent(nameof(AppFargateService), this, new FargateServiceProps
            {
                Cluster = EcsCluster,
                TaskDefinition = AppTaskDefinition,
                DesiredCount = settings.DesiredCount,
                ServiceName = settings.ECSServiceName,
                AssignPublicIp = settings.Vpc.IsDefault,
                SecurityGroups = EcsServiceSecurityGroups.ToArray()
            }));
        }

        private void ConfigureLoadBalancer(Configuration settings)
        {
            if (AppVpc == null)
                throw new InvalidOperationException($"{nameof(AppVpc)} has not been set. The {nameof(ConfigureVpc)} method should be called before {nameof(ConfigureLoadBalancer)}");
            if (EcsCluster == null)
                throw new InvalidOperationException($"{nameof(EcsCluster)} has not been set. The {nameof(ConfigureECSClusterAndService)} method should be called before {nameof(ConfigureLoadBalancer)}");
            if (AppFargateService == null)
                throw new InvalidOperationException($"{nameof(AppFargateService)} has not been set. The {nameof(ConfigureECSClusterAndService)} method should be called before {nameof(ConfigureLoadBalancer)}");

            if (settings.LoadBalancer.CreateNew)
            {
                ServiceLoadBalancer = new ApplicationLoadBalancer(this, nameof(ServiceLoadBalancer), InvokeCustomizeCDKPropsEvent(nameof(ServiceLoadBalancer), this, new ApplicationLoadBalancerProps
                {
                    Vpc = AppVpc,
                    InternetFacing = true
                }));

                LoadBalancerListener = ServiceLoadBalancer.AddListener(nameof(LoadBalancerListener), InvokeCustomizeCDKPropsEvent(nameof(LoadBalancerListener), this, new ApplicationListenerProps
                {
                    Protocol = ApplicationProtocol.HTTP,
                    Port = 80,
                    Open = true
                }));

                ServiceTargetGroup = LoadBalancerListener.AddTargets(nameof(ServiceTargetGroup), InvokeCustomizeCDKPropsEvent(nameof(ServiceTargetGroup), this, new AddApplicationTargetsProps
                {
                    Protocol = ApplicationProtocol.HTTP,
                    DeregistrationDelay = Duration.Seconds(settings.LoadBalancer.DeregistrationDelayInSeconds)
                }));
            }
            else
            {
                ServiceLoadBalancer = ApplicationLoadBalancer.FromLookup(this, nameof(ServiceLoadBalancer), InvokeCustomizeCDKPropsEvent(nameof(ServiceLoadBalancer), this, new ApplicationLoadBalancerLookupOptions
                {
                    LoadBalancerArn = settings.LoadBalancer.ExistingLoadBalancerArn
                }));

                LoadBalancerListener = ApplicationListener.FromLookup(this, nameof(LoadBalancerListener), InvokeCustomizeCDKPropsEvent(nameof(LoadBalancerListener), this, new ApplicationListenerLookupOptions
                {
                    LoadBalancerArn = settings.LoadBalancer.ExistingLoadBalancerArn,
                    ListenerPort = 80
                }));

                ServiceTargetGroup = new ApplicationTargetGroup(this, nameof(ServiceTargetGroup), InvokeCustomizeCDKPropsEvent(nameof(ServiceTargetGroup), this, new ApplicationTargetGroupProps
                {
                    Port = 80,
                    Vpc = EcsCluster.Vpc,
                }));


                var addApplicationTargetGroupsProps = new AddApplicationTargetGroupsProps
                {
                    TargetGroups = new[] { ServiceTargetGroup }
                };

                if(settings.LoadBalancer.ListenerConditionType != LoadBalancerConfiguration.ListenerConditionTypeEnum.None)
                {
                    addApplicationTargetGroupsProps.Priority = settings.LoadBalancer.ListenerConditionPriority;
                }

                if (settings.LoadBalancer.ListenerConditionType == LoadBalancerConfiguration.ListenerConditionTypeEnum.Path)
                {
                    if(settings.LoadBalancer.ListenerConditionPathPattern == null)
                    {
                        throw new ArgumentNullException("Listener condition type was set to \"Path\" but no value was set for the \"TargetPathPattern\"");
                    }
                    addApplicationTargetGroupsProps.Conditions = new ListenerCondition[]
                    {
                        ListenerCondition.PathPatterns(new []{ settings.LoadBalancer.ListenerConditionPathPattern })
                    };
                }

                LoadBalancerListener.AddTargetGroups("AddTargetGroup", InvokeCustomizeCDKPropsEvent("AddTargetGroup", this, addApplicationTargetGroupsProps));
            }

            // Configure health check for ALB Target Group
            var healthCheck = new Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck();
            if(settings.LoadBalancer.HealthCheckPath != null)
            {
                var path = settings.LoadBalancer.HealthCheckPath;
                if (!path.StartsWith("/"))
                    path = "/" + path;
                healthCheck.Path = path;
            }
            if(settings.LoadBalancer.HealthCheckInternval.HasValue)
            {
                healthCheck.Interval = Duration.Seconds(settings.LoadBalancer.HealthCheckInternval.Value);
            }
            if (settings.LoadBalancer.HealthyThresholdCount.HasValue)
            {
                healthCheck.HealthyThresholdCount = settings.LoadBalancer.HealthyThresholdCount.Value;
            }
            if (settings.LoadBalancer.UnhealthyThresholdCount.HasValue)
            {
                healthCheck.UnhealthyThresholdCount = settings.LoadBalancer.UnhealthyThresholdCount.Value;
            }

            ServiceTargetGroup.ConfigureHealthCheck(healthCheck);

            ServiceTargetGroup.AddTarget(AppFargateService);
        }

        private void ConfigureAutoScaling(Configuration settings)
        {
            if (AppFargateService == null)
                throw new InvalidOperationException($"{nameof(AppFargateService)} has not been set. The {nameof(ConfigureECSClusterAndService)} method should be called before {nameof(ConfigureAutoScaling)}");
            if (ServiceTargetGroup == null)
                throw new InvalidOperationException($"{nameof(ServiceTargetGroup)} has not been set. The {nameof(ConfigureLoadBalancer)} method should be called before {nameof(ConfigureAutoScaling)}");

            if (settings.AutoScaling.Enabled)
            {
                AutoScalingConfiguration = AppFargateService.AutoScaleTaskCount(InvokeCustomizeCDKPropsEvent(nameof(AutoScalingConfiguration), this, new EnableScalingProps
                {
                    MinCapacity = settings.AutoScaling.MinCapacity,
                    MaxCapacity = settings.AutoScaling.MaxCapacity
                }));

                switch (settings.AutoScaling.ScalingType)
                {
                    case AspNetAppEcsFargate.Configurations.AutoScalingConfiguration.ScalingTypeEnum.Cpu:
                        AutoScalingConfiguration.ScaleOnCpuUtilization(AutoScaleTypeCpuType, InvokeCustomizeCDKPropsEvent(AutoScaleTypeCpuType, this, new CpuUtilizationScalingProps
                        {
                            TargetUtilizationPercent = settings.AutoScaling.CpuTypeTargetUtilizationPercent,
                            ScaleOutCooldown = Duration.Seconds(settings.AutoScaling.CpuTypeScaleOutCooldownSeconds),
                            ScaleInCooldown = Duration.Seconds(settings.AutoScaling.CpuTypeScaleInCooldownSeconds)
                        }));
                        break;
                    case AspNetAppEcsFargate.Configurations.AutoScalingConfiguration.ScalingTypeEnum.Memory:
                        AutoScalingConfiguration.ScaleOnMemoryUtilization(AutoScaleTypeMemoryType, InvokeCustomizeCDKPropsEvent(AutoScaleTypeMemoryType, this, new MemoryUtilizationScalingProps
                        {
                            TargetUtilizationPercent = settings.AutoScaling.MemoryTypeTargetUtilizationPercent,
                            ScaleOutCooldown = Duration.Seconds(settings.AutoScaling.MemoryTypeScaleOutCooldownSeconds),
                            ScaleInCooldown = Duration.Seconds(settings.AutoScaling.MemoryTypeScaleInCooldownSeconds)
                        }));
                        break;
                    case AspNetAppEcsFargate.Configurations.AutoScalingConfiguration.ScalingTypeEnum.Request:
                        AutoScalingConfiguration.ScaleOnRequestCount(AutoScaleTypeRequestType, InvokeCustomizeCDKPropsEvent(AutoScaleTypeRequestType, this, new RequestCountScalingProps
                        {
                            TargetGroup = ServiceTargetGroup,
                            RequestsPerTarget = settings.AutoScaling.RequestTypeRequestsPerTarget,
                            ScaleOutCooldown = Duration.Seconds(settings.AutoScaling.RequestTypeScaleOutCooldownSeconds),
                            ScaleInCooldown = Duration.Seconds(settings.AutoScaling.RequestTypeScaleInCooldownSeconds)
                        }));
                        break;
                    default:
                        throw new ArgumentException($"Invalid AutoScaling type {settings.AutoScaling.ScalingType}");
                }
            }
        }
    }
}
