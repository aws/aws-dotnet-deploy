// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using AWS.Deploy.CLI.CloudFormation;
using AWS.Deploy.Common;
using AWS.Deploy.Recipes.CDK.Common;

namespace AWS.Deploy.CLI.Commands
{
    /// <summary>
    /// Represents a Delete command allows to delete a CloudFormation stack
    /// </summary>
    public class DeleteDeploymentCommand
    {
        private static readonly TimeSpan s_pollingPeriod = TimeSpan.FromSeconds(1);

        private readonly IAWSClientFactory _awsClientFactory;
        private readonly IToolInteractiveService _interactiveService;
        private readonly IAmazonCloudFormation _cloudFormationClient;
        private readonly IConsoleUtilities _consoleUtilities;

        public DeleteDeploymentCommand(IAWSClientFactory awsClientFactory, IToolInteractiveService interactiveService, IConsoleUtilities consoleUtilities)
        {
            _awsClientFactory = awsClientFactory;
            _interactiveService = interactiveService;
            _consoleUtilities = consoleUtilities;
            _cloudFormationClient = _awsClientFactory.GetAWSClient<IAmazonCloudFormation>();
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

            var confirmDelete = _consoleUtilities.AskYesNoQuestion($"Are you sure you want to delete {stackName}?", YesNo.No);
            if (confirmDelete == YesNo.No)
            {
                return;
            }

            _interactiveService.WriteLine($"{stackName}: deleting...");
            var monitor = new StackEventMonitor(stackName, _awsClientFactory, _consoleUtilities);

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
                _interactiveService.WriteLine($"{stackName}: deleted");
            }
            catch (AmazonCloudFormationException)
            {
                throw new FailedToDeleteException($"Failed to delete {stackName} stack.");
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

            var canDelete = stack.Tags.Any(tag => tag.Key.Equals(CloudFormationIdentifierConstants.STACK_TAG));
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
                throw new FailedToDeleteException($"The stack {stackName} is in a failed state. You may need to delete it from the AWS Console.");
            }

            throw new FailedToDeleteException($"Failed to delete {stackName} stack: {stack.StackStatus}");
        }

        private async Task<Stack> StabilizeStack(string stackName)
        {
            Stack stack;
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

        private async Task<Stack> GetStackAsync(string stackName)
        {
            try
            {
                var response = await _cloudFormationClient.DescribeStacksAsync(new DescribeStacksRequest
                {
                    StackName = stackName
                });

                return response.Stacks.Count == 0 ? null : response.Stacks[0];
            }
            catch (AmazonCloudFormationException exception) when (exception.ErrorCode.Equals("ValidationError") && exception.Message.Equals($"Stack with id {stackName} does not exist"))
            {
                return null;
            }
        }
    }
}
