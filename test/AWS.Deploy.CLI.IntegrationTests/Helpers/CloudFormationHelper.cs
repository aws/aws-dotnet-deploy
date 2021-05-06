// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Xunit;

namespace AWS.Deploy.CLI.IntegrationTests.Helpers
{
    public class CloudFormationHelper
    {
        private readonly IAmazonCloudFormation _cloudFormationClient;

        public CloudFormationHelper(IAmazonCloudFormation cloudFormationClient)
        {
            _cloudFormationClient = cloudFormationClient;
        }

        public async Task<StackStatus> GetStackStatus(string stackName)
        {
            var stack = await GetStackAsync(stackName);
            return stack.StackStatus;
        }

        public async Task<bool> IsStackDeleted(string stackName)
        {
            try
            {
                await GetStackAsync(stackName);
            }
            catch (AmazonCloudFormationException cloudFormationException) when (cloudFormationException.Message.Equals($"Stack with id {stackName} does not exist"))
            {
                return true;
            }

            return false;
        }

        public async Task DeleteStack(string stackName)
        {
            try
            {
                var request = new DeleteStackRequest()
                {
                    StackName = stackName
                };

                await _cloudFormationClient.DeleteStackAsync(request);
            }
            catch (AmazonCloudFormationException)
            {
                // Don't throw an error if the stack does not exist. Most likely a test has failed before a stack was actually created. If we
                // throw the exception here it will hide the original error.
            }
        }

        private async Task<Stack> GetStackAsync(string stackName)
        {
            var response = await _cloudFormationClient.DescribeStacksAsync(new DescribeStacksRequest
            {
                StackName = stackName
            });

            return response.Stacks.Count == 0 ? null : response.Stacks[0];
        }


    }
}
