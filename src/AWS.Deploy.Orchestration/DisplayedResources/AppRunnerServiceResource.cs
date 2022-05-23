// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Threading.Tasks;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Orchestration.Data;

namespace AWS.Deploy.Orchestration.DisplayedResources
{
    public class AppRunnerServiceResource : IDisplayedResourceCommand
    {
        private readonly IAWSResourceQueryer _awsResourceQueryer;

        public AppRunnerServiceResource(IAWSResourceQueryer awsResourceQueryer)
        {
            _awsResourceQueryer = awsResourceQueryer;
        }

        public async Task<Dictionary<string, string>> Execute(string resourceId)
        {
            var service = await _awsResourceQueryer.DescribeAppRunnerService(resourceId);

            return new Dictionary<string, string>() {
                { "Endpoint", $"https://{service.ServiceUrl}/" }
            };
        }
    }
}
