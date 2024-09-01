using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.Deploy.Constants
{
    internal static class RecipeIdentifier
    {
        // Recipe IDs
        public const string EXISTING_BEANSTALK_ENVIRONMENT_RECIPE_ID = "AspNetAppExistingBeanstalkEnvironment";
        public const string EXISTING_BEANSTALK_WINDOWS_ENVIRONMENT_RECIPE_ID = "AspNetAppExistingBeanstalkWindowsEnvironment";
        public const string PUSH_TO_ECR_RECIPE_ID = "PushContainerImageEcr";

        // Replacement Tokens
        public const string REPLACE_TOKEN_STACK_NAME = "{StackName}";
        public const string REPLACE_TOKEN_LATEST_DOTNET_BEANSTALK_PLATFORM_ARN = "{LatestDotnetBeanstalkPlatformArn}";
        public const string REPLACE_TOKEN_LATEST_DOTNET_WINDOWS_BEANSTALK_PLATFORM_ARN = "{LatestDotnetWindowsBeanstalkPlatformArn}";
        public const string REPLACE_TOKEN_ECR_REPOSITORY_NAME = "{DefaultECRRepositoryName}";
        public const string REPLACE_TOKEN_ECR_IMAGE_TAG = "{DefaultECRImageTag}";
        public const string REPLACE_TOKEN_DOCKERFILE_PATH = "{DockerfilePath}";
        public const string REPLACE_TOKEN_DEFAULT_VPC_ID = "{DefaultVpcId}";
        public const string REPLACE_TOKEN_HAS_DEFAULT_VPC = "{HasDefaultVpc}";
        public const string REPLACE_TOKEN_HAS_NOT_VPCS = "{HasNotVpcs}";
        public const string REPLACE_TOKEN_DEFAULT_CONTAINER_PORT = "{DefaultContainerPort}";
        public const string REPLACE_TOKEN_DEFAULT_ENVIRONMENT_ARCHITECTURE = "{DefaultEnvironmentArchitecture}";

        /// <summary>
        /// Id for the 'dotnet publish --configuration' recipe option
        /// </summary>
        public const string DotnetPublishConfigurationOptionId = "DotnetBuildConfiguration";

        /// <summary>
        /// Id for the additional args for 'dotnet publish' recipe option
        /// </summary>
        public const string DotnetPublishArgsOptionId = "DotnetPublishArgs";

        /// <summary>
        /// Id for the 'dotnet build --self-contained' recipe option
        /// </summary>
        public const string DotnetPublishSelfContainedBuildOptionId = "SelfContainedBuild";

        /// <summary>
        /// Id for the environment architecture recipe option
        /// </summary>
        public const string EnvironmentArchitectureOptionId = "EnvironmentArchitecture";

        public const string TARGET_SERVICE_ELASTIC_BEANSTALK = "AWS Elastic Beanstalk";
    }
}
