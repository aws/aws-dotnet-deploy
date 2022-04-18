// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Deploy.CLI.ServerMode.Controllers;

namespace AWS.Deploy.CLI.ServerMode.Models
{
    /// <summary>
    /// The response that will be returned by the <see cref="DeploymentController.GenerateCloudFormationTemplate(string)"/> operation.
    /// </summary>
    public class GenerateCloudFormationTemplateOutput
    {
        /// <summary>
        /// The CloudFormation template of the generated CDK deployment project.
        /// </summary>
        public string CloudFormationTemplate { get; set; }

        public GenerateCloudFormationTemplateOutput(string cloudFormationTemplate)
        {
            CloudFormationTemplate = cloudFormationTemplate;
        }
    }
}
