// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.DockerEngine;
using AWS.Deploy.Orchestration.CDK;
using AWS.Deploy.Orchestration.Data;
using AWS.Deploy.Recipes;
using Newtonsoft.Json;

namespace AWS.Deploy.Orchestration
{
    public class Orchestrator
    {
        private const string REPLACE_TOKEN_LATEST_DOTNET_BEANSTALK_PLATFORM_ARN = "{LatestDotnetBeanstalkPlatformArn}";

        private readonly ICdkProjectHandler _cdkProjectHandler;
        private readonly ICDKManager _cdkManager;
        private readonly IOrchestratorInteractiveService _interactiveService;
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IDeploymentBundleHandler _deploymentBundleHandler;
        private readonly IDockerEngine _dockerEngine;
        private readonly IList<string> _recipeDefinitionPaths;

        private readonly OrchestratorSession _session;

        public Orchestrator(
            OrchestratorSession session,
            IOrchestratorInteractiveService interactiveService,
            ICdkProjectHandler cdkProjectHandler,
            ICDKManager cdkManager,
            IAWSResourceQueryer awsResourceQueryer,
            IDeploymentBundleHandler deploymentBundleHandler,
            IDockerEngine dockerEngine,
            IList<string> recipeDefinitionPaths)
        {
            _session = session;
            _interactiveService = interactiveService;
            _cdkProjectHandler = cdkProjectHandler;
            _cdkManager = cdkManager;
            _awsResourceQueryer = awsResourceQueryer;
            _deploymentBundleHandler = deploymentBundleHandler;
            _dockerEngine = dockerEngine;
            _recipeDefinitionPaths = recipeDefinitionPaths;
        }

        public async Task<List<Recommendation>> GenerateDeploymentRecommendations()
        {
            var engine = new RecommendationEngine.RecommendationEngine(_recipeDefinitionPaths, _session);
            var additionalReplacements = await GetReplacements();
            return await engine.ComputeRecommendations(additionalReplacements);
        }

        public async Task<Dictionary<string, string>> GetReplacements()
        {
            var replacements = new Dictionary<string, string>();

            var latestPlatform = await _awsResourceQueryer.GetLatestElasticBeanstalkPlatformArn();
            replacements[REPLACE_TOKEN_LATEST_DOTNET_BEANSTALK_PLATFORM_ARN] = latestPlatform.PlatformArn;

            return replacements;
        }

        public async Task DeployRecommendation(CloudApplication cloudApplication, Recommendation recommendation)
        {
            _interactiveService.LogMessageLine(string.Empty);
            _interactiveService.LogMessageLine($"Initiating deployment: {recommendation.Name}");

            if (recommendation.Recipe.DeploymentType == DeploymentTypes.CdkProject)
            {
                _interactiveService.LogMessageLine("AWS CDK is being configured.");
                await _cdkManager.EnsureCompatibleCDKExists(CDKConstants.DeployToolWorkspaceDirectoryRoot, CDKConstants.MinimumCDKVersion);
            }

            switch (recommendation.Recipe.DeploymentType)
            {
                case DeploymentTypes.CdkProject:
                    await _cdkProjectHandler.CreateCdkDeployment(_session, cloudApplication, recommendation);
                    break;
                default:
                    _interactiveService.LogErrorMessageLine($"Unknown deployment type {recommendation.Recipe.DeploymentType} specified in recipe.");
                    break;
            }
        }

        public DeploymentBundleDefinition GetDeploymentBundleDefinition(Recommendation recommendation)
        {
            var deploymentBundleDefinitionsPath = DeploymentBundleDefinitionLocator.FindDeploymentBundleDefinitionPath();

            try
            {
                foreach (var deploymentBundleFile in Directory.GetFiles(deploymentBundleDefinitionsPath, "*.deploymentbundle", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        var content = File.ReadAllText(deploymentBundleFile);
                        var definition = JsonConvert.DeserializeObject<DeploymentBundleDefinition>(content);
                        if (definition.Type.Equals(recommendation.Recipe.DeploymentBundle))
                            return definition;
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Failed to Deserialize Deployment Bundle [{deploymentBundleFile}]: {e.Message}", e);
                    }
                }
            }
            catch(IOException)
            {
                throw new NoDeploymentBundleDefinitionsFoundException("Failed to find a deployment bundle definition");
            }

            throw new NoDeploymentBundleDefinitionsFoundException("Failed to find a deployment bundle definition");
        }

        public async Task<bool> CreateContainerDeploymentBundle(CloudApplication cloudApplication, Recommendation recommendation)
        {
            if (!recommendation.ProjectDefinition.HasDockerFile)
            {
                _interactiveService.LogMessageLine("Generating Dockerfile...");
                try
                {
                    _dockerEngine.GenerateDockerFile();
                }
                catch (DockerEngineExceptionBase ex)
                {
                    throw new FailedToGenerateDockerFileException("Failed to generate a docker file", ex);
                }
            }

            _dockerEngine.DetermineDockerExecutionDirectory(recommendation);

            try
            {
                var imageTag = await _deploymentBundleHandler.BuildDockerImage(cloudApplication, recommendation);

                await _deploymentBundleHandler.PushDockerImageToECR(cloudApplication, recommendation, imageTag);
            }
            catch(DockerBuildFailedException ex)
            {
                _interactiveService.LogErrorMessageLine("We were unable to build the docker image due to the following error:");
                _interactiveService.LogErrorMessageLine(ex.Message);
                _interactiveService.LogErrorMessageLine("Docker builds usually fail due to executing them from a working directory that is incompatible with the Dockerfile.");
                _interactiveService.LogErrorMessageLine("You can try setting the 'Docker Execution Directory' in the option settings.");
                return false;
            }

            return true;
        }

        public async Task<bool> CreateDotnetPublishDeploymentBundle(Recommendation recommendation)
        {
            try
            {
                await _deploymentBundleHandler.CreateDotnetPublishZip(recommendation);
            }
            catch (DotnetPublishFailedException ex)
            {
                _interactiveService.LogErrorMessageLine("We were unable to package the application using 'dotnet publish' due to the following error:");
                _interactiveService.LogErrorMessageLine(ex.Message);
                return false;
            }
            catch (FailedToCreateZipFileException)
            {
                _interactiveService.LogErrorMessageLine("We were unable to create a zip archive of the packaged application.");
                _interactiveService.LogErrorMessageLine("Normally this indicates a problem running the \"zip\" utility. Make sure that application is installed and available in your PATH.");
                return false;
            }

            return true;
        }
    }
}
