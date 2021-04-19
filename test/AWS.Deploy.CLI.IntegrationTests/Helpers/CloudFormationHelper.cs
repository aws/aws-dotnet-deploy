// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
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
            if (stack == null)
            {
                throw new AmazonCloudFormationException($"There does not exists a stack with {stackName} name.");
            }

            return stack.StackStatus;
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
