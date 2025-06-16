// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel;
using System.IO;
using Spectre.Console.Cli;

namespace AWS.Deploy.CLI.Commands.Settings;

/// <summary>
/// Represents the settings for configuring the <see cref="GenerateDeploymentProjectCommand"/>.
/// </summary>
public class GenerateDeploymentProjectCommandSettings : CommandSettings
{
    /// <summary>
    /// Directory path in which the CDK deployment project will be saved
    /// </summary>
    [CommandOption("-o|--output")]
    [Description("Directory path in which the CDK deployment project will be saved.")]
    public string? Output { get; set; }

    /// <summary>
    /// Enable diagnostic output
    /// </summary>
    [CommandOption("-d|--diagnostics")]
    [Description("Enable diagnostic output.")]
    public bool Diagnostics { get; set; }

    /// <summary>
    /// Path to the project to deploy
    /// </summary>
    [CommandOption("--project-path")]
    [Description("Path to the project to deploy.")]
    public string ProjectPath { get; set; } = Directory.GetCurrentDirectory();

    /// <summary>
    /// The name of the deployment project that will be displayed in the list of available deployment options
    /// </summary>
    [CommandOption("--project-display-name")]
    [Description("The name of the deployment project that will be displayed in the list of available deployment options.")]
    public string? ProjectDisplayName { get; set; }
}
