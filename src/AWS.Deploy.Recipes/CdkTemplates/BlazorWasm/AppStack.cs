using Amazon.CDK;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.S3.Deployment;
using AWS.Deploy.Recipes.CDK.Common;
using System.IO;
using System.Collections.Generic;
using BlazorWasm.Configurations;

namespace BlazorWasm
{
    public class AppStack : Stack
    {
        internal AppStack(Construct scope, RecipeConfiguration<Configuration> recipeConfiguration, IStackProps? props = null)
            : base(scope, recipeConfiguration.StackName, props)
        {
            var bucketProps = new BucketProps
            {
                WebsiteIndexDocument = recipeConfiguration.Settings.IndexDocument,
                PublicReadAccess = true,

                // Turn on delete objects so deployed Blazor application is deleted when the stack is deleted.
                AutoDeleteObjects = true,
                RemovalPolicy = RemovalPolicy.DESTROY
            };

            if(recipeConfiguration.Settings.Redirect404ToRoot)
            {
                bucketProps.WebsiteRoutingRules = new IRoutingRule[] {
                    new RoutingRule
                    {
                        Condition = new RoutingRuleCondition
                        {
                            HttpErrorCodeReturnedEquals = "404"
                        },
                        ReplaceKey = ReplaceKey.With("")
                    }
                };
            }

            if(!string.IsNullOrEmpty(recipeConfiguration.Settings.ErrorDocument))
            {
                bucketProps.WebsiteErrorDocument = recipeConfiguration.Settings.ErrorDocument;
            }

            var bucket = new Bucket(this, "BlazorHost", bucketProps);

            if (string.IsNullOrEmpty(recipeConfiguration.DotnetPublishOutputDirectory))
                throw new InvalidOrMissingConfigurationException("The provided path containing the dotnet publish output is null or empty.");

            new BucketDeployment(this, "BlazorDeployment", new BucketDeploymentProps
            {
                Sources = new ISource[] { Source.Asset(Path.Combine(recipeConfiguration.DotnetPublishOutputDirectory, "wwwroot")) },
                DestinationBucket = bucket
            });

            new CfnOutput(this, "EndpointURL", new CfnOutputProps
            {
                Value = $"http://{bucket.BucketWebsiteDomainName}/"
            });
        }
    }
}
