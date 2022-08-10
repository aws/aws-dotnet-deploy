// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.ElasticBeanstalk;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.S3.Assets;
using AWS.Deploy.Recipes.CDK.Common;

using AspNetAppElasticBeanstalkLinux.Configurations;
using Constructs;
using System.Linq;
using Amazon.CDK.AWS.EC2;

// This is a generated file from the original deployment recipe. It is recommended to not modify this file in order
// to allow easy updates to the file when the original recipe that this project was created from has updates.
// To customize the CDK constructs created in this file you should use the AppStack.CustomizeCDKProps() method.

namespace AspNetAppElasticBeanstalkLinux
{
    using static AWS.Deploy.Recipes.CDK.Common.CDKRecipeCustomizer<Recipe>;

    public class Recipe : Construct
    {
        public const string ENVIRONMENTTYPE_SINGLEINSTANCE = "SingleInstance";
        public const string ENVIRONMENTTYPE_LOADBALANCED = "LoadBalanced";

        public const string LOADBALANCERTYPE_APPLICATION = "application";

        public const string REVERSEPROXY_NGINX = "nginx";
        
        public const string ENHANCED_HEALTH_REPORTING = "enhanced";

        public Vpc? AppVpc { get; private set; }

        public IRole? AppIAMRole { get; private set; }

        public IRole? BeanstalkServiceRole { get; private set; }

        public Asset? ApplicationAsset { get; private set; }

        public CfnInstanceProfile? Ec2InstanceProfile { get; private set; }

        public CfnApplicationVersion? ApplicationVersion { get; private set; }

        public CfnApplication? BeanstalkApplication { get; private set; }

        public CfnEnvironment? BeanstalkEnvironment { get; private set; }

        public Recipe(Construct scope, IRecipeProps<Configuration> props)
            // The "Recipe" construct ID will be used as part of the CloudFormation logical ID. If the value is changed this will
            // change the expected values for the "DisplayedResources" in the corresponding recipe file.
            : base(scope, "Recipe")
        {
            var settings = props.Settings;

            if (string.IsNullOrEmpty(props.DotnetPublishZipPath))
                throw new InvalidOrMissingConfigurationException("The provided path containing the dotnet publish zip file is null or empty.");

            ApplicationAsset = new Asset(this, "Asset", new AssetProps
            {
                Path = props.DotnetPublishZipPath
            });

            ConfigureVpc(settings);
            ConfigureIAM(settings);
            var beanstalkApplicationName = ConfigureApplication(settings);
            ConfigureBeanstalkEnvironment(settings, beanstalkApplicationName);
        }

        private void ConfigureVpc(Configuration settings)
        {
            if (settings.VPC.UseVPC)
            {
                if (settings.VPC.CreateNew)
                {
                    AppVpc = new Vpc(this, nameof(AppVpc), InvokeCustomizeCDKPropsEvent(nameof(AppVpc), this, new VpcProps
                    {
                        MaxAzs = 2
                    }));
                }
            }
        }

        private void ConfigureIAM(Configuration settings)
        {
            if (settings.ApplicationIAMRole.CreateNew)
            {
                AppIAMRole = new Role(this, nameof(AppIAMRole), InvokeCustomizeCDKPropsEvent(nameof(AppIAMRole), this, new RoleProps
                {
                    AssumedBy = new ServicePrincipal("ec2.amazonaws.com"),

                    // https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/iam-instanceprofile.html
                    ManagedPolicies = new[]
                    {
                        ManagedPolicy.FromAwsManagedPolicyName("AWSElasticBeanstalkWebTier"),
                        ManagedPolicy.FromAwsManagedPolicyName("AWSElasticBeanstalkWorkerTier")
                    }
                }));
            }
            else
            {
                if (string.IsNullOrEmpty(settings.ApplicationIAMRole.RoleArn))
                    throw new InvalidOrMissingConfigurationException("The provided Application IAM Role ARN is null or empty.");

                AppIAMRole = Role.FromRoleArn(this, nameof(AppIAMRole), settings.ApplicationIAMRole.RoleArn);
            }

            Ec2InstanceProfile = new CfnInstanceProfile(this, nameof(Ec2InstanceProfile), InvokeCustomizeCDKPropsEvent(nameof(Ec2InstanceProfile), this, new CfnInstanceProfileProps
            {
                Roles = new[]
                {
                    AppIAMRole.RoleName
                }
            }));

            if (settings.ServiceIAMRole.CreateNew)
            {
                BeanstalkServiceRole = new Role(this, nameof(BeanstalkServiceRole), InvokeCustomizeCDKPropsEvent(nameof(BeanstalkServiceRole), this, new RoleProps
                {
                    AssumedBy = new ServicePrincipal("elasticbeanstalk.amazonaws.com"),

                    ManagedPolicies = new[]
                    {
                        ManagedPolicy.FromAwsManagedPolicyName("AWSElasticBeanstalkManagedUpdatesCustomerRolePolicy"),
                        ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSElasticBeanstalkEnhancedHealth")
                    }
                }));
            }
            else
            {
                if (string.IsNullOrEmpty(settings.ServiceIAMRole.RoleArn))
                    throw new InvalidOrMissingConfigurationException("The provided Service IAM Role ARN is null or empty.");

                BeanstalkServiceRole = Role.FromRoleArn(this, nameof(BeanstalkServiceRole), settings.ServiceIAMRole.RoleArn);
            }
        }

        private string ConfigureApplication(Configuration settings)
        {
            if (ApplicationAsset == null)
                throw new InvalidOperationException($"{nameof(ApplicationAsset)} has not been set.");

            string beanstalkApplicationName;
            if(settings.BeanstalkApplication.CreateNew)
            {
                if (settings.BeanstalkApplication.ApplicationName == null)
                    throw new InvalidOperationException($"{nameof(settings.BeanstalkApplication.ApplicationName)} has not been set.");

                beanstalkApplicationName = settings.BeanstalkApplication.ApplicationName;
            }
            else
            {
                // This check is here for deployments that were initially done with an older version of the project.
                // In those deployments the existing application name was persisted in the ApplicationName property.
                if (settings.BeanstalkApplication.ExistingApplicationName == null && settings.BeanstalkApplication.ApplicationName != null)
                {
                    beanstalkApplicationName = settings.BeanstalkApplication.ApplicationName;
                }
                else
                {
                    if (settings.BeanstalkApplication.ExistingApplicationName == null)
                        throw new InvalidOperationException($"{nameof(settings.BeanstalkApplication.ExistingApplicationName)} has not been set.");

                    beanstalkApplicationName = settings.BeanstalkApplication.ExistingApplicationName;
                }
            }

            // Create an app version from the S3 asset defined above
            // The S3 "putObject" will occur first before CF generates the template
            ApplicationVersion = new CfnApplicationVersion(this, nameof(ApplicationVersion), InvokeCustomizeCDKPropsEvent(nameof(ApplicationVersion), this, new CfnApplicationVersionProps
            {
                ApplicationName = beanstalkApplicationName,
                SourceBundle = new CfnApplicationVersion.SourceBundleProperty
                {
                    S3Bucket = ApplicationAsset.S3BucketName,
                    S3Key = ApplicationAsset.S3ObjectKey
                }
            }));

            if (settings.BeanstalkApplication.CreateNew)
            {
                BeanstalkApplication = new CfnApplication(this, nameof(BeanstalkApplication), InvokeCustomizeCDKPropsEvent(nameof(BeanstalkApplication), this, new CfnApplicationProps
                {
                    ApplicationName = beanstalkApplicationName
                }));

                ApplicationVersion.AddDependsOn(BeanstalkApplication);
            }

            return beanstalkApplicationName;
        }

        private void ConfigureBeanstalkEnvironment(Configuration settings, string beanstalkApplicationName)
        {
            if (Ec2InstanceProfile == null)
                throw new InvalidOperationException($"{nameof(Ec2InstanceProfile)} has not been set. The {nameof(ConfigureIAM)} method should be called before {nameof(ConfigureBeanstalkEnvironment)}");
            if (ApplicationVersion == null)
                throw new InvalidOperationException($"{nameof(ApplicationVersion)} has not been set. The {nameof(ConfigureApplication)} method should be called before {nameof(ConfigureBeanstalkEnvironment)}");

            var optionSettingProperties = new List<CfnEnvironment.OptionSettingProperty> {
                   new CfnEnvironment.OptionSettingProperty {
                        Namespace = "aws:autoscaling:launchconfiguration",
                        OptionName =  "IamInstanceProfile",
                        Value = Ec2InstanceProfile.AttrArn
                   },
                   new CfnEnvironment.OptionSettingProperty {
                        Namespace = "aws:elasticbeanstalk:environment",
                        OptionName =  "EnvironmentType",
                        Value = settings.EnvironmentType
                   },
                   new CfnEnvironment.OptionSettingProperty
                   {
                        Namespace = "aws:elasticbeanstalk:managedactions",
                        OptionName = "ManagedActionsEnabled",
                        Value = settings.ElasticBeanstalkManagedPlatformUpdates.ManagedActionsEnabled.ToString().ToLower()
                   },
                   new CfnEnvironment.OptionSettingProperty
                   {
                        Namespace = "aws:elasticbeanstalk:xray",
                        OptionName = "XRayEnabled",
                        Value = settings.XRayTracingSupportEnabled.ToString().ToLower()
                   },
                   new CfnEnvironment.OptionSettingProperty
                   {
                        Namespace = "aws:elasticbeanstalk:healthreporting:system",
                        OptionName = "SystemType",
                        Value = settings.EnhancedHealthReporting
                   }
                };

            if (!string.IsNullOrEmpty(settings.InstanceType))
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

                if (!string.IsNullOrEmpty(settings.HealthCheckURL))
                {
                    optionSettingProperties.Add(
                        new CfnEnvironment.OptionSettingProperty
                        {
                            Namespace = "aws:elasticbeanstalk:application",
                            OptionName = "Application Healthcheck URL",
                            Value = settings.HealthCheckURL
                        }
                    );

                    optionSettingProperties.Add(
                        new CfnEnvironment.OptionSettingProperty
                        {
                            Namespace = "aws:elasticbeanstalk:environment:process:default",
                            OptionName = "HealthCheckPath",
                            Value = settings.HealthCheckURL
                        }
                    );
                }
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

            if (settings.ElasticBeanstalkManagedPlatformUpdates.ManagedActionsEnabled)
            {
                if (BeanstalkServiceRole == null)
                    throw new InvalidOrMissingConfigurationException("The Elastic Beanstalk service role cannot be null");

                optionSettingProperties.Add(new CfnEnvironment.OptionSettingProperty
                {
                    Namespace = "aws:elasticbeanstalk:environment",
                    OptionName = "ServiceRole",
                    Value = BeanstalkServiceRole.RoleArn
                });

                optionSettingProperties.Add(new CfnEnvironment.OptionSettingProperty
                {
                    Namespace = "aws:elasticbeanstalk:managedactions",
                    OptionName = "PreferredStartTime",
                    Value = settings.ElasticBeanstalkManagedPlatformUpdates.PreferredStartTime
                });

                optionSettingProperties.Add(new CfnEnvironment.OptionSettingProperty
                {
                    Namespace = "aws:elasticbeanstalk:managedactions:platformupdate",
                    OptionName = "UpdateLevel",
                    Value = settings.ElasticBeanstalkManagedPlatformUpdates.UpdateLevel
                });
            }

            if (!string.IsNullOrEmpty(settings.ReverseProxy))
            {
                optionSettingProperties.Add(
                    new CfnEnvironment.OptionSettingProperty
                    {
                        Namespace = "aws:elasticbeanstalk:environment:proxy",
                        OptionName = "ProxyServer",
                        Value = settings.ReverseProxy
                    }
                );
            }
            
            if (settings.ElasticBeanstalkRollingUpdates.RollingUpdatesEnabled)
            {
                optionSettingProperties.Add(new CfnEnvironment.OptionSettingProperty
                {
                    Namespace = "aws:autoscaling:updatepolicy:rollingupdate",
                    OptionName = "RollingUpdateEnabled",
                    Value = settings.ElasticBeanstalkRollingUpdates.RollingUpdatesEnabled.ToString().ToLower()
                });

                optionSettingProperties.Add(new CfnEnvironment.OptionSettingProperty
                {
                    Namespace = "aws:autoscaling:updatepolicy:rollingupdate",
                    OptionName = "RollingUpdateType",
                    Value = settings.ElasticBeanstalkRollingUpdates.RollingUpdateType
                });

                optionSettingProperties.Add(new CfnEnvironment.OptionSettingProperty
                {
                    Namespace = "aws:autoscaling:updatepolicy:rollingupdate",
                    OptionName = "Timeout",
                    Value = settings.ElasticBeanstalkRollingUpdates.Timeout
                });

                if (settings.ElasticBeanstalkRollingUpdates.MaxBatchSize != null)
                {
                    optionSettingProperties.Add(new CfnEnvironment.OptionSettingProperty
                    {
                        Namespace = "aws:autoscaling:updatepolicy:rollingupdate",
                        OptionName = "MaxBatchSize",
                        Value = settings.ElasticBeanstalkRollingUpdates.MaxBatchSize.ToString()
                    });
                }

                if (settings.ElasticBeanstalkRollingUpdates.MinInstancesInService != null)
                {
                    optionSettingProperties.Add(new CfnEnvironment.OptionSettingProperty
                    {
                        Namespace = "aws:autoscaling:updatepolicy:rollingupdate",
                        OptionName = "MinInstancesInService",
                        Value = settings.ElasticBeanstalkRollingUpdates.MinInstancesInService.ToString()
                    });
                }

                if (settings.ElasticBeanstalkRollingUpdates.PauseTime != null)
                {
                    optionSettingProperties.Add(new CfnEnvironment.OptionSettingProperty
                    {
                        Namespace = "aws:autoscaling:updatepolicy:rollingupdate",
                        OptionName = "PauseTime",
                        Value = settings.ElasticBeanstalkRollingUpdates.PauseTime
                    });
                }
            }

            if (settings.ElasticBeanstalkEnvironmentVariables != null)
            {
                foreach (var (key, value) in settings.ElasticBeanstalkEnvironmentVariables)
                {
                    optionSettingProperties.Add(new CfnEnvironment.OptionSettingProperty
                    {
                        Namespace = "aws:elasticbeanstalk:application:environment",
                        OptionName = key,
                        Value = value
                    });
                }
            }

            if (settings.VPC.UseVPC)
            {
                if (settings.VPC.CreateNew)
                {
                    if (AppVpc == null)
                        throw new InvalidOperationException($"{nameof(AppVpc)} has not been set. The {nameof(ConfigureVpc)} method should be called before {nameof(ConfigureBeanstalkEnvironment)}");

                    optionSettingProperties.Add(new CfnEnvironment.OptionSettingProperty
                    {
                        Namespace = "aws:ec2:vpc",
                        OptionName = "VPCId",
                        Value = AppVpc.VpcId
                    });

                    if (settings.EnvironmentType.Equals(ENVIRONMENTTYPE_SINGLEINSTANCE))
                    {
                        optionSettingProperties.Add(new CfnEnvironment.OptionSettingProperty
                        {
                            Namespace = "aws:ec2:vpc",
                            OptionName = "Subnets",
                            Value = string.Join(",", AppVpc.PublicSubnets.Select(x => x.SubnetId))
                        });
                    }
                    else if (settings.EnvironmentType.Equals(ENVIRONMENTTYPE_LOADBALANCED))
                    {
                        optionSettingProperties.Add(new CfnEnvironment.OptionSettingProperty
                        {
                            Namespace = "aws:ec2:vpc",
                            OptionName = "Subnets",
                            Value = string.Join(",", AppVpc.PrivateSubnets.Select(x => x.SubnetId))
                        });
                        optionSettingProperties.Add(new CfnEnvironment.OptionSettingProperty
                        {
                            Namespace = "aws:ec2:vpc",
                            OptionName = "ELBSubnets",
                            Value = string.Join(",", AppVpc.PublicSubnets.Select(x => x.SubnetId))
                        });
                    }

                    optionSettingProperties.Add(new CfnEnvironment.OptionSettingProperty
                    {
                        Namespace = "aws:autoscaling:launchconfiguration",
                        OptionName = "SecurityGroups",
                        Value = AppVpc.VpcDefaultSecurityGroup
                    });
                }
                else
                {
                    optionSettingProperties.Add(new CfnEnvironment.OptionSettingProperty
                    {
                        Namespace = "aws:ec2:vpc",
                        OptionName = "VPCId",
                        Value = settings.VPC.VpcId
                    });

                    if (settings.VPC.Subnets.Any())
                    {
                        optionSettingProperties.Add(new CfnEnvironment.OptionSettingProperty
                        {
                            Namespace = "aws:ec2:vpc",
                            OptionName = "Subnets",
                            Value = string.Join(",", settings.VPC.Subnets)
                        });

                        if (settings.VPC.SecurityGroups.Any())
                        {
                            optionSettingProperties.Add(new CfnEnvironment.OptionSettingProperty
                            {
                                Namespace = "aws:autoscaling:launchconfiguration",
                                OptionName = "SecurityGroups",
                                Value = string.Join(",", settings.VPC.SecurityGroups)
                            });
                        }
                    }
                }
            }

            BeanstalkEnvironment = new CfnEnvironment(this, nameof(BeanstalkEnvironment), InvokeCustomizeCDKPropsEvent(nameof(BeanstalkEnvironment), this, new CfnEnvironmentProps
            {
                EnvironmentName = settings.BeanstalkEnvironment.EnvironmentName,
                ApplicationName = beanstalkApplicationName,
                PlatformArn = settings.ElasticBeanstalkPlatformArn,
                OptionSettings = optionSettingProperties.ToArray(),
                CnamePrefix = !string.IsNullOrEmpty(settings.CNamePrefix) ? settings.CNamePrefix : null,
                // This line is critical - reference the label created in this same stack
                VersionLabel = ApplicationVersion.Ref,
            }));
        }
    }
}
