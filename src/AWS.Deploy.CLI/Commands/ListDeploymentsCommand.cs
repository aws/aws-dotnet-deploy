// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Threading.Tasks;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Orchestration.Utilities;

namespace AWS.Deploy.CLI.Commands
{
    public class ListDeploymentsCommand
    {
        private readonly IToolInteractiveService _interactiveService;
        private readonly IDeployedApplicationQueryer _deployedApplicationQueryer;
        
        public ListDeploymentsCommand(
            IToolInteractiveService interactiveService,
            IDeployedApplicationQueryer deployedApplicationQueryer)
        {
            _interactiveService = interactiveService;
            _deployedApplicationQueryer = deployedApplicationQueryer;
        }

        public async Task ExecuteAsync()
        {
            // Add Header
            _interactiveService.WriteLine();
            _interactiveService.WriteLine("Cloud Applications:");
            _interactiveService.WriteLine("-------------------");

            var deploymentTypes = new List<DeploymentTypes>(){ DeploymentTypes.CdkProject, DeploymentTypes.BeanstalkEnvironment };
            var existingApplications = await _deployedApplicationQueryer.GetExistingDeployedApplications(deploymentTypes);
            foreach (var app in existingApplications)
            {
                _interactiveService.WriteLine(app.DisplayName);
            }
        }
    }
}
