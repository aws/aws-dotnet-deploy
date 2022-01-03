// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.ElasticBeanstalk;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.S3.Assets;
using AWS.Deploy.Recipes.CDK.Common;
using Amazon.CDK.CustomResources;

using AspNetAppElasticBeanstalkLinux.Configurations;
using Amazon.CDK.AWS.Lambda;

// This is a generated file from the original deployment recipe. It is recommended to not modify this file in order
// to allow easy updates to the file when the original recipe that this project was created from has updates.
// To customize the CDK constructs created in this file you should use the AppStack.CustomizeCDKProps() method.

namespace AspNetAppElasticBeanstalkLinux
{
    using static AWS.Deploy.Recipes.CDK.Common.CDKRecipeCustomizer<Recipe>;

    public class Recipe : Construct
    {
        private readonly string _awsAccountId;
        private readonly string _awsRegion;

        public const string ENVIRONMENTTYPE_SINGLEINSTANCE = "SingleInstance";
        public const string ENVIRONMENTTYPE_LOADBALANCED = "LoadBalanced";

        public const string LOADBALANCERTYPE_APPLICATION = "application";

        public const string REVERSEPROXY_NGINX = "nginx";
        
        public const string ENHANCED_HEALTH_REPORTING = "enhanced";

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
            if (string.IsNullOrEmpty(props.AWSAccountId))
                throw new InvalidOrMissingConfigurationException($"{nameof(props.AWSAccountId)} cannot be null or empty");

            if (string.IsNullOrEmpty(props.AWSRegion))
                throw new InvalidOrMissingConfigurationException($"{nameof(props.AWSRegion)} cannot be null or empty");

            var settings = props.Settings;
            _awsAccountId = props.AWSAccountId;
            _awsRegion = props.AWSRegion;

            if (string.IsNullOrEmpty(props.DotnetPublishZipPath))
                throw new InvalidOrMissingConfigurationException("The provided path containing the dotnet publish zip file is null or empty.");

            ApplicationAsset = new Asset(this, "Asset", new AssetProps
            {
                Path = props.DotnetPublishZipPath
            });

            ConfigureIAM(settings);
            ConfigureApplication(settings);
            ConfigureBeanstalkEnvironment(settings);
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
        }

        private void ConfigureApplication(Configuration settings)
        {
            if (ApplicationAsset == null)
                throw new InvalidOperationException($"{nameof(ApplicationAsset)} has not been set.");

            // Create an app version from the S3 asset defined above
            // The S3 "putObject" will occur first before CF generates the template
            ApplicationVersion = new CfnApplicationVersion(this, nameof(ApplicationVersion), InvokeCustomizeCDKPropsEvent(nameof(ApplicationVersion), this, new CfnApplicationVersionProps
            {
                ApplicationName = settings.BeanstalkApplication.ApplicationName,
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
                    ApplicationName = settings.BeanstalkApplication.ApplicationName
                }));

                ApplicationVersion.AddDependsOn(BeanstalkApplication);
            }
        }

        private void ConfigureBeanstalkEnvironment(Configuration settings)
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
                BeanstalkServiceRole = new Role(this, nameof(BeanstalkServiceRole), InvokeCustomizeCDKPropsEvent(nameof(BeanstalkServiceRole), this, new RoleProps
                {
                    AssumedBy = new ServicePrincipal("elasticbeanstalk.amazonaws.com"),
                    ManagedPolicies = new[]
                    {
                        ManagedPolicy.FromAwsManagedPolicyName("AWSElasticBeanstalkManagedUpdatesCustomerRolePolicy")
                    }
                }));

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

            if (settings.BeanstalkEnvironment.CreateNew)
            {
                BeanstalkEnvironment = new CfnEnvironment(this, nameof(BeanstalkEnvironment), InvokeCustomizeCDKPropsEvent(nameof(BeanstalkEnvironment), this, new CfnEnvironmentProps
                {
                    EnvironmentName = settings.BeanstalkEnvironment.EnvironmentName,
                    ApplicationName = settings.BeanstalkApplication.ApplicationName,
                    PlatformArn = settings.ElasticBeanstalkPlatformArn,
                    OptionSettings = optionSettingProperties.ToArray(),
                    CnamePrefix = !string.IsNullOrEmpty(settings.CNamePrefix) ? settings.CNamePrefix : null,
                    // This line is critical - reference the label created in this same stack
                    VersionLabel = ApplicationVersion.Ref,
                }));
            }
            else
            {
                UpdateBeanStalkEvironment(settings);
            }
        }

        private void UpdateBeanStalkEvironment(Configuration settings)
        {   if (string.IsNullOrEmpty(settings.BeanstalkApplication?.ApplicationName))
                throw new InvalidOrMissingConfigurationException($"{nameof(settings.BeanstalkApplication.ApplicationName)} cannot be null or empty.");

            if (string.IsNullOrEmpty(settings.BeanstalkEnvironment?.EnvironmentName))
                throw new InvalidOrMissingConfigurationException($"{nameof(settings.BeanstalkEnvironment.EnvironmentName)} cannot be null or empty.");

            if (string.IsNullOrEmpty(ApplicationVersion?.Ref))
                throw new InvalidOrMissingConfigurationException($"{nameof(ApplicationVersion.Ref)} cannot be null or empty.");

            var environmentArn = $"arn:aws:elasticbeanstalk:{_awsRegion}:{_awsAccountId}:environment/{settings.BeanstalkApplication.ApplicationName}/{settings.BeanstalkEnvironment.EnvironmentName}";

            var customResourceProvider = new Provider(this, "EnvironmentUpdateCustomResourceProvider", new ProviderProps
            {
                OnEventHandler = GetOnEventHandler(environmentArn),
                IsCompleteHandler = GetIsCompleteHandler()
            });

            new CustomResource(this, "EnvironmentUpdateCustomResource", new CustomResourceProps
            {
                ServiceToken = customResourceProvider.ServiceToken,
                Properties = new Dictionary<string, object>()
                {
                    #pragma warning disable CS8604 // Disabling null reference warnings because we are explicitly checking for null at the start of the method.
                    { "ApplicationName", settings.BeanstalkApplication.ApplicationName },
                    { "EnvironmentName", settings.BeanstalkEnvironment.EnvironmentName },
                    { "VersionLabel", ApplicationVersion?.Ref }
                    #pragma warning restore CS8604
                }
            });
        }

        private IFunction GetOnEventHandler(string environmentArn)
        {
            IFunction onEventlambdaFunction = new Function(this, "OnEventlambdaFunction", new FunctionProps
            {
                Runtime = Runtime.PYTHON_3_9, // execution environment
                Code = Code.FromAsset("Generated/UpdateBeanstalkEnvironment/OnEventLambda"), // Code loaded from the disk path
                Handler = "OnEvent.handler", // file is "OnEvent.py", function is "handler"
                Role = GetOnEventLambdaRole(environmentArn),
                Timeout = Duration.Seconds(30)
            });

            return onEventlambdaFunction;
        }

        private IFunction GetIsCompleteHandler()
        {
            IFunction isCompletelambdaFunction = new Function(this, "IsCompletelambdaFunction", new FunctionProps
            {
                Runtime = Runtime.PYTHON_3_9, // execution environment
                Code = Code.FromAsset("Generated/UpdateBeanstalkEnvironment/IsCompleteLambda"), // Code loaded from the disk path
                Handler = "IsComplete.handler", // file is "IsComplete.py", function is "handler"
                Role = GetIsCompleteLambdaRole(),
                Timeout = Duration.Seconds(30)
            });

            return isCompletelambdaFunction;
        }

        private IRole GetOnEventLambdaRole(string environmentArn)
        {
            return new Role(this, "OnEventlambdaExecutionRole", InvokeCustomizeCDKPropsEvent("OnEventlambdaExecutionRole", this, new RoleProps
            {
                AssumedBy = new ServicePrincipal("lambda.amazonaws.com"),
                ManagedPolicies = new[]{ ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambdaBasicExecutionRole") },
                InlinePolicies = new Dictionary<string, PolicyDocument>()
                    {
                        {
                            "EnvironmentUpdatePolicy", new PolicyDocument(new PolicyDocumentProps
                            {
                                Statements = new[]
                                {
                                    new PolicyStatement(new PolicyStatementProps
                                    {
                                        Actions = new[]{ "elasticbeanstalk:UpdateEnvironment" },
                                        Resources = new[]{ environmentArn },
                                    }),
                                    new PolicyStatement(new PolicyStatementProps
                                    {
                                        Actions = new[]{ "s3:*", "cloudformation:*", "autoScaling:*" },
                                        Resources = new[]{ "*" },
                                    })
                                }
                            })
                        }
                    }
            }));
        }

        private IRole GetIsCompleteLambdaRole()
        {
            return new Role(this, "IsCompletelambdaExecutionRole", InvokeCustomizeCDKPropsEvent("IsCompletelambdaExecutionRole", this, new RoleProps
            {
                AssumedBy = new ServicePrincipal("lambda.amazonaws.com"),
                ManagedPolicies = new[] { ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambdaBasicExecutionRole") },
                InlinePolicies = new Dictionary<string, PolicyDocument>()
                    {
                        {
                            "IsEnvironmentUpdateCompletePolicy", new PolicyDocument(new PolicyDocumentProps
                            {
                                Statements = new[]
                                {
                                    new PolicyStatement(new PolicyStatementProps
                                    {
                                        Actions = new[]{ "elasticbeanstalk:DescribeEnvironments" },
                                        Resources = new[]{ "*" },
                                    })
                                }
                            })
                        }
                    }
            }));
        }
    }
}
