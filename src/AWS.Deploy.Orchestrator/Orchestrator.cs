// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AWS.Deploy.Common;
using AWS.Deploy.Orchestrator.Utilities;
using AWS.DeploymentCommon;

namespace AWS.Deploy.Orchestrator
{
    public class Orchestrator
    {
        private const string DEPLOYMENT_ENGINE_CDKPROJECT = "CdkProject";

        private readonly IOrchestratorInteractiveService _interactiveService;
        private readonly IList<string> _recipeDefinitionPaths;
        private readonly CommandLineWrapper _commandLineWrapper;

        private readonly OrchestratorSession _session;

        public Orchestrator(OrchestratorSession session, IOrchestratorInteractiveService interactiveService, string recipeDefinitionPath)
            : this(session, interactiveService, new List<string> { recipeDefinitionPath })
        {
        }

        public Orchestrator(OrchestratorSession session, IOrchestratorInteractiveService interactiveService, IList<string> recipeDefinitionPaths)
        {
            _session = session;
            _interactiveService = interactiveService;
            _recipeDefinitionPaths = recipeDefinitionPaths;

            _commandLineWrapper = new CommandLineWrapper(_interactiveService, _session.AWSCredentials, _session.AWSRegion);
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
            var engine = new RecommendationEngine(_recipeDefinitionPaths);
            return engine.ComputeRecommendations(_session.ProjectPath);
        }

        public bool DeployRecommendation(string cloudApplicationName, Recommendation recommendation)
        {
            _interactiveService.LogMessageLine($"Initiating deployment: {recommendation.Name}");

            if (recommendation.Recipe.DeploymentBundle == RecipeDefinition.DeploymentBundleTypes.Container &&
                !recommendation.ProjectDefinition.HasDockerFile)
            {
                _interactiveService.LogErrorMessageLine($"Generating Dockerfile");
                var dockerEngine = new DockerEngine.DockerEngine(recommendation.ProjectPath);
                dockerEngine.GenerateDockerFile();
            }

            bool success;
            switch (recommendation.Recipe.DeploymentType)
            {
                case RecipeDefinition.DeploymentTypes.CdkProject:
                    success = CdkDeployment(cloudApplicationName, recommendation);
                    break;
                default:
                    _interactiveService.LogErrorMessageLine($"Unknown deployment type {recommendation.Recipe.DeploymentType} specified in recipe.");
                    success = false;
                    break;
            }

            if (success)
            {
                PersistDeploymentSettings(cloudApplicationName, recommendation);
            }

            return success;
        }

        private bool CdkDeployment(string cloudApplicationName, Recommendation recommendation)
        {
            JsonSerializer.Serialize(recommendation);

            var cdkProjectDirectory = Path.Combine(Path.GetDirectoryName(recommendation.Recipe.RecipePath), recommendation.Recipe.CdkProjectTemplate);

            // Write required configuration in appsettings.json
            var appSettingsFilePath = Path.Combine(cdkProjectDirectory, "appsettings.json");
            var recommendationSerializer = new CdkAppSettingsSerializer(cloudApplicationName, recommendation);
            recommendationSerializer.Write(appSettingsFilePath);

            // Handover to CDK command line tool
            var commands = new List<string> { "cdk deploy --require-approval never" };
            _commandLineWrapper.Run(commands, cdkProjectDirectory);

            // fake
            return true;
        }

        private void PersistDeploymentSettings(string cloudApplicationName, Recommendation recommendation)
        {
            var settings = GetPreviousDeploymentSettings();
            settings.Profile = _session.AWSProfileName;
            settings.Region = _session.AWSRegion;

            var deployment = settings.Deployments.FirstOrDefault(x => string.Equals(cloudApplicationName, x.StackName));
            if (deployment == null)
            {
                deployment = new PreviousDeploymentSettings.DeploymentSettings();
                settings.Deployments.Add(deployment);
            }

            deployment.StackName = cloudApplicationName;
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
