using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

using Amazon.CDK;

namespace AWS.Deploy.Recipes.CDK.Common
{
    /// <summary>
    /// This class contains methods for setting up a CDK stack as a to be managed by the AWS Deploy Tool.
    /// </summary>
    public static class CDKRecipeSetup
    {
        /// <summary>
        /// Add the AWS Deploy Tool configuration as a source to the IConfigurationBuilder.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IConfigurationBuilder AddAWSDeployToolConfiguration(this IConfigurationBuilder builder, App app)
        {
            builder.AddJsonFile(CDKRecipeSetup.DetermineAWSDeployToolSettingsFile(app), false, false);
            return builder;
        }

        /// <summary>
        /// Determine the location of the JSON config file written by the AWS Deploy Tool.
        ///
        /// Currently only the appsettings.json is used which is created by the AWS Deploy Tool. The "args" parameter
        /// is passed in so in the future the file could be customized by the AWS Deploy Tool.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string DetermineAWSDeployToolSettingsFile(App app)
        {
            var settingsPath = app.Node.TryGetContext(CloudFormationIdentifierContants.SettingsPathCDKContextParameter)?.ToString();

            if(string.IsNullOrEmpty(settingsPath))
            {
                throw new InvalidAWSDeployToolSettingsException("Missing CDK context parameter specifing the AWS Deploy Tool settings file.");
            }
            if(!File.Exists(settingsPath))
            {
                throw new InvalidAWSDeployToolSettingsException($"AWS Deploy Tool settings file {settingsPath} can not be found.");
            }

            return settingsPath;
        }

        /// <summary>
        /// Tags the stack to identify the stack as an AWS Deploy Tool stack. Appropiate metadata as also applied to the generated
        /// template to identify the recipe used as well as the last AWS Deploy Tool settings. This is required to support the
        /// AWS Deploy Tool to be able to redeploy new versions of the application to AWS.
        /// </summary>
        /// <typeparam name="C">The configuration type defined as part of the recipe that contains all of the recipe specific settings.</typeparam>
        /// <param name="stack"></param>
        /// <param name="recipeConfiguration"></param>
        public static void RegisterStack<C>(Stack stack, RecipeConfiguration<C> recipeConfiguration)
        {
            // Set the AWS Deploy Tool tag which also identifies the recipe used.
            stack.Tags.SetTag(CloudFormationIdentifierContants.StackTag, $"{recipeConfiguration.RecipeId}");

            // Serializes all AWS Deploy Tool settings.
            var json = JsonSerializer.Serialize(recipeConfiguration.Settings, new JsonSerializerOptions { WriteIndented = false });

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
            metadata[CloudFormationIdentifierContants.StackMetadataSettings] = json;
            metadata[CloudFormationIdentifierContants.StackMetadataRecipeId] = recipeConfiguration.RecipeId;
            metadata[CloudFormationIdentifierContants.StackMetadataRecipeVersion] = recipeConfiguration.RecipeVersion;

            // For the CDK to pick up the changes to the metadata .NET Dictionary you have to reassign the Metadata property.
            stack.TemplateOptions.Metadata = metadata;

            // CloudFormation tags are propagated to resources created by the stack. In case of Beanstalk deployment a second CloudFormation stack is
            // launched which will also have the AWS Deploy Tool tag. To differentiate these additional stacks a special AWS Deploy Tool prefix
            // is added to the description.
            if (string.IsNullOrEmpty(stack.TemplateOptions.Description))
            {
                stack.TemplateOptions.Description = CloudFormationIdentifierContants.StackDescriptionPrefix;
            }
            else
            {
                stack.TemplateOptions.Description = CloudFormationIdentifierContants.StackDescriptionPrefix + ": " + stack.TemplateOptions.Description;
            }
        }
    }
}
