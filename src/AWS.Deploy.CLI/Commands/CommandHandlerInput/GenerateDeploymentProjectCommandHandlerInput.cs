// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.CLI.Commands.CommandHandlerInput
{
    /// <summary>
    /// This class maps the command line options for the "deployment-project generate" command to the appropriate C# properties.
    /// </summary>
    public class GenerateDeploymentProjectCommandHandlerInput : CommandHandlerInput
    {
        public string? ProjectPath { get; set; }
        public string Output { get; set; } = string.Empty;
        public string ProjectDisplayName { get; set; } = string.Empty;
    }
}
