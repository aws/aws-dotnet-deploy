using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.Deploy.Constants
{
    internal static class CLI
    {
        public const string CREATE_NEW_LABEL = "*** Create new ***";
        public const string DEFAULT_LABEL = "*** Default ***";
        public const string EMPTY_LABEL = "*** Empty ***";
        public const string CREATE_NEW_STACK_LABEL = "*** Deploy to a new CloudFormation stack ***";
        public const string PROMPT_NEW_STACK_NAME = "Enter the name of the new CloudFormationStack stack";
        public const string PROMPT_CHOOSE_DEPLOYMENT_TARGET = "Choose deployment target";

        public const string CLI_APP_NAME = "AWS .NET Deployment Tool";
    }
}
