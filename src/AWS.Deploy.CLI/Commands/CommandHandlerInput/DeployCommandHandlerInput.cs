// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AWS.Deploy.CLI.Commands.CommandHandlerInput
{
    public class DeployCommandHandlerInput
    {
        /// <summary>
        /// AWS credential profile used to make calls to AWS.
        /// </summary>
        public string? Profile { get; set; }

        /// <summary>
        /// AWS region to deploy the application to. For example, us-west-2.
        /// </summary>
        public string? Region { get; set; }

        /// <summary>
        /// Path to the project to deploy.
        /// </summary>
        public string? ProjectPath { get; set; }

        /// <summary>
        /// Name of the cloud application.
        /// </summary>
        public string? ApplicationName { get; set; }

        /// <summary>
        /// Path to the deployment settings file to be applied.
        /// </summary>
        public string? Apply { get; set; }

        /// <summary>
        /// Flag to enable diagnostic output.
        /// </summary>
        public bool Diagnostics { get; set; }

        /// <summary>
        /// Flag to disable interactivity to execute commands without any prompts.
        /// </summary>
        public bool Silent { get; set; }

        /// <summary>
        /// The absolute or relative path of the CDK project that will be used for deployment.
        /// </summary>
        public string? DeploymentProject { get; set; }

        /// <summary>
        /// The absolute or the relative JSON file path where the deployment settings will be saved. Only the settings modified by the user are persisted.
        /// </summary>
        public string? SaveSettings { get; set; }

        /// <summary>
        /// The absolute or the relative JSON file path where the deployment settings will be saved. All deployment settings are persisted.
        /// </summary>
        public string? SaveAllSettings { get; set; }
    }
}
