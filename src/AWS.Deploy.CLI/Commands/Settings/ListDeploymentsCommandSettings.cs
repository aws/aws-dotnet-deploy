// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel;
using Spectre.Console.Cli;

namespace AWS.Deploy.CLI.Commands.Settings;

/// <summary>
/// Represents the settings for configuring the <see cref="ListDeploymentsCommand"/>.
/// </summary>
public class ListDeploymentsCommandSettings : CommandSettings
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
    /// Enable diagnostic output
    /// </summary>
    [CommandOption("-d|--diagnostics")]
    [Description("Enable diagnostic output.")]
    public bool Diagnostics { get; set; }
}
