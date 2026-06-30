// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Threading.Tasks;
using AWS.Deploy.Common.Data;

namespace AWS.Deploy.Orchestration.DisplayedResources
{
    public class BedrockAgentCoreRuntimeResource : IDisplayedResourceCommand
    {
        private readonly IAWSResourceQueryer _awsResourceQueryer;

        public BedrockAgentCoreRuntimeResource(IAWSResourceQueryer awsResourceQueryer)
        {
            _awsResourceQueryer = awsResourceQueryer;
        }

        public async Task<Dictionary<string, string>> Execute(string resourceId)
        {
            var runtime = await _awsResourceQueryer.DescribeBedrockAgentCoreRuntime(resourceId);

            return new Dictionary<string, string>
            {
                { "ARN", runtime.AgentRuntimeArn }
            };
        }
    }
}
