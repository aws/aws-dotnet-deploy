// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using Amazon.CDK;
using Amazon.CDK.AWS.AppRunner;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS;
using AWS.Deploy.Recipes.CDK.Common;
using AspNetAppAppRunner.Configurations;

using CfnService = Amazon.CDK.AWS.AppRunner.CfnService;
using CfnServiceProps = Amazon.CDK.AWS.AppRunner.CfnServiceProps;
using Constructs;
using System.Collections.Generic;

// This is a generated file from the original deployment recipe. It is recommended to not modify this file in order
// to allow easy updates to the file when the original recipe that this project was created from has updates.
// To customize the CDK constructs created in this file you should use the AppStack.CustomizeCDKProps() method.

namespace AspNetAppAppRunner
{
    using static AWS.Deploy.Recipes.CDK.Common.CDKRecipeCustomizer<Recipe>;

    public class Recipe : Construct
    {
        public CfnService? AppRunnerService { get; private set; }

        public IRole? ServiceAccessRole { get; private set; }

        public IRole? TaskRole { get; private set; }

        public CfnVpcConnector? VPCConnector { get; private set; }

        public Recipe(Construct scope, IRecipeProps<Configuration> props)
            // The "Recipe" construct ID will be used as part of the CloudFormation logical ID. If the value is changed this will
            // change the expected values for the "DisplayedResources" in the corresponding recipe file.
            : base(scope, "Recipe")
        {
            ConfigureVPCConnector(props.Settings);
            ConfigureIAMRoles(props.Settings);
            ConfigureAppRunnerService(props);
        }

        private void ConfigureVPCConnector(Configuration settings)
        {
            if (settings.VPCConnector.CreateNew)
            {
                if (settings.VPCConnector.Subnets.Count == 0)
                    throw new InvalidOrMissingConfigurationException("The provided list of Subnets is null or empty.");

                VPCConnector = new CfnVpcConnector(this, nameof(VPCConnector), InvokeCustomizeCDKPropsEvent(nameof(VPCConnector), this, new CfnVpcConnectorProps
                {
                    Subnets = settings.VPCConnector.Subnets.ToArray(),

                    // the properties below are optional
                    SecurityGroups = settings.VPCConnector.SecurityGroups.ToArray()
                }));
            }
        }

        private void ConfigureIAMRoles(Configuration settings)
        {
            if (settings.ServiceAccessIAMRole.CreateNew)
            {
                ServiceAccessRole = new Role(this, nameof(ServiceAccessRole), InvokeCustomizeCDKPropsEvent(nameof(ServiceAccessRole), this, new RoleProps
                {
                    AssumedBy = new ServicePrincipal("build.apprunner.amazonaws.com")
                }));

                ServiceAccessRole.AddManagedPolicy(ManagedPolicy.FromManagedPolicyArn(this, "ServiceAccessRoleManagedPolicy", "arn:aws:iam::aws:policy/service-role/AWSAppRunnerServicePolicyForECRAccess"));
            }
            else
            {
                if (string.IsNullOrEmpty(settings.ServiceAccessIAMRole.RoleArn))
                    throw new InvalidOrMissingConfigurationException("The provided Application IAM Role ARN is null or empty.");

                ServiceAccessRole = Role.FromRoleArn(this, nameof(ServiceAccessRole), settings.ServiceAccessIAMRole.RoleArn, new FromRoleArnOptions
                {
                    Mutable = false
                });
            }

            if (settings.ApplicationIAMRole.CreateNew)
            {
                TaskRole = new Role(this, nameof(TaskRole), InvokeCustomizeCDKPropsEvent(nameof(TaskRole), this, new RoleProps
                {
                    AssumedBy = new ServicePrincipal("tasks.apprunner.amazonaws.com")
                }));
            }
            else
            {
                if (string.IsNullOrEmpty(settings.ApplicationIAMRole.RoleArn))
                    throw new InvalidOrMissingConfigurationException("The provided Application IAM Role ARN is null or empty.");

                TaskRole = Role.FromRoleArn(this, nameof(TaskRole), settings.ApplicationIAMRole.RoleArn, new FromRoleArnOptions
                {
                    Mutable = false
                });
            }
        }

        private void ConfigureAppRunnerService(IRecipeProps<Configuration> props)
        {
            if (ServiceAccessRole == null)
                throw new InvalidOperationException($"{nameof(ServiceAccessRole)} has not been set. The {nameof(ConfigureIAMRoles)} method should be called before {nameof(ConfigureAppRunnerService)}");
            if (TaskRole == null)
                throw new InvalidOperationException($"{nameof(TaskRole)} has not been set. The {nameof(ConfigureIAMRoles)} method should be called before {nameof(ConfigureAppRunnerService)}");

            if (string.IsNullOrEmpty(props.ECRRepositoryName))
                throw new InvalidOrMissingConfigurationException("The provided ECR Repository Name is null or empty.");

            var ecrRepository = Repository.FromRepositoryName(this, "ECRRepository", props.ECRRepositoryName);

            Configuration settings = props.Settings;

            var runtimeEnvironmentVariables = new List<CfnService.IKeyValuePairProperty>();
            foreach (var variable in settings.AppRunnerEnvironmentVariables)
            {
                runtimeEnvironmentVariables.Add(new CfnService.KeyValuePairProperty
                {
                    Name = variable.Key,
                    Value = variable.Value
                });
            }

            var appRunnerServiceProp = new CfnServiceProps
            {
                ServiceName = settings.ServiceName,
                SourceConfiguration = new CfnService.SourceConfigurationProperty
                {
                    AuthenticationConfiguration = new CfnService.AuthenticationConfigurationProperty
                    {
                        AccessRoleArn = ServiceAccessRole.RoleArn
                    },
                    ImageRepository = new CfnService.ImageRepositoryProperty
                    {
                        ImageRepositoryType = "ECR",
                        ImageIdentifier = ContainerImage.FromEcrRepository(ecrRepository, props.ECRImageTag).ImageName,
                        ImageConfiguration = new CfnService.ImageConfigurationProperty
                        {
                            Port = settings.Port.ToString(),
                            StartCommand = !string.IsNullOrWhiteSpace(settings.StartCommand) ? settings.StartCommand : null,
                            RuntimeEnvironmentVariables = runtimeEnvironmentVariables
                        }
                    }
                }
            };

            if (settings.VPCConnector.UseVPCConnector)
            {
                appRunnerServiceProp.NetworkConfiguration = new CfnService.NetworkConfigurationProperty
                {
                    EgressConfiguration = new CfnService.EgressConfigurationProperty
                    {
                        EgressType = "VPC",
                        VpcConnectorArn = VPCConnector != null ? VPCConnector.AttrVpcConnectorArn : settings.VPCConnector.VpcConnectorId
                    }
                };
            }

            if (!string.IsNullOrEmpty(settings.EncryptionKmsKey))
            {
                var encryptionConfig = new CfnService.EncryptionConfigurationProperty();
                appRunnerServiceProp.EncryptionConfiguration = encryptionConfig;

                encryptionConfig.KmsKey = settings.EncryptionKmsKey;
            }

            var healthCheckConfig = new CfnService.HealthCheckConfigurationProperty();
            appRunnerServiceProp.HealthCheckConfiguration = healthCheckConfig;

            healthCheckConfig.HealthyThreshold = settings.HealthCheckHealthyThreshold;
            healthCheckConfig.Interval = settings.HealthCheckInterval;
            healthCheckConfig.Protocol = settings.HealthCheckProtocol;
            healthCheckConfig.Timeout = settings.HealthCheckTimeout;
            healthCheckConfig.UnhealthyThreshold = settings.HealthCheckUnhealthyThreshold;

            if (string.Equals(healthCheckConfig.Protocol, "HTTP"))
            {
                healthCheckConfig.Path = string.IsNullOrEmpty(settings.HealthCheckPath) ? "/" : settings.HealthCheckPath;
            }

            var instanceConfig = new CfnService.InstanceConfigurationProperty();
            appRunnerServiceProp.InstanceConfiguration = instanceConfig;


            instanceConfig.InstanceRoleArn = TaskRole.RoleArn;

            instanceConfig.Cpu = settings.Cpu;
            instanceConfig.Memory = settings.Memory;

            AppRunnerService = new CfnService(this, nameof(AppRunnerService), InvokeCustomizeCDKPropsEvent(nameof(AppRunnerService), this, appRunnerServiceProp));

            var output = new CfnOutput(this, "EndpointURL", new CfnOutputProps
            {
                Value = $"https://{AppRunnerService.AttrServiceUrl}/"
            });
        }
    }
}
