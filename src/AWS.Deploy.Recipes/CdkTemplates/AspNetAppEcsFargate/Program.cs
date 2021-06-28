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
        public static void Main(string[] args)
        {
            var app = new App();

            var builder = new ConfigurationBuilder().AddAWSDeployToolConfiguration(app);
            var recipeConfiguration = builder.Build().Get<RecipeConfiguration<Configuration>>();

            CDKRecipeSetup.RegisterStack<Configuration>(new AppStack(app, recipeConfiguration, new StackProps
            {
                Env = new Environment
                {
                    Account = recipeConfiguration.AWSAccountId,
                    Region = recipeConfiguration.AWSRegion
                }
            }), recipeConfiguration);

            app.Synth();
        }
    }
}
