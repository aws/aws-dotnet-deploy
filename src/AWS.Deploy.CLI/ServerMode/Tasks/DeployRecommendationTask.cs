// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
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
        private readonly IDirectoryManager _directoryManager;

        public DeployRecommendationTask(OrchestratorSession orchestratorSession, Orchestrator orchestrator, CloudApplication cloudApplication, Recommendation selectedRecommendation, IDirectoryManager directoryManager)
        {
            _orchestratorSession = orchestratorSession;
            _orchestrator = orchestrator;
            _cloudApplication = cloudApplication;
            _selectedRecommendation = selectedRecommendation;
            _directoryManager = directoryManager;
        }

        public async Task Execute()
        {
            await CreateDeploymentBundle();
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

            await CreateDeploymentBundle();
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

        private async Task CreateDeploymentBundle()
        {
            if (_selectedRecommendation.Recipe.DeploymentBundle == DeploymentBundleTypes.Container)
            {
                var dockerBuildDeploymentBundleResult = await _orchestrator.CreateContainerDeploymentBundle(_cloudApplication, _selectedRecommendation);
                if (!dockerBuildDeploymentBundleResult)
                {
                    if (!_selectedRecommendation.ProjectDefinition.HasDockerFile)
                    {
                        var projectDirectory = _directoryManager.GetDirectoryInfo(_selectedRecommendation.ProjectPath).Parent.FullName;
                        var dockerfilePath = Path.Combine(projectDirectory, "Dockerfile");
                        var errorMessage = $"Failed to create a container image from generated Docker file. " +
                            $"Please edit the Dockerfile at {dockerfilePath} to correct the required build steps for the project. Common errors are missing project dependencies not included in the Dockerfile.";

                        throw new FailedToCreateDeploymentBundleException(DeployToolErrorCode.FailedToCreateContainerDeploymentBundleFromGeneratedDockerFile, errorMessage);
                    }
                    throw new FailedToCreateDeploymentBundleException(DeployToolErrorCode.FailedToCreateContainerDeploymentBundle, "Failed to create a deployment bundle");
                }
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
