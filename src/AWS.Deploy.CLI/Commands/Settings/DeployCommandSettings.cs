// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel;
using System.IO;
using Spectre.Console.Cli;

namespace AWS.Deploy.CLI.Commands.Settings;

/// <summary>
/// Represents the settings for configuring the <see cref="DeployCommand"/>.
/// </summary>
public class DeployCommandSettings : CommandSettings
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
    /// Name of the cloud application
    /// </summary>
    [CommandOption("--application-name")]
    [Description("Name of the cloud application. If you choose to deploy via CloudFormation, this name will be used to identify the CloudFormation stack.")]
    public string? ApplicationName { get; set; }

    /// <summary>
    /// Path to the deployment settings file to be applied
    /// </summary>
    [CommandOption("--apply")]
    [Description("Path to the deployment settings file to be applied.")]
    public string? Apply { get; set; }

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
    /// The absolute or relative path of the CDK project that will be used for deployment
    /// </summary>
    [CommandOption("--deployment-project")]
    [Description("The absolute or relative path of the CDK project that will be used for deployment.")]
    public string? DeploymentProject { get; set; }

    /// <summary>
    /// The absolute or the relative JSON file path where the deployment settings will be saved.
    /// Only the settings modified by the user will be persisted.
    /// </summary>
    [CommandOption("--save-settings")]
    [Description("The absolute or the relative JSON file path where the deployment settings will be saved. Only the settings modified by the user will be persisted.")]
    public string? SaveSettings { get; set; }

    /// <summary>
    /// The absolute or the relative JSON file path where the deployment settings will be saved.
    /// All deployment settings will be persisted.
    /// </summary>
    [CommandOption("--save-all-settings")]
    [Description("The absolute or the relative JSON file path where the deployment settings will be saved. All deployment settings will be persisted.")]
    public string? SaveAllSettings { get; set; }
}
