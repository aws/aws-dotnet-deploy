using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.Deploy.Constants
{
    internal static class CloudFormationIdentifier
    {
        /// <summary>
        /// The CDK context parameter name used to pass in the location of the AWS .NET deployment tool's settings file.
        /// </summary>
        public const string SETTINGS_PATH_CDK_CONTEXT_PARAMETER = "aws-deploy-tool-setting";

        /// <summary>
        /// The name of the identifier tag applied to CloudFormation stacks deployed by the AWS .NET deployment tool. The value of the
        /// tag is the recipe id used by the AWS .NET deployment tool.
        /// </summary>
        public const string STACK_TAG = "aws-dotnet-deploy";

        /// <summary>
        /// AWS .NET deployment tool CloudFormation stacks will prefix the description with this value to help identify stacks that are created by the AWS .NET deployment tool.
        /// </summary>
        public const string STACK_DESCRIPTION_PREFIX = "AWSDotnetDeployCDKStack";

        /// <summary>
        /// The CloudFormation template metadata key used to hold the last used recipe option settings to deploy the application.
        /// </summary>
        public const string STACK_METADATA_SETTINGS = "aws-dotnet-deploy-settings";

        /// <summary>
        /// The CloudFormation template metadata key used to hold the last used deployment bundle settings to deploy the application.
        /// </summary>
        public const string STACK_METADATA_DEPLOYMENT_BUNDLE_SETTINGS = "aws-dotnet-deploy-deployment-bundle-settings";

        /// <summary>
        /// The CloudFormation template metadata key for storing the id of the AWS .NET deployment tool recipe.
        /// </summary>
        public const string STACK_METADATA_RECIPE_ID = "aws-dotnet-deploy-recipe-id";

        /// <summary>
        /// The CloudFormation template metadata key for storing the version of the AWS .NET deployment tool recipe.
        /// </summary>
        public const string STACK_METADATA_RECIPE_VERSION = "aws-dotnet-deploy-recipe-version";
    }
}
