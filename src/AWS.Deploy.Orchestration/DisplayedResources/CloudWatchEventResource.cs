// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Threading.Tasks;
using AWS.Deploy.Orchestration.Data;

namespace AWS.Deploy.Orchestration.DisplayedResources
{
    public class CloudWatchEventResource : IDisplayedResourceCommand
    {
        private const string DATA_TITLE_EVENT_SCHEDULE = "Event Schedule";
        private readonly IAWSResourceQueryer _awsResourceQueryer;

        public CloudWatchEventResource(IAWSResourceQueryer awsResourceQueryer)
        {
            _awsResourceQueryer = awsResourceQueryer;
        }

        public async Task<Dictionary<string, string>> Execute(string resourceId)
        {
            var rule = await _awsResourceQueryer.DescribeCloudWatchRule(resourceId);

            return new Dictionary<string, string>() {
                { DATA_TITLE_EVENT_SCHEDULE, rule.ScheduleExpression }
            };
        }
    }
}
