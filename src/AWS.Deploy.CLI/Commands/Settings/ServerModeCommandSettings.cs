// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel;
using Spectre.Console.Cli;

namespace AWS.Deploy.CLI.Commands.Settings;

/// <summary>
/// Represents the settings for configuring the <see cref="ServerModeCommand"/>.
/// </summary>
public class ServerModeCommandSettings : CommandSettings
{
    /// <summary>
    /// Port the server mode will listen to
    /// </summary>
    [CommandOption("--port")]
    [Description("Port the server mode will listen to.")]
    public required int Port { get; set; }

    /// <summary>
    /// The ID of the process that is launching server mode
    /// </summary>
    [CommandOption("--parent-pid")]
    [Description("The ID of the process that is launching server mode. Server mode will exit when the parent pid terminates.")]
    public int? ParentPid { get; set; }

    /// <summary>
    /// If set the cli uses an unsecure mode without encryption
    /// </summary>
    [CommandOption("--unsecure-mode")]
    [Description("If set the cli uses an unsecure mode without encryption.")]
    public bool UnsecureMode { get; set; }

    /// <summary>
    /// Enable diagnostic output
    /// </summary>
    [CommandOption("-d|--diagnostics")]
    [Description("Enable diagnostic output.")]
    public bool Diagnostics { get; set; }
}
