// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Threading.Tasks;
using AWS.Deploy.Orchestration.Data;

namespace AWS.Deploy.Orchestration.DisplayedResources
{
    public class ElasticBeanstalkEnvironmentResource : IDisplayedResourceCommand
    {
        private readonly IAWSResourceQueryer _awsResourceQueryer;

        public ElasticBeanstalkEnvironmentResource(IAWSResourceQueryer awsResourceQueryer)
        {
            _awsResourceQueryer = awsResourceQueryer;
        }

        public async Task<Dictionary<string, string>> Execute(string resourceId)
        {
            var environment = await _awsResourceQueryer.DescribeElasticBeanstalkEnvironment(resourceId);
            return new Dictionary<string, string>() {
                { "Endpoint", $"http://{environment.CNAME}/" }
            };
        }
    }
}
