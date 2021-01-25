// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.Common;

namespace AWS.Deploy.Orchestrator
{
    public class Orchestrator
    {
        private readonly ICdkProjectHandler _cdkProjectHandler;
        private readonly IOrchestratorInteractiveService _interactiveService;
        private readonly IList<string> _recipeDefinitionPaths;

        private readonly OrchestratorSession _session;

        public Orchestrator(
            OrchestratorSession session,
            IOrchestratorInteractiveService interactiveService,
            ICdkProjectHandler cdkProjectHandler,
            IList<string> recipeDefinitionPaths)
        {
            _session = session;
            _interactiveService = interactiveService;
            _cdkProjectHandler = cdkProjectHandler;
            _recipeDefinitionPaths = recipeDefinitionPaths;
        }

        public PreviousDeploymentSettings GetPreviousDeploymentSettings()
        {
            try
            {
                return PreviousDeploymentSettings.ReadSettings(_session.ProjectPath, _session.ConfigFile);
            }
            catch (Exception e)
            {
                _interactiveService.LogErrorMessageLine($"Warning: unable to parse config file {_session.ConfigFile ?? PreviousDeploymentSettings.DEFAULT_FILE_NAME}: {e.Message}");
                return new PreviousDeploymentSettings();
            }
        }

        public IList<Recommendation> GenerateDeploymentRecommendations()
        {
            var engine = new RecommendationEngine.RecommendationEngine(_recipeDefinitionPaths);
            return engine.ComputeRecommendations(_session.ProjectPath);
        }

        public async Task DeployRecommendation(CloudApplication cloudApplication, Recommendation recommendation)
        {
            _interactiveService.LogMessageLine($"Initiating deployment: {recommendation.Name}");

            if (recommendation.Recipe.DeploymentBundle == RecipeDefinition.DeploymentBundleTypes.Container &&
                !recommendation.ProjectDefinition.HasDockerFile)
            {
                _interactiveService.LogErrorMessageLine("Generating Dockerfile");
                var dockerEngine =
                    new DockerEngine.DockerEngine(
                        new ProjectDefinition(recommendation.ProjectPath));
                dockerEngine.GenerateDockerFile();
            }

            switch (recommendation.Recipe.DeploymentType)
            {
                case RecipeDefinition.DeploymentTypes.CdkProject:
                    await _cdkProjectHandler.CreateCdkDeployment(_session, cloudApplication, recommendation);
                    PersistDeploymentSettings(cloudApplication, recommendation);
                    break;
                default:
                    _interactiveService.LogErrorMessageLine($"Unknown deployment type {recommendation.Recipe.DeploymentType} specified in recipe.");
                    break;
            }
        }

        private void PersistDeploymentSettings(CloudApplication cloudApplication, Recommendation recommendation)
        {
            var settings = GetPreviousDeploymentSettings();
            settings.Profile = _session.AWSProfileName;
            settings.Region = _session.AWSRegion;

            var deployment = settings.Deployments.FirstOrDefault(x => string.Equals(cloudApplication.StackName, x.StackName));
            if (deployment == null)
            {
                deployment = new PreviousDeploymentSettings.DeploymentSettings();
                settings.Deployments.Add(deployment);
            }

            deployment.StackName = cloudApplication.StackName;
            deployment.RecipeId = recommendation.Recipe.Id;

            deployment.RecipeOverrideSettings.Clear();
            foreach (var option in recommendation.Recipe.OptionSettings)
            {
                var value = recommendation.GetOptionSettingValue(option.Id, true);
                if (value != null)
                {
                    deployment.RecipeOverrideSettings[option.Id] = value;
                }
            }

            try
            {
                settings.SaveSettings(_session.ProjectPath, _session.ConfigFile);
            }
            catch (Exception e)
            {
                _interactiveService.LogErrorMessageLine($"Warning: unable to save deployment settings to config file {_session.ConfigFile ?? PreviousDeploymentSettings.DEFAULT_FILE_NAME}: {e.Message}");
            }
        }
    }
}
