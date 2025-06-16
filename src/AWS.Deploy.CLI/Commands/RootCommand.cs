// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using AWS.Deploy.CLI.Commands.Settings;
using AWS.Deploy.CLI.Utilities;
using Spectre.Console.Cli;

namespace AWS.Deploy.CLI.Commands;

/// <summary>
/// Represents the root command which displays information about the tool
/// </summary>
public class RootCommand(
    IToolInteractiveService toolInteractiveService) : Command<RootCommandSettings>
{
    /// <summary>
    /// Displays information about the tool
    /// </summary>
    /// <param name="context">Command context</param>
    /// <param name="settings">Command settings</param>
    /// <returns>The command exit code</returns>
    public override int Execute(CommandContext context, RootCommandSettings settings)
    {
        if (settings.Version)
        {
            var toolVersion = CommandLineHelpers.GetToolVersion();
            toolInteractiveService.WriteLine($"Version: {toolVersion}");
        }

        return CommandReturnCodes.SUCCESS;
    }
}
