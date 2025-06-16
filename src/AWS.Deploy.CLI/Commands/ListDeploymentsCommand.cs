// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using AWS.Deploy.CLI.Commands.Settings;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Orchestration.Utilities;
using Spectre.Console.Cli;

namespace AWS.Deploy.CLI.Commands;

/// <summary>
/// Represents a List command that allows listing deployed applications
/// </summary>
public class ListDeploymentsCommand(
    IToolInteractiveService toolInteractiveService,
    IDeployedApplicationQueryer deployedApplicationQueryer,
    IAWSUtilities awsUtilities,
    IAWSClientFactory awsClientFactory,
    IAWSResourceQueryer awsResourceQueryer) : CancellableAsyncCommand<ListDeploymentsCommandSettings>
{
    /// <summary>
    /// List deployed applications
    /// </summary>
    /// <param name="context">Command context</param>
    /// <param name="settings">Command settings</param>
    /// <param name="cancellationTokenSource">Cancellation token source</param>
    /// <returns>The command exit code</returns>
    public override async Task<int> ExecuteAsync(CommandContext context, ListDeploymentsCommandSettings settings, CancellationTokenSource cancellationTokenSource)
    {
        toolInteractiveService.Diagnostics = settings.Diagnostics;

        var (awsCredentials, regionFromProfile) = await awsUtilities.ResolveAWSCredentials(settings.Profile);
        var awsRegion = awsUtilities.ResolveAWSRegion(settings.Region ?? regionFromProfile);

        awsClientFactory.ConfigureAWSOptions(awsOptions =>
        {
            awsOptions.Credentials = awsCredentials;
            awsOptions.Region = RegionEndpoint.GetBySystemName(awsRegion);
        });

        await awsResourceQueryer.GetCallerIdentity(awsRegion);

        // Add Header
        toolInteractiveService.WriteLine();
        toolInteractiveService.WriteLine("Cloud Applications:");
        toolInteractiveService.WriteLine("-------------------");

        var deploymentTypes = new List<DeploymentTypes>(){ DeploymentTypes.CdkProject, DeploymentTypes.BeanstalkEnvironment };
        var existingApplications = await deployedApplicationQueryer.GetExistingDeployedApplications(deploymentTypes);
        foreach (var app in existingApplications)
        {
            toolInteractiveService.WriteLine(app.DisplayName);
        }

        return CommandReturnCodes.SUCCESS;
    }
}
