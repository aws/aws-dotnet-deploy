// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using AWS.Deploy.CLI.Extensions;
using AWS.Deploy.Common;

namespace AWS.Deploy.CLI.CloudFormation
{
    /// <summary>
    /// Monitors a CloudFormation Stack event activities independently
    /// It uses stdout and displays the current status of the CloudFormation stack by polling periodically
    /// </summary>
    internal class StackEventMonitor
    {
        private const int TIMESTAMP_WIDTH = 18;
        private const int RESOURCE_STATUS_WIDTH = 20;
        private const int RESOURCE_TYPE_WIDTH = 40;
        private const int LOGICAL_RESOURCE_WIDTH = 40;
        private static readonly TimeSpan s_pollingPeriod = TimeSpan.FromSeconds(5);

        private readonly string _stackName;
        private bool _isActive;
        private DateTime _startTime;
        private readonly IAmazonCloudFormation _cloudFormationClient;
        private readonly HashSet<string> _processedEventIds = new HashSet<string>();
        private readonly IConsoleUtilities _consoleUtilities;
        private readonly IToolInteractiveService _interactiveService;

        public StackEventMonitor(string stackName, IAWSClientFactory awsClientFactory, IConsoleUtilities consoleUtilities, IToolInteractiveService interactiveService)
        {
            _stackName = stackName;
            _consoleUtilities = consoleUtilities;
            _interactiveService = interactiveService;

            _cloudFormationClient = awsClientFactory.GetAWSClient<IAmazonCloudFormation>();
        }

        /// <summary>
        /// Starts monitoring the CloudFormation Stack events since now
        /// </summary>
        public async Task StartAsync()
        {
            // CloudFormation API returns timestamps for events based on the system time
            _startTime = DateTime.Now;

            _isActive = true;

            await PollEventsAsync();
        }

        /// <summary>
        /// Stops monitoring the CloudFormation Stack events
        /// </summary>
        public void Stop()
        {
            _isActive = false;
        }

        private async Task PollEventsAsync()
        {
            while (_isActive)
            {
                await ReadNewEventsAsync();
                await Task.Delay(s_pollingPeriod);
            }
        }

        private async Task ReadNewEventsAsync()
        {
            var stackEvents = new List<StackEvent>();

            var describeStackEventsRequest = new DescribeStackEventsRequest { StackName = _stackName };
            var listStacksPaginator = _cloudFormationClient.Paginators.DescribeStackEvents(describeStackEventsRequest);

            try
            {
                var breakPaginator = false;
                await foreach (var response in listStacksPaginator.Responses)
                {
                    foreach (var stackEvent in response?.StackEvents ?? new List<StackEvent>())
                    {
                        // Event from before we are interested in
                        if (stackEvent.Timestamp < _startTime)
                        {
                            breakPaginator = true;
                            break;
                        }

                        // Already processed event
                        if (_processedEventIds.Contains(stackEvent.EventId))
                        {
                            breakPaginator = true;
                            break;
                        }

                        // New event, save it
                        _processedEventIds.Add(stackEvent.EventId);
                        stackEvents.Add(stackEvent);
                    }

                    if (breakPaginator)
                    {
                        break;
                    }
                }
            }
            catch (AmazonCloudFormationException exception) when (exception.ErrorCode.Equals("ValidationError") && exception.Message.Equals($"Stack [{_stackName}] does not exist"))
            {
                // Stack is deleted, there could be some missed events between the last poll timestamp and DELETE_COMPLETE
                _interactiveService.WriteDebugLine(exception.PrettyPrint());
            }
            catch (AmazonCloudFormationException exception)
            {
                // Other AmazonCloudFormationException
                _interactiveService.WriteDebugLine(exception.PrettyPrint());
            }

            foreach (var stackEvent in stackEvents.OrderBy(e => e.Timestamp))
            {
                var row = new[]
                {
                    (stackEvent.Timestamp.ToString(CultureInfo.InvariantCulture), TIMESTAMP_WIDTH),
                    (stackEvent.ResourceStatus.ToString(), RESOURCE_STATUS_WIDTH),
                    (stackEvent.ResourceType.Truncate(RESOURCE_TYPE_WIDTH, true), RESOURCE_TYPE_WIDTH),
                    (stackEvent.LogicalResourceId, LOGICAL_RESOURCE_WIDTH),
                };
                _consoleUtilities.DisplayRow(row);
            }
        }
    }
}
