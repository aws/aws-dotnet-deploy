using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

using Amazon.CDK;
using System.Text.Json.Serialization;

namespace AWS.Deploy.Recipes.CDK.Common
{
    /// <summary>
    /// This class contains methods for setting up a CDK stack to be managed by the AWS .NET deployment tool.
    /// </summary>
    public static class CDKRecipeSetup
    {
        /// <summary>
        /// Tags the stack to identify it as an AWS .NET deployment tool Clould Application. Appropriate metadata as also applied to the generated
        /// template to identify the recipe used as well as the last AWS .NET deployment tool settings. This is required to support the
        /// AWS .NET deployment tool to be able to redeploy new versions of the application to AWS.
        /// </summary>
        /// <typeparam name="C">The configuration type defined as part of the recipe that contains all of the recipe specific settings.</typeparam>
        /// <param name="stack"></param>
        /// <param name="recipeConfiguration"></param>
        public static void RegisterStack<C>(Stack stack, IRecipeProps<C> recipeConfiguration)
        {
            // Set the AWS .NET deployment tool tag which also identifies the recipe used.
            stack.Tags.SetTag(Constants.CloudFormationIdentifier.STACK_TAG, $"{recipeConfiguration.RecipeId}");

            // Serializes all AWS .NET deployment tool settings.
            var json = JsonSerializer.Serialize(
                recipeConfiguration.Settings,
                new JsonSerializerOptions
                {
                    WriteIndented = false,
                    Converters = { new JsonStringEnumConverter() }
                });

            Dictionary<string, object> metadata;
            if(stack.TemplateOptions.Metadata?.Count > 0)
            {
                metadata = new Dictionary<string, object>(stack.TemplateOptions.Metadata);
            }
            else
            {
                metadata = new Dictionary<string, object>();
            }

            // Save the settings, recipe id and version as metadata to the CloudFormation template.
            metadata[Constants.CloudFormationIdentifier.STACK_METADATA_SETTINGS] = json;
            metadata[Constants.CloudFormationIdentifier.STACK_METADATA_RECIPE_ID] = recipeConfiguration.RecipeId;
            metadata[Constants.CloudFormationIdentifier.STACK_METADATA_RECIPE_VERSION] = recipeConfiguration.RecipeVersion;

            // For the CDK to pick up the changes to the metadata .NET Dictionary you have to reassign the Metadata property.
            stack.TemplateOptions.Metadata = metadata;

            // CloudFormation tags are propagated to resources created by the stack. In case of Beanstalk deployment a second CloudFormation stack is
            // launched which will also have the AWS .NET deployment tool tag. To differentiate these additional stacks a special AWS .NET deployment tool prefix
            // is added to the description.
            if (string.IsNullOrEmpty(stack.TemplateOptions.Description))
            {
                stack.TemplateOptions.Description = Constants.CloudFormationIdentifier.STACK_DESCRIPTION_PREFIX;
            }
            else
            {
                stack.TemplateOptions.Description = $"{Constants.CloudFormationIdentifier.STACK_DESCRIPTION_PREFIX}: {stack.TemplateOptions.Description}";
            }
        }
    }
}
