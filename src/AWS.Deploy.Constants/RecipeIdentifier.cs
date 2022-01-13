using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.Deploy.Constants
{
    internal static class RecipeIdentifier
    {
        public const string REPLACE_TOKEN_STACK_NAME = "{StackName}";
        public const string REPLACE_TOKEN_LATEST_DOTNET_BEANSTALK_PLATFORM_ARN = "{LatestDotnetBeanstalkPlatformArn}";
        public const string EXISTING_BEANSTALK_ENVIRONMENT_RECIPE_ID = "AspNetAppExistingBeanstalkEnvironment";
    }
}
