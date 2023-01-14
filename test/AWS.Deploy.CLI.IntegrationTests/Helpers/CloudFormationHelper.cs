// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;

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

        public async Task<StackStatus> GetStackArn(string stackName)
        {
            var stack = await GetStackAsync(stackName);
            return stack.StackId;
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
            var request = new DeleteStackRequest()
            {
                StackName = stackName
            };

            await _cloudFormationClient.DeleteStackAsync(request);
        }

        public async Task<string> GetResourceId(string stackName, string logicalId)
        {
            var request = new DescribeStackResourceRequest
            {
                StackName = stackName,
                LogicalResourceId = logicalId
            };

            var response = await _cloudFormationClient.DescribeStackResourceAsync(request);
            return response.StackResourceDetail.PhysicalResourceId;
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
