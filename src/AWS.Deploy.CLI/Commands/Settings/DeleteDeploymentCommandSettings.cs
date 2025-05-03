// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel;
using System.IO;
using Spectre.Console.Cli;

namespace AWS.Deploy.CLI.Commands.Settings;

/// <summary>
/// Represents the settings for configuring the <see cref="DeleteDeploymentCommand"/>.
/// </summary>
public class DeleteDeploymentCommandSettings : CommandSettings
{
    /// <summary>
    /// AWS credential profile used to make calls to AWS
    /// </summary>
    [CommandOption("--profile")]
    [Description("AWS credential profile used to make calls to AWS.")]
    public string? Profile { get; set; }

    /// <summary>
    /// AWS region to deploy the application to
    /// </summary>
    [CommandOption("--region")]
    [Description("AWS region to deploy the application to. For example, us-west-2.")]
    public string? Region { get; set; }

    /// <summary>
    /// Path to the project to deploy
    /// </summary>
    [CommandOption("--project-path")]
    [Description("Path to the project to deploy.")]
    public string ProjectPath { get; set; } = Directory.GetCurrentDirectory();

    /// <summary>
    /// Enable diagnostic output
    /// </summary>
    [CommandOption("-d|--diagnostics")]
    [Description("Enable diagnostic output.")]
    public bool Diagnostics { get; set; }

    /// <summary>
    /// Disable interactivity to execute commands without any prompts for user input
    /// </summary>
    [CommandOption("-s|--silent")]
    [Description("Disable interactivity to execute commands without any prompts for user input.")]
    public bool Silent { get; set; }

    /// <summary>
    /// The name of the deployment to be deleted
    /// </summary>
    [CommandArgument(0, "<deployment-name>")]
    [Description("The name of the deployment to be deleted.")]
    public required string DeploymentName { get; set; }
}
