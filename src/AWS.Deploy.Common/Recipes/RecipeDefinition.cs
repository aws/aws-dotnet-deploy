// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using AWS.Deploy.Common.Recipes.Validation;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AWS.Deploy.Common.Recipes
{
    /// <summary>
    /// Used to deserialize a JSON recipe definition into.
    /// </summary>
    public class RecipeDefinition
    {
        /// <summary>
        /// The unique id of the recipe. That value will be persisted in other config files so it should never be changed once the recipe definition is released.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The version of the recipe
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// The display friendly name of the recipe definition
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Indicates if this recipe should be presented as an option during new deployments.
        /// </summary>
        public bool DisableNewDeployments { get; set; }

        /// <summary>
        /// Description of the recipe informing the user what this recipe does and why it is recommended.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Short Description of the recipe informing the user what this recipe does and why it is recommended.
        /// </summary>
        public string ShortDescription { get; set; }

        /// <summary>
        /// A runtime property set when the recipe definition is loaded to the location of the definition. This property is used to find
        /// other assets like the CDK template project in relation to the recipe definition.
        /// </summary>
        public string? RecipePath { get; set; }

        /// <summary>
        /// The name of the AWS service the recipe deploys to. This is used for display purposes back to the user to inform then what AWS service the project
        /// will be deployed to.
        /// </summary>
        public string TargetService { get; set; }

        /// <summary>
        /// The environment platform the recipe deploys to. This is used to publish a self-contained .NET application for that platform.
        /// </summary>
        public TargetPlatform? TargetPlatform { get; set; }

        /// <summary>
        /// The CPU architecture that is supported by the recipe.
        /// </summary>
        public List<SupportedArchitecture>? SupportedArchitectures { get; set; }

        /// <summary>
        /// The list of DisplayedResources that lists logical CloudFormation IDs with a description.
        /// </summary>
        public List<DisplayedResource>? DisplayedResources { get; set; }

        /// <summary>
        /// Confirmation messages to display to the user before performing deployment.
        /// </summary>
        public DeploymentConfirmationType? DeploymentConfirmation { get; set; }

        /// <summary>
        /// The type of deployment to perform. This controls what other tool to use to perform the deployment. For example a value of `CdkProject` means that CDK should
        /// be used to perform the deployment.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public DeploymentTypes DeploymentType { get; set; }

        /// <summary>
        /// The type of deployment bundle the project should be converted into before deploying. For example turning the project into a build container or a zip file of the build binaries.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public DeploymentBundleTypes DeploymentBundle { get; set; }

        /// <summary>
        /// The location of the CDK project template relative from the recipe definition file.
        /// </summary>
        public string? CdkProjectTemplate { get; set; }

        /// <summary>
        /// The ID of the CDK project template for the template generator.
        /// </summary>
        public string? CdkProjectTemplateId { get; set; }

        /// <summary>
        /// The rules used by the recommendation engine to determine if the recipe definition is compatible with the project.
        /// </summary>
        public List<RecommendationRuleItem> RecommendationRules { get; set; } = new ();

        /// <summary>
        /// The list of categories for the recipes.
        /// </summary>
        public List<Category> Categories { get; set; } = new ();

        /// <summary>
        /// The settings that can be configured by the user before deploying.
        /// </summary>
        public List<OptionSettingItem> OptionSettings { get; set; } = new ();

        /// <summary>
        /// List of all validators that should be run against this recipe before deploying.
        /// </summary>
        public List<RecipeValidatorConfig> Validators { get; set; } = new ();
        
        /// <summary>
        /// The priority of the recipe. The highest priority is the top choice for deploying to.
        /// </summary>
        public int RecipePriority { get; set; }

        /// <summary>
        /// Flag to indicate if this recipe is generated while saving a deployment project.
        /// </summary>
        public bool PersistedDeploymentProject { get; set; }

        /// <summary>
        /// The recipe ID of the parent recipe which was used to create the CDK deployment project. 
        /// </summary>
        public string? BaseRecipeId { get; set; }

        /// <summary>
        /// The settings that are specific to the Deployment Bundle.
        /// These settings are populated by the <see cref="IRecipeHandler"/>.
        /// </summary>
        public List<OptionSettingItem> DeploymentBundleSettings = new();

        public RecipeDefinition(
            string id,
            string version,
            string name,
            DeploymentTypes deploymentType,
            DeploymentBundleTypes deploymentBundle,
            string cdkProjectTemplate,
            string cdkProjectTemplateId,
            string description,
            string shortDescription,
            string targetService)
        {
            Id = id;
            Version = version;
            Name = name;
            DeploymentType = deploymentType;
            DeploymentBundle = deploymentBundle;
            CdkProjectTemplate = cdkProjectTemplate;
            CdkProjectTemplateId = cdkProjectTemplateId;
            Description = description;
            ShortDescription = shortDescription;
            TargetService = targetService;
        }
        public override string ToString()
        {
            return $"{Name} ({Id})";
        }
    }
}
