using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using AWS.Deploy.Common;
using AWS.Deploy.Orchestrator;
using AWS.Deploy.Orchestrator.Data;
using AWS.Deploy.Recipes;

namespace AWS.Deploy.CLI.Commands
{
    public class ListStacksCommand
    {
        private readonly IAWSClientFactory _awsClientFactory;
        private readonly IOrchestratorInteractiveService _orchestratorInteractiveService;
        private readonly IToolInteractiveService _interactiveService;
        private readonly OrchestratorSession _session;
        private readonly ICdkProjectHandler _cdkProjectHandler;
        private readonly IAWSResourceQueryer _awsResourceQueryer;

        public ListStacksCommand(IAWSClientFactory awsClientFactory,
            IToolInteractiveService interactiveService,
            IOrchestratorInteractiveService orchestratorInteractiveService,
            ICdkProjectHandler cdkProjectHandler, IAWSResourceQueryer awsResourceQueryer, OrchestratorSession session)
        {
            _awsClientFactory = awsClientFactory;
            _interactiveService = interactiveService;
            _orchestratorInteractiveService = orchestratorInteractiveService;
            _cdkProjectHandler = cdkProjectHandler;
            _awsResourceQueryer = awsResourceQueryer;
            _session = session;
        }

        public async Task ExecuteAsync()
        {
            var orchestrator =
                new Orchestrator.Orchestrator(
                    _session,
                    _orchestratorInteractiveService,
                    _cdkProjectHandler,
                    _awsResourceQueryer,
                    new[] { RecipeLocator.FindRecipeDefinitionsPath() });


            var existingApplications = await orchestrator.GetExistingDeployedApplications();
            foreach (var app in existingApplications)
            {
                _interactiveService.WriteLine(app.Name);
            }
        }
    }
}
