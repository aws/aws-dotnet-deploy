// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using AWS.Deploy.CLI.CloudFormation;
using AWS.Deploy.CLI.Commands.Settings;
using AWS.Deploy.CLI.Utilities;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.LocalUserSettings;
using Spectre.Console.Cli;

namespace AWS.Deploy.CLI.Commands;

/// <summary>
/// Represents a Delete command allows to delete a CloudFormation stack
/// </summary>
public class DeleteDeploymentCommand : CancellableAsyncCommand<DeleteDeploymentCommandSettings>
{
    private static readonly TimeSpan s_pollingPeriod = TimeSpan.FromSeconds(5);

    private readonly IAWSClientFactory _awsClientFactory;
    private readonly IToolInteractiveService _interactiveService;
    private readonly IAmazonCloudFormation _cloudFormationClient;
    private readonly IConsoleUtilities _consoleUtilities;
    private readonly ILocalUserSettingsEngine _localUserSettingsEngine;
    private readonly IAWSUtilities _awsUtilities;
    private readonly IProjectParserUtility _projectParserUtility;
    private readonly IAWSResourceQueryer _awsResourceQueryer;
    private const int MAX_RETRIES = 4;

    /// <summary>
    /// Constructor for <see cref="DeleteDeploymentCommand"/>
    /// </summary>
    public DeleteDeploymentCommand(
        IAWSClientFactory awsClientFactory,
        IToolInteractiveService interactiveService,
        IConsoleUtilities consoleUtilities,
        ILocalUserSettingsEngine localUserSettingsEngine,
        IAWSUtilities awsUtilities,
        IProjectParserUtility projectParserUtility,
        IAWSResourceQueryer awsResourceQueryer)
    {
        _awsClientFactory = awsClientFactory;
        _interactiveService = interactiveService;
        _consoleUtilities = consoleUtilities;
        _cloudFormationClient = _awsClientFactory.GetAWSClient<IAmazonCloudFormation>();
        _localUserSettingsEngine = localUserSettingsEngine;
        _awsUtilities = awsUtilities;
        _projectParserUtility = projectParserUtility;
        _awsResourceQueryer = awsResourceQueryer;
    }

    /// <summary>
    /// Deletes given CloudFormation stack
    /// </summary>
    /// <param name="context">Command context</param>
    /// <param name="settings">Command settings</param>
    /// <param name="cancellationTokenSource">Cancellation token source</param>
    /// <exception cref="FailedToDeleteException">Thrown when deletion fails</exception>
    /// <returns>The command exit code</returns>
    public override async Task<int> ExecuteAsync(CommandContext context, DeleteDeploymentCommandSettings settings, CancellationTokenSource cancellationTokenSource)
    {
        _interactiveService.Diagnostics = settings.Diagnostics;
        _interactiveService.DisableInteractive = settings.Silent;

        var (awsCredentials, regionFromProfile) = await _awsUtilities.ResolveAWSCredentials(settings.Profile);
        var awsRegion = _awsUtilities.ResolveAWSRegion(settings.Region ?? regionFromProfile);

        _awsClientFactory.ConfigureAWSOptions(awsOption =>
        {
            awsOption.Credentials = awsCredentials;
            awsOption.Region = RegionEndpoint.GetBySystemName(awsRegion);
        });

        if (string.IsNullOrEmpty(settings.DeploymentName))
        {
            _interactiveService.WriteErrorLine(string.Empty);
            _interactiveService.WriteErrorLine("Deployment name cannot be empty. Please provide a valid deployment name and try again.");
            return CommandReturnCodes.USER_ERROR;
        }

        OrchestratorSession? session = null;

        try
        {
            var projectDefinition = await _projectParserUtility.Parse(settings.ProjectPath);

            var callerIdentity = await _awsResourceQueryer.GetCallerIdentity(awsRegion);

            session = new OrchestratorSession(
                projectDefinition,
                awsCredentials,
                awsRegion,
                callerIdentity.Account);
        }
        catch (FailedToFindDeployableTargetException) { }

        var canDelete = await CanDeleteAsync(settings.DeploymentName);
        if (!canDelete)
        {
            return CommandReturnCodes.SUCCESS;
        }

        var confirmDelete =  _interactiveService.DisableInteractive
            ? YesNo.Yes
            : _consoleUtilities.AskYesNoQuestion($"Are you sure you want to delete {settings.DeploymentName}?", YesNo.No);

        if (confirmDelete == YesNo.No)
        {
            return CommandReturnCodes.SUCCESS;
        }

        _interactiveService.WriteLine($"{settings.DeploymentName}: deleting...");
        var monitor = new StackEventMonitor(settings.DeploymentName, _awsClientFactory, _consoleUtilities, _interactiveService);

        try
        {
            await _cloudFormationClient.DeleteStackAsync(new DeleteStackRequest
            {
                StackName = settings.DeploymentName
            });

            // Fire and forget the monitor
            // Monitor updates the stdout with current status of the CloudFormation stack
            var _ = monitor.StartAsync();

            await WaitForStackDelete(settings.DeploymentName);

            if (session != null)
            {
                await _localUserSettingsEngine.DeleteLastDeployedStack(settings.DeploymentName, session.ProjectDefinition.ProjectName, session.AWSAccountId, session.AWSRegion);
            }

            _interactiveService.WriteLine($"{settings.DeploymentName}: deleted");
        }
        finally
        {
            // Stop monitoring CloudFormation stack status once the deletion operation finishes
            monitor.Stop();
        }

        return CommandReturnCodes.SUCCESS;
    }

    private async Task<bool> CanDeleteAsync(string stackName)
    {
        var stack = await GetStackAsync(stackName);
        if (stack == null)
        {
            _interactiveService.WriteErrorLine($"Stack with name {stackName} does not exist.");
            return false;
        }

        var canDelete = stack.Tags.Any(tag => tag.Key.Equals(Constants.CloudFormationIdentifier.STACK_TAG));
        if (!canDelete)
        {
            _interactiveService.WriteErrorLine("Only stacks that were deployed with this tool can be deleted.");
        }

        return canDelete;
    }

    private async Task WaitForStackDelete(string stackName)
    {
        var stack = await StabilizeStack(stackName);
        if (stack == null)
        {
            return;
        }

        if (stack.StackStatus.IsDeleted())
        {
            return;
        }

        if (stack.StackStatus.IsFailed())
        {
            throw new FailedToDeleteException(DeployToolErrorCode.FailedToDeleteStack, $"The stack {stackName} is in a failed state. You may need to delete it from the AWS Console.");
        }

        throw new FailedToDeleteException(DeployToolErrorCode.FailedToDeleteStack, $"Failed to delete {stackName} stack: {stack.StackStatus}");
    }

    private async Task<Stack?> StabilizeStack(string stackName)
    {
        Stack? stack;
        do
        {
            stack = await GetStackAsync(stackName);
            if (stack == null)
            {
                return null;
            }
            await Task.Delay(s_pollingPeriod);
        } while (stack.StackStatus.IsInProgress());

        return stack;
    }

    private async Task<Stack?> GetStackAsync(string stackName)
    {
        var retryCount = 0;
        bool shouldRetry;

        Stack? stack = null;
        do
        {
            var waitTime = GetWaitTime(retryCount);
            try
            {
                await Task.Delay(waitTime);

                var response = await _cloudFormationClient.DescribeStacksAsync(new DescribeStacksRequest
                {
                    StackName = stackName
                });

                stack = response.Stacks.Count == 0 ? null : response.Stacks[0];
                shouldRetry = false;
            }
            catch (AmazonCloudFormationException exception) when (exception.ErrorCode.Equals("ValidationError") && exception.Message.Equals($"Stack with id {stackName} does not exist"))
            {
                _interactiveService.WriteDebugLine(exception.PrettyPrint());
                shouldRetry = false;
            }
            catch (AmazonCloudFormationException exception) when (exception.ErrorCode.Equals("Throttling"))
            {
                _interactiveService.WriteDebugLine(exception.PrettyPrint());
                shouldRetry = true;
            }
        } while (shouldRetry && retryCount++ < MAX_RETRIES);

        return stack;
    }

    /// <summary>
    /// Returns the next wait interval, in milliseconds, using an exponential backoff algorithm
    /// Read more here https://docs.aws.amazon.com/general/latest/gr/api-retries.html
    /// </summary>
    /// <param name="retryCount"></param>
    /// <returns></returns>
    private static TimeSpan GetWaitTime(int retryCount) {
        if (retryCount == 0) {
            return TimeSpan.Zero;
        }

        var waitTime = Math.Pow(2, retryCount) * 5;
        return TimeSpan.FromSeconds(waitTime);
    }
}
