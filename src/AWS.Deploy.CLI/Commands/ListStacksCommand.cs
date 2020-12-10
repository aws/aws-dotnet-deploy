using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using AWS.Deploy.Orchestrator;
using AWS.DeploymentCommon;

namespace AWS.Deploy.CLI.Commands
{
    public class ListStacksCommand
    {
        private readonly IAWSClientFactory _awsClientFactory;
        private readonly IToolInteractiveService _interactiveService;
        private readonly OrchestratorSession _session;

        public ListStacksCommand(IAWSClientFactory awsClientFactory, IToolInteractiveService interactiveService, OrchestratorSession session)
        {
            _awsClientFactory = awsClientFactory;
            _interactiveService = interactiveService;
            _session = session;
        }

        public async Task ExecuteAsync()
        {
            using var cloudFormationClient = _awsClientFactory.GetAWSClient<IAmazonCloudFormation>(_session.AWSCredentials, _session.AWSRegion);
            var listStacksRequest = new ListStacksRequest
            {
                // Show only active Cloud Formation stacks, i.e. CREATE_FAILED & DELETE_COMPLETE statuses aren't included
                StackStatusFilter = new List<string>
                {
                    "CREATE_IN_PROGRESS",
                    "CREATE_COMPLETE",
                    "ROLLBACK_IN_PROGRESS",
                    "ROLLBACK_FAILED",
                    "ROLLBACK_COMPLETE",
                    "DELETE_IN_PROGRESS",
                    "DELETE_FAILED",
                    "UPDATE_IN_PROGRESS",
                    "UPDATE_COMPLETE_CLEANUP_IN_PROGRESS",
                    "UPDATE_COMPLETE",
                    "UPDATE_ROLLBACK_IN_PROGRESS",
                    "UPDATE_ROLLBACK_FAILED",
                    "UPDATE_ROLLBACK_COMPLETE_CLEANUP_IN_PROGRESS",
                    "UPDATE_ROLLBACK_COMPLETE",
                    "REVIEW_IN_PROGRESS"
                }
            };

            var listStacksPaginator = cloudFormationClient.Paginators.ListStacks(listStacksRequest);
            await foreach (var response in listStacksPaginator.Responses)
            {
                foreach (var stackSummary in response.StackSummaries)
                {
                    _interactiveService.WriteLine(stackSummary.StackName);
                }
            }
        }
    }
}
