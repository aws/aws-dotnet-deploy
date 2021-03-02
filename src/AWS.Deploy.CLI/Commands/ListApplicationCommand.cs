// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;
using AWS.Deploy.Orchestrator;
using AWS.Deploy.Orchestrator.Data;
using AWS.Deploy.Recipes;

namespace AWS.Deploy.CLI.Commands
{
    public class ListApplicationCommand
    {
        private readonly IOrchestratorInteractiveService _orchestratorInteractiveService;
        private readonly IToolInteractiveService _interactiveService;
        private readonly OrchestratorSession _session;
        private readonly ICdkProjectHandler _cdkProjectHandler;
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IDeploymentBundleHandler _deploymentBundleHandler;

        public ListApplicationCommand(IToolInteractiveService interactiveService,
            IOrchestratorInteractiveService orchestratorInteractiveService,
            ICdkProjectHandler cdkProjectHandler,
            IDeploymentBundleHandler deploymentBundleHandler,
            IAWSResourceQueryer awsResourceQueryer,
            OrchestratorSession session)
        {
            _interactiveService = interactiveService;
            _orchestratorInteractiveService = orchestratorInteractiveService;
            _cdkProjectHandler = cdkProjectHandler;
            _deploymentBundleHandler = deploymentBundleHandler;
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
                    _deploymentBundleHandler,
                    new[] { RecipeLocator.FindRecipeDefinitionsPath() });

            // Add Header
            _interactiveService.WriteLine();
            _interactiveService.WriteLine("Cloud Applications:");
            _interactiveService.WriteLine("-------------------");

            var existingApplications = await orchestrator.GetExistingDeployedApplications();
            foreach (var app in existingApplications)
            {
                _interactiveService.WriteLine(app.Name);
            }
        }
    }
}
