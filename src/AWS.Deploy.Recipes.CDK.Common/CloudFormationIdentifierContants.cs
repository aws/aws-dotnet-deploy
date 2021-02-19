// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.Deploy.Recipes.CDK.Common
{
    public class CloudFormationIdentifierContants
    {
        /// <summary>
        /// The CDK context parameter name used to pass in the location of the AWS Deploy Tool's settings file.
        /// </summary>
        public const string SettingsPathCDKContextParameter = "aws-deploy-tool-setting";

        /// <summary>
        /// The name of the tag applied to the CloudFormation stack.
        /// </summary>
        public const string StackTag = "aws-dotnet-deploy";

        /// <summary>
        /// AWS Deploy Tool CloudFormation stacks will prefix the description with this value to help identify stacks that are created by the AWS Deploy Tool.
        /// </summary>
        public const string StackDescriptionPrefix = "AWSDotnetDeployCDKStack";

        /// <summary>
        /// The CloudFormation template metadata key used to hold the last used settings to deploy the application.
        /// </summary>
        public const string StackMetadataSettings = "aws-dotnet-deploy-settings";

        /// <summary>
        /// The CloudFormation template metadata key for storing the id of the AWS Deploy Tool recipe.
        /// </summary>
        public const string StackMetadataRecipeId = "aws-dotnet-deploy-recipe-id";

        /// <summary>
        /// The CloudFormation template metadata key for storing the version of the AWS Deploy Tool recipe.
        /// </summary>
        public const string StackMetadataRecipeVersion = "aws-dotnet-deploy-recipe-version";
    }
}
