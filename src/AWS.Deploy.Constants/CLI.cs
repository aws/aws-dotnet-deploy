// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.Constants
{
    public static class CLI
    {
        // Represents the default STS AWS region that is used for the purposes of
        // retrieving the caller identity and determining if a user is in an opt-in region.
        public const string DEFAULT_STS_AWS_REGION = "us-east-1";

        // labels
        public const string CREATE_NEW_LABEL = "*** Create new ***";
        public const string DEFAULT_LABEL = "*** Default ***";
        public const string EMPTY_LABEL = "*** Empty ***";
        public const string CREATE_NEW_APPLICATION_LABEL = "*** Deploy to a new Cloud Application ***";

        // input prompts
        public const string PROMPT_NEW_STACK_NAME = "Enter the name of the new CloudFormationStack stack";
        public const string PROMPT_ECR_REPOSITORY_NAME = "Enter the name of the ECR repository";
        public const string PROMPT_CHOOSE_DEPLOYMENT_TARGET = "Choose deployment target";

        public const string CLI_APP_NAME = "AWS .NET Deployment Tool";
        public const string WORKSPACE_ENV_VARIABLE = "AWS_DOTNET_DEPLOYTOOL_WORKSPACE";

        public const string TOOL_NAME = "dotnet-aws";
    }
}
