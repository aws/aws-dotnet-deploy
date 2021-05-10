// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;

namespace AWS.Deploy.Recipes.CDK.Common
{
    /// <summary>
    /// A representation of the settings transferred from the AWS .NET deployment tool to the CDK project.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RecipeConfiguration<T>
    {
        /// <summary>
        /// The name of the CloudFormation stack
        /// </summary>
        public string StackName { get; set; }

        /// <summary>
        /// The path to the .NET project to deploy to AWS.
        /// </summary>
        public string ProjectPath { get; set; }

        /// <summary>
        /// The ECR Repository Name where the docker image will be pushed to.
        /// </summary>
        public string? ECRRepositoryName { get; set; }

        /// <summary>
        /// The ECR Image Tag of the docker image.
        /// </summary>
        public string? ECRImageTag { get; set; }

        /// <summary>
        /// The path of the zip file containing the assemblies produced by the dotnet publish command.
        /// </summary>
        public string? DotnetPublishZipPath { get; set; }

        /// <summary>
        /// The directory containing the assemblies produced by the dotnet publish command.
        /// </summary>
        public string? DotnetPublishOutputDirectory { get; set; }

        /// <summary>
        /// The ID of the recipe being used to deploy the application.
        /// </summary>
        public string RecipeId { get; set; }

        /// <summary>
        /// The version of the recipe being used to deploy the application.
        /// </summary>
        public string RecipeVersion { get; set; }

        /// <summary>
        /// The configured settings made by the frontend. These are recipe specific and defined in the recipe's definition.
        /// </summary>
        public T Settings { get; set; }

        /// A parameterless constructor is needed for <see cref="Microsoft.Extensions.Configuration.ConfigurationBuilder"/>
        /// or the classes will fail to initialize.
        /// The warnings are disabled since a parameterless constructor will allow non-nullable properties to be initialized with null values.
#nullable disable warnings
        public RecipeConfiguration()
        {

        }
#nullable restore warnings

        public RecipeConfiguration(string stackName, string projectPath, string recipeId, string recipeVersion, T settings)
        {
            StackName = stackName;
            ProjectPath = projectPath;
            RecipeId = recipeId;
            RecipeVersion = recipeVersion;
            Settings = settings;
        }
    }
}
