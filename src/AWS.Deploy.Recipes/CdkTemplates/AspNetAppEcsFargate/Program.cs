// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using Amazon.CDK;
using AWS.Deploy.Recipes.CDK.Common;
using AspNetAppEcsFargate.Configurations;
using Microsoft.Extensions.Configuration;
using Environment = Amazon.CDK.Environment;

namespace AspNetAppEcsFargate
{
    sealed class Program
    {
        public static void Main()
        {
            var app = new App();

            var builder = new ConfigurationBuilder().AddAWSDeployToolConfiguration(app);
            var recipeProps = builder.Build().Get<RecipeProps<Configuration>>();
            if (recipeProps is null)
            {
                throw new InvalidOrMissingConfigurationException("The configuration is missing for the selected recipe.");
            }
            var appStackProps = new DeployToolStackProps<Configuration>(recipeProps)
            {
                Env = new Environment
                {
                    Account = recipeProps.AWSAccountId,
                    Region = recipeProps.AWSRegion
                }
            };

            // The RegisterStack method is used to set identifying information on the stack
            // for the recipe used to deploy the application and preserve the settings used in the recipe
            // to allow redeployment. The information is stored as CloudFormation tags and metadata inside
            // the generated CloudFormation template.
            CDKRecipeSetup.RegisterStack<Configuration>(new AppStack(app, appStackProps), appStackProps.RecipeProps);

            app.Synth();
        }
    }
}
