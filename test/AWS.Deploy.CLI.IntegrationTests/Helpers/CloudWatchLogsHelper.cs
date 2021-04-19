// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;

namespace AWS.Deploy.CLI.IntegrationTests.Helpers
{
    public class CloudWatchLogsHelper
    {
        private readonly IAmazonCloudWatchLogs _client;

        public CloudWatchLogsHelper(IAmazonCloudWatchLogs client)
        {
            _client = client;
        }

        public async Task<IEnumerable<string>> GetLogMessages(string logGroup)
        {
            var logStream = await GetLatestLogStream(logGroup);
            return await GetLogMessages(logGroup, logStream);
        }

        private async Task<IEnumerable<string>> GetLogMessages(string logGroup, string logStreamName)
        {
            var request = new GetLogEventsRequest
            {
                LogGroupName = logGroup,
                LogStreamName = logStreamName
            };
            ;
            var logMessages = new List<string>();

            var listStacksPaginator = _client.Paginators.GetLogEvents(request);
            await foreach (var response in listStacksPaginator.Responses)
            {
                if (response.Events.Count == 0)
                {
                    break;
                }

                var messages = response.Events.Select(e => e.Message);
                logMessages.AddRange(messages);
            }

            return logMessages;
        }

        private async Task<string> GetLatestLogStream(string logGroup)
        {
            var request = new DescribeLogStreamsRequest
            {
                LogGroupName = logGroup
            };

            var response = await _client.DescribeLogStreamsAsync(request);
            return response.LogStreams.FirstOrDefault()?.LogStreamName;
        }
    }
}
