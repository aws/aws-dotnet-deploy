// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Amazon.CDK;
using Amazon.CDK.AWS.CloudFront;
using Amazon.CDK.AWS.CloudFront.Origins;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.S3.Deployment;
using AWS.Deploy.Recipes.CDK.Common;
using BlazorWasm.Configurations;

// This is a generated file from the original deployment recipe. It is recommended to not modify this file in order
// to allow easy updates to the file when the original recipe that this project was created from has updates.
// To customize the CDK constructs created in this file you should use the AppStack.CustomizeCDKProps() method.

namespace BlazorWasm
{
    using static AWS.Deploy.Recipes.CDK.Common.CDKRecipeCustomizer<Recipe>;

    public class Recipe : Construct
    {
        public Bucket? ContentS3Bucket { get; private set; }

        public BucketDeployment? ContentS3Deployment { get; private set; }

        public Distribution? CloudFrontDistribution { get; private set; }

        public string AccessLoggingBucket { get; } = "AccessLoggingBucket";

        public Recipe(Construct scope, IRecipeProps<Configuration> props)
            // The "Recipe" construct ID will be used as part of the CloudFormation logical ID. If the value is changed this will
            // change the expected values for the "DisplayedResources" in the corresponding recipe file.
            : base(scope, "Recipe")
        {

            ConfigureS3ContentBucket();
            ConfigureCloudFrontDistribution(props.Settings);
            ConfigureS3Deployment(props);
        }

        private void ConfigureS3ContentBucket()
        {
            var bucketProps = new BucketProps
            {
                // Turn on delete objects so deployed Blazor application is deleted when the stack is deleted.
                AutoDeleteObjects = true,
                RemovalPolicy = RemovalPolicy.DESTROY
            };

            ContentS3Bucket = new Bucket(this, nameof(ContentS3Bucket), InvokeCustomizeCDKPropsEvent(nameof(ContentS3Bucket), this, bucketProps));

            new CfnOutput(this, "S3ContentBucket", new CfnOutputProps
            {
                Description = "S3 bucket where Blazor application is uploaded to",
                Value = ContentS3Bucket.BucketName
            });
        }

        private void ConfigureCloudFrontDistribution(Configuration settings)
        {
            if (ContentS3Bucket == null)
                throw new InvalidOperationException($"{nameof(ContentS3Bucket)} has not been set. The {nameof(ConfigureS3ContentBucket)} method should be called before {nameof(ConfigureCloudFrontDistribution)}");

            var distributionProps = new DistributionProps
            {
                DefaultBehavior = new BehaviorOptions
                {
                    Origin = new S3Origin(ContentS3Bucket, new S3OriginProps())
                },
                DefaultRootObject = settings.IndexDocument,
                EnableIpv6 = settings.EnableIpv6,
                HttpVersion = settings.MaxHttpVersion,
                PriceClass = settings.PriceClass
            };

            var errorResponses = new List<ErrorResponse>();

            if (!string.IsNullOrEmpty(settings.ErrorDocument))
            {
                errorResponses.Add(
                    new ErrorResponse
                    {
                        ResponsePagePath = settings.ErrorDocument
                    }
                );
            }

            if (settings.Redirect404ToRoot)
            {
                errorResponses.Add(
                    new ErrorResponse
                    {
                        HttpStatus = 404,
                        ResponseHttpStatus = 200,
                        ResponsePagePath = "/"
                    }
                );

                // Since S3 returns back an access denied for objects that don't exist to CloudFront treat 403 as 404 not found.
                errorResponses.Add(
                    new ErrorResponse
                    {
                        HttpStatus = 403,
                        ResponseHttpStatus = 200,
                        ResponsePagePath = "/"
                    }
                );
            }

            if (errorResponses.Any())
            {
                distributionProps.ErrorResponses = errorResponses.ToArray();
            }

            if(settings.AccessLogging?.EnableAccessLogging == true)
            {
                distributionProps.EnableLogging = true;

                if(settings.AccessLogging.CreateLoggingS3Bucket)
                {
                    var loggingBucket = new Bucket(this, nameof(AccessLoggingBucket), InvokeCustomizeCDKPropsEvent(nameof(AccessLoggingBucket), this, new BucketProps
                    {
                        RemovalPolicy = RemovalPolicy.RETAIN,
                    }));

                    distributionProps.LogBucket = loggingBucket;

                    new CfnOutput(this, "S3AccessLoggingBucket", new CfnOutputProps
                    {
                        Description = "S3 bucket storing access logs. Bucket and logs will be retained after deployment is deleted.",
                        Value = distributionProps.LogBucket.BucketName
                    });
                }
                else if(!string.IsNullOrEmpty(settings.AccessLogging.ExistingS3LoggingBucket))
                {
                    distributionProps.LogBucket = Bucket.FromBucketName(this, nameof(AccessLoggingBucket), settings.AccessLogging.ExistingS3LoggingBucket);
                }

                if(!string.IsNullOrEmpty(settings.AccessLogging.LoggingS3KeyPrefix))
                {
                    distributionProps.LogFilePrefix = settings.AccessLogging.LoggingS3KeyPrefix;
                }

                distributionProps.LogIncludesCookies = settings.AccessLogging.LogIncludesCookies;
            }

            if(!string.IsNullOrEmpty(settings.WebAclId))
            {
                distributionProps.WebAclId = settings.WebAclId;
            }

            CloudFrontDistribution = new Distribution(this, nameof(CloudFrontDistribution), InvokeCustomizeCDKPropsEvent(nameof(CloudFrontDistribution), this, distributionProps));

            new CfnOutput(this, "EndpointURL", new CfnOutputProps
            {
                Description = "Endpoint to access application",
                Value = $"https://{CloudFrontDistribution.DomainName}/"
            });
        }

        private void ConfigureS3Deployment(IRecipeProps<Configuration> props)
        {
            if (ContentS3Bucket == null)
                throw new InvalidOperationException($"{nameof(ContentS3Bucket)} has not been set. The {nameof(ConfigureS3ContentBucket)} method should be called before {nameof(ContentS3Bucket)}");

            if (string.IsNullOrEmpty(props.DotnetPublishOutputDirectory))
                throw new InvalidOrMissingConfigurationException("The provided path containing the dotnet publish output is null or empty.");

            var bucketDeploymentProps = new BucketDeploymentProps
            {
                Sources = new ISource[] { Source.Asset(Path.Combine(props.DotnetPublishOutputDirectory, "wwwroot")) },
                DestinationBucket = ContentS3Bucket,
                MemoryLimit = 4096,

                Distribution = CloudFrontDistribution,
                DistributionPaths = new string[] { "/*" }
            };

            ContentS3Deployment = new BucketDeployment(this, nameof(ContentS3Deployment), InvokeCustomizeCDKPropsEvent(nameof(ContentS3Deployment), this, bucketDeploymentProps));
        }
    }
}
