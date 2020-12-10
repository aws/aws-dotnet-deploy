using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using AWS.Deploy.Orchestrator;
using AWS.DeploymentCommon;

namespace AWS.Deploy.CLI.Commands
{
    public class DeleteStackCommand
    {
        private const string _inProgressSuffix = "IN_PROGRESS";
        private static readonly TimeSpan _pollingPeriod = TimeSpan.FromSeconds(3);

        private readonly IAWSClientFactory _awsClientFactory;
        private readonly IToolInteractiveService _interactiveService;
        private readonly OrchestratorSession _session;
        private readonly IAmazonCloudFormation _cloudFormationClient;
        private readonly ConsoleUtilities _consoleUtilities;

        public DeleteStackCommand(IAWSClientFactory awsClientFactory, IToolInteractiveService interactiveService, OrchestratorSession session)
        {
            _awsClientFactory = awsClientFactory;
            _interactiveService = interactiveService;
            _session = session;
            _cloudFormationClient = _awsClientFactory.GetAWSClient<IAmazonCloudFormation>(_session.AWSCredentials, _session.AWSRegion);
            _consoleUtilities = new ConsoleUtilities(interactiveService);
        }

        public async Task ExecuteAsync(string stackName)
        {
            try
            {
                await _cloudFormationClient.DeleteStackAsync(new DeleteStackRequest { StackName = stackName });

                await WaitStackToCompleteAsync(stackName, DateTime.Now);
            }
            catch (Exception e)
            {
                throw new Exception($"Error removing previous failed stack creation {stackName}: {e.Message}");
            }

            using var cloudFormationClient = _awsClientFactory.GetAWSClient<IAmazonCloudFormation>(_session.AWSCredentials, _session.AWSRegion);
            var deleteStackRequest = new DeleteStackRequest()
            {
                StackName = stackName
            };

            await cloudFormationClient.DeleteStackAsync(deleteStackRequest);
            await WaitStackToCompleteAsync(stackName, DateTime.Now);
        }

        private async Task WaitStackToCompleteAsync(string stackName, DateTime minTimeStampForEvents)
        {
            const int timestampWidth = 20;
            const int logicalResourceWidth = 40;
            const int resourceStatus = 40;
            string mostRecentEventId = "";

            // Write header for the status table.

            var header = new[]
            {
                ("Timestamp", timestampWidth),
                ("Logical Resource Id", logicalResourceWidth),
                ("Status", resourceStatus),
            };
            _consoleUtilities.DisplayRow(header);

            Stack stack;
            do
            {
                Thread.Sleep(_pollingPeriod);
                stack = await GetExistingStackAsync(stackName);

                var events = await GetLatestEventsAsync(stackName, minTimeStampForEvents, mostRecentEventId);
                if (events.Count > 0)
                    mostRecentEventId = events[0].EventId;

                for (int i = events.Count - 1; i >= 0; i--)
                {
                    var row = new[]
                    {
                        (events[i].Timestamp.ToString(CultureInfo.InvariantCulture), timestampWidth),
                        (events[i].LogicalResourceId, logicalResourceWidth),
                        (events[i].ResourceStatus.ToString(), resourceStatus),
                        (events[i].ResourceStatusReason, resourceStatus)
                    };
                    _consoleUtilities.DisplayRow(row);
                }
            } while (stack.StackStatus.ToString().EndsWith(_inProgressSuffix));
        }

        private async Task<Stack> GetExistingStackAsync(string stackName)
        {
            try
            {
                var response = await _cloudFormationClient.DescribeStacksAsync(new DescribeStacksRequest
                {
                    StackName = stackName
                });
                if (response.Stacks.Count != 1)
                    return null;

                return response.Stacks[0];
            }
            catch (AmazonCloudFormationException)
            {
                return null;
            }
        }

        private async Task<List<StackEvent>> GetLatestEventsAsync(string stackName, DateTime minTimeStampForEvents, string mostRecentEventId)
        {
            var noNewEvents = false;
            var events = new List<StackEvent>();
            DescribeStackEventsResponse response = null;
            do
            {
                var request = new DescribeStackEventsRequest()
                {
                    StackName = stackName
                };
                if (response != null)
                    request.NextToken = response.NextToken;

                try
                {
                    response = await _cloudFormationClient.DescribeStackEventsAsync(request);
                }
                catch (Exception e)
                {
                    throw new Exception($"Error getting events for stack: {e.Message}");
                }

                foreach (var stackEvent in response.StackEvents)
                {
                    if (string.Equals(stackEvent.EventId, mostRecentEventId) || stackEvent.Timestamp < minTimeStampForEvents)
                    {
                        noNewEvents = true;
                        break;
                    }

                    events.Add(stackEvent);
                }
            } while (!noNewEvents && !string.IsNullOrEmpty(response.NextToken));

            return events;
        }
    }
}
