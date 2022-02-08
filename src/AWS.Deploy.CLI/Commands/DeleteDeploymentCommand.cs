// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using AWS.Deploy.CLI.CloudFormation;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.LocalUserSettings;

namespace AWS.Deploy.CLI.Commands
{
    /// <summary>
    /// Represents a Delete command allows to delete a CloudFormation stack
    /// </summary>
    public class DeleteDeploymentCommand
    {
        private static readonly TimeSpan s_pollingPeriod = TimeSpan.FromSeconds(5);

        private readonly IAWSClientFactory _awsClientFactory;
        private readonly IToolInteractiveService _interactiveService;
        private readonly IAmazonCloudFormation _cloudFormationClient;
        private readonly IConsoleUtilities _consoleUtilities;
        private readonly ILocalUserSettingsEngine _localUserSettingsEngine;
        private readonly OrchestratorSession? _session;
        private const int MAX_RETRIES = 4;

        public DeleteDeploymentCommand(
            IAWSClientFactory awsClientFactory,
            IToolInteractiveService interactiveService,
            IConsoleUtilities consoleUtilities,
            ILocalUserSettingsEngine localUserSettingsEngine,
            OrchestratorSession? session)
        {
            _awsClientFactory = awsClientFactory;
            _interactiveService = interactiveService;
            _consoleUtilities = consoleUtilities;
            _cloudFormationClient = _awsClientFactory.GetAWSClient<IAmazonCloudFormation>();
            _localUserSettingsEngine = localUserSettingsEngine;
            _session = session;
        }

        /// <summary>
        /// Deletes given CloudFormation stack
        /// </summary>
        /// <param name="stackName">The stack name to be deleted</param>
        /// <exception cref="FailedToDeleteException">Thrown when deletion fails</exception>
        public async Task ExecuteAsync(string stackName)
        {
            var canDelete = await CanDeleteAsync(stackName);
            if (!canDelete)
            {
                return;
            }

            var confirmDelete =  _interactiveService.DisableInteractive
                ? YesNo.Yes
                : _consoleUtilities.AskYesNoQuestion($"Are you sure you want to delete {stackName}?", YesNo.No);

            if (confirmDelete == YesNo.No)
            {
                return;
            }

            _interactiveService.WriteLine($"{stackName}: deleting...");
            var monitor = new StackEventMonitor(stackName, _awsClientFactory, _consoleUtilities, _interactiveService);

            try
            {
                await _cloudFormationClient.DeleteStackAsync(new DeleteStackRequest
                {
                    StackName = stackName
                });

                // Fire and forget the monitor
                // Monitor updates the stdout with current status of the CloudFormation stack
                var _ = monitor.StartAsync();

                await WaitForStackDelete(stackName);

                if (_session != null)
                {
                    await _localUserSettingsEngine.DeleteLastDeployedStack(stackName, _session.ProjectDefinition.ProjectName, _session.AWSAccountId, _session.AWSRegion);
                }

                _interactiveService.WriteLine($"{stackName}: deleted");
            }
            finally
            {
                // Stop monitoring CloudFormation stack status once the deletion operation finishes
                monitor.Stop();
            }
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
            var shouldRetry = false;

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
}
