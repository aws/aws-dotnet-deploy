using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.Deploy.Constants
{
    internal static class CLI
    {
        // Represents the default STS AWS region that is used for the purposes of
        // retrieving the caller identity and determining if a user is in an opt-in region.
        public const string DEFAULT_STS_AWS_REGION = "us-east-1";

        public const string CREATE_NEW_LABEL = "*** Create new ***";
        public const string DEFAULT_LABEL = "*** Default ***";
        public const string EMPTY_LABEL = "*** Empty ***";
        public const string CREATE_NEW_STACK_LABEL = "*** Deploy to a new CloudFormation stack ***";
        public const string PROMPT_NEW_STACK_NAME = "Enter the name of the new CloudFormationStack stack";
        public const string PROMPT_CHOOSE_DEPLOYMENT_TARGET = "Choose deployment target";

        public const string CLI_APP_NAME = "AWS .NET Deployment Tool";

        public const string REPLACE_TOKEN_LATEST_DOTNET_BEANSTALK_PLATFORM_ARN = "{LatestDotnetBeanstalkPlatformArn}";
        public const string REPLACE_TOKEN_STACK_NAME = "{StackName}";
    }
}
