// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Orchestration;

namespace AWS.Deploy.CLI.ServerMode.Tasks
{
    public class DeployRecommendationTask
    {
        private readonly CloudApplication _cloudApplication;
        private readonly Orchestrator _orchestrator;
        private readonly OrchestratorSession _orchestratorSession;
        private readonly Recommendation _selectedRecommendation;

        public DeployRecommendationTask(OrchestratorSession orchestratorSession, Orchestrator orchestrator, CloudApplication cloudApplication, Recommendation selectedRecommendation)
        {
            _orchestratorSession = orchestratorSession;
            _orchestrator = orchestrator;
            _cloudApplication = cloudApplication;
            _selectedRecommendation = selectedRecommendation;
        }

        public async Task Execute()
        {
            await _orchestrator.CreateDeploymentBundle(_cloudApplication, _selectedRecommendation);
            await _orchestrator.DeployRecommendation(_cloudApplication, _selectedRecommendation);
        }

        /// <summary>
        /// Generates the CloudFormation template that will be used by CDK for the deployment.
        /// This involves creating a deployment bundle, generating the CDK project and running 'cdk diff' to get the CF template.
        /// This operation returns the CloudFormation template that is created for this deployment.
        /// </summary>
        public async Task<string> GenerateCloudFormationTemplate(CdkProjectHandler cdkProjectHandler)
        {
            if (cdkProjectHandler == null)
                throw new FailedToCreateCDKProjectException(DeployToolErrorCode.FailedToCreateCDKProject, $"We could not create a CDK deployment project due to a missing dependency '{nameof(cdkProjectHandler)}'.");

            await _orchestrator.CreateDeploymentBundle(_cloudApplication, _selectedRecommendation);

            var cdkProject = await cdkProjectHandler.ConfigureCdkProject(_orchestratorSession, _cloudApplication, _selectedRecommendation);
            try
            {
                return await cdkProjectHandler.PerformCdkDiff(cdkProject, _cloudApplication);
            }
            finally
            {
                cdkProjectHandler.DeleteTemporaryCdkProject(cdkProject);
            }
        }
    }
}
