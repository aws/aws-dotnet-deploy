using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using AWS.Deploy.Common;
using AWS.Deploy.Orchestrator;

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
            var describeStacksRequest = new DescribeStacksRequest();

            var listStacksPaginator = cloudFormationClient.Paginators.DescribeStacks(describeStacksRequest);
            await foreach (var response in listStacksPaginator.Responses)
            {
                var stacks = response.Stacks.Where(stack => stack.Tags.Any(tag => tag.Key.Equals(CloudApplication.StackTagKey)));
                foreach (var stack in stacks)
                {
                    _interactiveService.WriteLine(stack.StackName);
                }
            }
        }
    }
}
