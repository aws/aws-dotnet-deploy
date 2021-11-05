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
        private readonly Recommendation _selectedRecommendation;

        public DeployRecommendationTask(Orchestrator orchestrator, CloudApplication cloudApplication, Recommendation selectedRecommendation)
        {
            _orchestrator = orchestrator;
            _cloudApplication = cloudApplication;
            _selectedRecommendation = selectedRecommendation;
        }

        public async Task Execute()
        {
            await CreateDeploymentBundle();
            await _orchestrator.DeployRecommendation(_cloudApplication, _selectedRecommendation);
        }

        private async Task CreateDeploymentBundle()
        {
            if (_selectedRecommendation.Recipe.DeploymentBundle == DeploymentBundleTypes.Container)
            {
                var dockerBuildDeploymentBundleResult = await _orchestrator.CreateContainerDeploymentBundle(_cloudApplication, _selectedRecommendation);
                if (!dockerBuildDeploymentBundleResult)
                    throw new FailedToCreateDeploymentBundleException(DeployToolErrorCode.FailedToCreateContainerDeploymentBundle, "Failed to create a deployment bundle");
            }
            else if (_selectedRecommendation.Recipe.DeploymentBundle == DeploymentBundleTypes.DotnetPublishZipFile)
            {
                var dotnetPublishDeploymentBundleResult = await _orchestrator.CreateDotnetPublishDeploymentBundle(_selectedRecommendation);
                if (!dotnetPublishDeploymentBundleResult)
                    throw new FailedToCreateDeploymentBundleException(DeployToolErrorCode.FailedToCreateDotnetPublishDeploymentBundle, "Failed to create a deployment bundle");
            }
        }
    }
}
