// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.CDK;
using Amazon.CDK.AWS.IAM;
using AWS.Deploy.Recipes.CDK.Common;
using AspNetAppBedrockAgentCore.Configurations;
using Constructs;

namespace AspNetAppBedrockAgentCore
{
    public class AppStack : Stack
    {
        private readonly Configuration _configuration;

        internal AppStack(Construct scope, IDeployToolStackProps<Configuration> props)
            : base(scope, props.StackName, props)
        {
            _configuration = props.RecipeProps.Settings;

            // Setup callback for generated construct to provide access to customize CDK properties before creating constructs.
            CDKRecipeCustomizer<Recipe>.CustomizeCDKProps += CustomizeCDKProps;

            // Create custom CDK constructs here that might need to be referenced in the CustomizeCDKProps.

            // Create the recipe defined CDK construct with all of its sub constructs.
            var generatedRecipe = new Recipe(this, props.RecipeProps);

            // Create additional CDK constructs here. The recipe's constructs can be accessed as properties on
            // the generatedRecipe variable.
        }

        /// <summary>
        /// This method can be used to customize the properties for any CDK construct being created by the
        /// Recipe's Generated/Recipe.cs construct. It is invoked before each resource is created, giving
        /// you a chance to modify the construct properties.
        /// </summary>
        private void CustomizeCDKProps(CustomizePropsEventArgs<Recipe> evnt)
        {
            // Example of how to customize the AgentCore Runtime role to add additional policies.
            //if (string.Equals(evnt.ResourceLogicalName, nameof(evnt.Construct.RuntimeRole)))
            //{
            //    if (evnt.Props is RoleProps props)
            //    {
            //        Console.WriteLine("Customizing AgentCore Runtime Role");
            //    }
            //}
        }
    }
}
