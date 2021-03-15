// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.ElasticBeanstalk;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.S3.Assets;
using AWS.Deploy.Recipes.CDK.Common;
using AspNetAppElasticBeanstalkLinux.Configurations;

namespace AspNetAppElasticBeanstalkLinux
{
    public class AppStack : Stack
    {
        private const string ENVIRONMENTTYPE_SINGLEINSTANCE = "SingleInstance";
        private const string ENVIRONMENTTYPE_LOADBALANCED = "LoadBalanced";

        internal AppStack(Construct scope, RecipeConfiguration<Configuration> recipeConfiguration, IStackProps props = null)
            : base(scope, recipeConfiguration.StackName, props)
        {
            var settings = recipeConfiguration.Settings;

            var asset = new Asset(this, "Asset", new AssetProps
            {
                Path = recipeConfiguration.DotnetPublishZipPath
            });

            CfnApplication application = null;

            // Create an app version from the S3 asset defined above
            // The S3 "putObject" will occur first before CF generates the template
            var applicationVersion = new CfnApplicationVersion(this, "ApplicationVersion", new CfnApplicationVersionProps
            {
                ApplicationName = settings.BeanstalkApplication.ApplicationName,
                SourceBundle = new CfnApplicationVersion.SourceBundleProperty
                {
                    S3Bucket = asset.S3BucketName,
                    S3Key = asset.S3ObjectKey
                }
            });

            if (settings.BeanstalkApplication.CreateNew)
            {
                application = new CfnApplication(this, "Application", new CfnApplicationProps
                {
                    ApplicationName = settings.BeanstalkApplication.ApplicationName
                });

                applicationVersion.AddDependsOn(application);
            }

            IRole role;
            if (settings.ApplicationIAMRole.CreateNew)
            {
                role = new Role(this, "Role", new RoleProps
                {
                    AssumedBy = new ServicePrincipal("ec2.amazonaws.com"),

                    // https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/iam-instanceprofile.html
                    ManagedPolicies = new[]
                    {
                        ManagedPolicy.FromAwsManagedPolicyName("AWSElasticBeanstalkWebTier"),
                        ManagedPolicy.FromAwsManagedPolicyName("AWSElasticBeanstalkWorkerTier")
                    }
                });
            }
            else
            {
                role = Role.FromRoleArn(this, "Role", settings.ApplicationIAMRole.RoleArn);
            }

            var instanceProfile = new CfnInstanceProfile(this, "InstanceProfile", new CfnInstanceProfileProps
            {
                Roles = new[]
                {
                    role.RoleName
                }
            });

            var optionSettingProperties = new List<CfnEnvironment.OptionSettingProperty> {
                   new CfnEnvironment.OptionSettingProperty {
                        Namespace = "aws:autoscaling:launchconfiguration",
                        OptionName =  "IamInstanceProfile",
                        Value = instanceProfile.AttrArn
                   },
                   new CfnEnvironment.OptionSettingProperty {
                        Namespace = "aws:elasticbeanstalk:environment",
                        OptionName =  "EnvironmentType",
                        Value = settings.EnvironmentType
                   }
                };

            if(!string.IsNullOrEmpty(settings.InstanceType))
            {
                optionSettingProperties.Add(new CfnEnvironment.OptionSettingProperty
                {
                    Namespace = "aws:autoscaling:launchconfiguration",
                    OptionName = "InstanceType",
                    Value = settings.InstanceType
                });
            }

            if (settings.EnvironmentType.Equals(ENVIRONMENTTYPE_LOADBALANCED))
            {
                optionSettingProperties.Add(
                    new CfnEnvironment.OptionSettingProperty
                    {
                        Namespace = "aws:elasticbeanstalk:environment",
                        OptionName = "LoadBalancerType",
                        Value = settings.LoadBalancerType
                    }
                );
            }

            if (!string.IsNullOrEmpty(settings.EC2KeyPair))
            {
                optionSettingProperties.Add(
                    new CfnEnvironment.OptionSettingProperty
                    {
                        Namespace = "aws:autoscaling:launchconfiguration",
                        OptionName = "EC2KeyName",
                        Value = settings.EC2KeyPair
                    }
                );
            }

            var environment = new CfnEnvironment(this, "Environment", new CfnEnvironmentProps
            {
                EnvironmentName = settings.EnvironmentName,
                ApplicationName = settings.BeanstalkApplication.ApplicationName,
                PlatformArn = settings.ElasticBeanstalkPlatformArn,
                OptionSettings = optionSettingProperties.ToArray(),
                // This line is critical - reference the label created in this same stack
                VersionLabel = applicationVersion.Ref,
            });

            var output = new CfnOutput(this, "EndpointURL", new CfnOutputProps
            {
                Value = $"http://{environment.AttrEndpointUrl}/"
            });
        }
    }
}
