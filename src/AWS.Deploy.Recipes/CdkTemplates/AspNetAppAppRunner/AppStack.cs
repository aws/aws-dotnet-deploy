// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.AppRunner;
using Amazon.CDK.AWS.IAM;
using AWS.Deploy.Recipes.CDK.Common;
using AspNetAppAppRunner.Configurations;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS;

using CfnService = Amazon.CDK.AWS.AppRunner.CfnService;
using CfnServiceProps = Amazon.CDK.AWS.AppRunner.CfnServiceProps;


namespace AspNetAppAppRunner
{
    public class AppStack : Stack
    {
        internal AppStack(Construct scope, RecipeConfiguration<Configuration> recipeConfiguration, IStackProps? props = null)
            : base(scope, recipeConfiguration.StackName, props)
        {
            var settings = recipeConfiguration.Settings;

            if (string.IsNullOrEmpty(recipeConfiguration.ECRRepositoryName))
                throw new InvalidOrMissingConfigurationException("The provided ECR Repository Name is null or empty.");

            var ecrRepository = Repository.FromRepositoryName(this, "ECRRepository", recipeConfiguration.ECRRepositoryName);

            IRole serviceAccessRole;
            if (settings.ServiceAccessIAMRole.CreateNew)
            {
                serviceAccessRole = new Role(this, "ServiceAccessRole", new RoleProps
                {
                    AssumedBy = new ServicePrincipal("build.apprunner.amazonaws.com")
                });

                serviceAccessRole.AddManagedPolicy(ManagedPolicy.FromManagedPolicyArn(this, "ServiceAccessRoleManagedPolicy", "arn:aws:iam::aws:policy/service-role/AWSAppRunnerServicePolicyForECRAccess"));
            }
            else
            {
                if (string.IsNullOrEmpty(settings.ServiceAccessIAMRole.RoleArn))
                    throw new InvalidOrMissingConfigurationException("The provided Application IAM Role ARN is null or empty.");

                serviceAccessRole = Role.FromRoleArn(this, "ServiceAccessRole", settings.ServiceAccessIAMRole.RoleArn, new FromRoleArnOptions
                {
                    Mutable = false
                });
            }

            var appRunnerServiceProp = new CfnServiceProps
            {
                ServiceName = settings.ServiceName,
                SourceConfiguration = new CfnService.SourceConfigurationProperty
                {
                    AuthenticationConfiguration = new CfnService.AuthenticationConfigurationProperty
                    {
                        AccessRoleArn = serviceAccessRole.RoleArn
                    },
                    ImageRepository = new CfnService.ImageRepositoryProperty
                    {
                        ImageRepositoryType = "ECR",
                        ImageIdentifier = ContainerImage.FromEcrRepository(ecrRepository, recipeConfiguration.ECRImageTag).ImageName,
                        ImageConfiguration = new CfnService.ImageConfigurationProperty
                        {
                            Port = settings.Port.ToString(),
                            StartCommand = !string.IsNullOrWhiteSpace(settings.StartCommand) ? settings.StartCommand : null
                        }
                    }
                }
            };

            if(!string.IsNullOrEmpty(settings.EncryptionKmsKey))
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

            if(string.Equals(healthCheckConfig.Protocol, "HTTP"))
            {
                healthCheckConfig.Path = string.IsNullOrEmpty(settings.HealthCheckPath) ? "/" : settings.HealthCheckPath;
            }

            var instanceConfig = new CfnService.InstanceConfigurationProperty();
            appRunnerServiceProp.InstanceConfiguration = instanceConfig;

            IRole role;
            if (settings.ApplicationIAMRole.CreateNew)
            {
                role = new Role(this, "TaskRole", new RoleProps
                {
                    AssumedBy = new ServicePrincipal("tasks.apprunner.amazonaws.com")
                });
            }
            else
            {
                if (string.IsNullOrEmpty(settings.ApplicationIAMRole.RoleArn))
                    throw new InvalidOrMissingConfigurationException("The provided Application IAM Role ARN is null or empty.");

                role = Role.FromRoleArn(this, "TaskRole", settings.ApplicationIAMRole.RoleArn, new FromRoleArnOptions
                {
                    Mutable = false
                });
            }
            instanceConfig.InstanceRoleArn = role.RoleArn;

            instanceConfig.Cpu = settings.Cpu;
            instanceConfig.Memory = settings.Memory;

            var service = new CfnService(this, "AppRunnerService", appRunnerServiceProp);

            var output = new CfnOutput(this, "EndpointURL", new CfnOutputProps
            {
                Value = $"https://{service.AttrServiceUrl}/"
            });            
        }
    }
}
