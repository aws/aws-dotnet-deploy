// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel;
using Spectre.Console.Cli;

namespace AWS.Deploy.CLI.Commands.Settings;

/// <summary>
/// Represents the settings for configuring the <see cref="RootCommand"/>.
/// </summary>
public sealed class RootCommandSettings : CommandSettings
{
    /// <summary>
    /// Show help and usage information
    /// </summary>
    [CommandOption("-v|--version")]
    [Description("Show help and usage information")]
    public bool Version { get; set; }
}
