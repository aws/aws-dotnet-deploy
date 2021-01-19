using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.ElasticBeanstalk;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.S3.Assets;

namespace AspNetAppElasticBeanstalkLinux
{
    public class AppStack : Stack
    {
        private const string ENVIRONMENTTYPE_SINGLEINSTANCE = "SingleInstance";
        private const string ENVIRONMENTTYPE_LOADBALANCED = "LoadBalanced";
        internal AppStack(Construct scope, string id, Configuration configuration, IStackProps props = null) : base(scope, id, props)
        {
            var asset = new Asset(this, "Asset", new AssetProps
            {
                Path = configuration.AssetPath
            });

            CfnApplication application = null;

            // Create an app version from the S3 asset defined above
            // The S3 "putObject" will occur first before CF generates the template
            var applicationVersion = new CfnApplicationVersion(this, "ApplicationVersion", new CfnApplicationVersionProps
            {
                ApplicationName = configuration.ApplicationName,
                SourceBundle = new CfnApplicationVersion.SourceBundleProperty
                {
                    S3Bucket = asset.S3BucketName,
                    S3Key = asset.S3ObjectKey
                }
            });

            if (!configuration.UseExistingApplication)
            {
                application = new CfnApplication(this, "Application", new CfnApplicationProps
                {
                    ApplicationName = configuration.ApplicationName
                });

                applicationVersion.AddDependsOn(application);
            }

            var role = new Role(this, "Role", new RoleProps
            {
                AssumedBy = new ServicePrincipal("ec2.amazonaws.com"),
                RoleName = configuration.ApplicationIAMRole,

                // https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/iam-instanceprofile.html
                ManagedPolicies = new[]
                {
                    ManagedPolicy.FromAwsManagedPolicyName("AWSElasticBeanstalkWebTier"),
                    ManagedPolicy.FromAwsManagedPolicyName("AWSElasticBeanstalkMulticontainerDocker"),
                    ManagedPolicy.FromAwsManagedPolicyName("AWSElasticBeanstalkWorkerTier")
                }
            });

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
                        OptionName = "InstanceType",
                        Value= configuration.InstanceType
                   },
                   new CfnEnvironment.OptionSettingProperty {
                        Namespace = "aws:autoscaling:launchconfiguration",
                        OptionName =  "IamInstanceProfile",
                        Value = instanceProfile.AttrArn
                   },
                   new CfnEnvironment.OptionSettingProperty {
                        Namespace = "aws:elasticbeanstalk:environment",
                        OptionName =  "EnvironmentType",
                        Value = configuration.EnvironmentType
                   }
                };

            if (configuration.EnvironmentType.Equals(ENVIRONMENTTYPE_LOADBALANCED))
            {
                optionSettingProperties.Add(
                    new CfnEnvironment.OptionSettingProperty
                    {
                        Namespace = "aws:elasticbeanstalk:environment",
                        OptionName = "LoadBalancerType",
                        Value = configuration.LoadBalancerType
                    }
                );
            }

            new CfnEnvironment(this, "Environment", new CfnEnvironmentProps
            {
                EnvironmentName = configuration.EnvironmentName,
                ApplicationName = configuration.ApplicationName,
                SolutionStackName = configuration.SolutionStackName,
                OptionSettings = optionSettingProperties.ToArray(),
                // This line is critical - reference the label created in this same stack
                VersionLabel = applicationVersion.Ref,
            });
        }
    }
}
