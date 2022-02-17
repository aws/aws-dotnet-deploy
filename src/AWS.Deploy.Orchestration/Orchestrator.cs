// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.DockerEngine;
using AWS.Deploy.Orchestration.CDK;
using AWS.Deploy.Orchestration.Data;
using AWS.Deploy.Orchestration.DeploymentCommands;
using AWS.Deploy.Orchestration.LocalUserSettings;
using AWS.Deploy.Orchestration.ServiceHandlers;
using AWS.Deploy.Orchestration.Utilities;

namespace AWS.Deploy.Orchestration
{
    /// <summary>
    /// The Orchestrator holds all the metadata that the CLI and the AWS toolkit for Visual studio interact with to perform a deployment.
    /// It is responsible for generating deployment recommendations, creating deployment bundles and also acts as a mediator
    /// between the client UI and the CDK.
    /// </summary>
    public class Orchestrator
    {
        internal readonly ICdkProjectHandler? _cdkProjectHandler;
        internal readonly ICDKManager? _cdkManager;
        internal readonly ICDKVersionDetector? _cdkVersionDetector;
        internal readonly IOrchestratorInteractiveService? _interactiveService;
        internal readonly IAWSResourceQueryer? _awsResourceQueryer;
        internal readonly IDeploymentBundleHandler? _deploymentBundleHandler;
        internal readonly ILocalUserSettingsEngine? _localUserSettingsEngine;
        internal readonly IDockerEngine? _dockerEngine;
        internal readonly IList<string>? _recipeDefinitionPaths;
        internal readonly IFileManager? _fileManager;
        internal readonly IDirectoryManager? _directoryManager;
        internal readonly ICustomRecipeLocator? _customRecipeLocator;
        internal readonly OrchestratorSession? _session;
        internal readonly IAWSServiceHandler? _awsServiceHandler;

        public Orchestrator(
            OrchestratorSession session,
            IOrchestratorInteractiveService interactiveService,
            ICdkProjectHandler cdkProjectHandler,
            ICDKManager cdkManager,
            ICDKVersionDetector cdkVersionDetector,
            IAWSResourceQueryer awsResourceQueryer,
            IDeploymentBundleHandler deploymentBundleHandler,
            ILocalUserSettingsEngine localUserSettingsEngine,
            IDockerEngine dockerEngine,
            ICustomRecipeLocator customRecipeLocator,
            IList<string> recipeDefinitionPaths,
            IFileManager fileManager,
            IDirectoryManager directoryManager,
            IAWSServiceHandler awsServiceHandler)
        {
            _session = session;
            _interactiveService = interactiveService;
            _cdkProjectHandler = cdkProjectHandler;
            _cdkManager = cdkManager;
            _cdkVersionDetector = cdkVersionDetector;
            _awsResourceQueryer = awsResourceQueryer;
            _deploymentBundleHandler = deploymentBundleHandler;
            _dockerEngine = dockerEngine;
            _customRecipeLocator = customRecipeLocator;
            _recipeDefinitionPaths = recipeDefinitionPaths;
            _localUserSettingsEngine = localUserSettingsEngine;
            _fileManager = fileManager;
            _directoryManager = directoryManager;
            _awsServiceHandler = awsServiceHandler;
        }

        public Orchestrator(OrchestratorSession session, IList<string> recipeDefinitionPaths)
        {
            _session = session;
            _recipeDefinitionPaths = recipeDefinitionPaths;
        }

        public async Task<List<Recommendation>> GenerateDeploymentRecommendations()
        {
            if (_recipeDefinitionPaths == null)
                throw new InvalidOperationException($"{nameof(_recipeDefinitionPaths)} is null as part of the orchestartor object");
            if (_session == null)
                throw new InvalidOperationException($"{nameof(_session)} is null as part of the orchestartor object");

            var targetApplicationFullPath = new DirectoryInfo(_session.ProjectDefinition.ProjectPath).FullName;
            var solutionDirectoryPath = !string.IsNullOrEmpty(_session.ProjectDefinition.ProjectSolutionPath) ?
                new DirectoryInfo(_session.ProjectDefinition.ProjectSolutionPath).Parent.FullName : string.Empty;

            var customRecipePaths = await LocateCustomRecipePaths(targetApplicationFullPath, solutionDirectoryPath);
            var engine = new RecommendationEngine.RecommendationEngine(_recipeDefinitionPaths.Union(customRecipePaths), _session);
            return await engine.ComputeRecommendations();
        }

        public async Task<List<Recommendation>> GenerateRecommendationsToSaveDeploymentProject()
        {
            if (_recipeDefinitionPaths == null)
                throw new InvalidOperationException($"{nameof(_recipeDefinitionPaths)} is null as part of the orchestartor object");
            if (_session == null)
                throw new InvalidOperationException($"{nameof(_session)} is null as part of the orchestartor object");

            var engine = new RecommendationEngine.RecommendationEngine(_recipeDefinitionPaths, _session);
            return await engine.ComputeRecommendations();
        }

        public async Task<List<Recommendation>> GenerateRecommendationsFromSavedDeploymentProject(string deploymentProjectPath)
        {
            if (_session == null)
                throw new InvalidOperationException($"{nameof(_session)} is null as part of the orchestartor object");
            if (_directoryManager == null)
                throw new InvalidOperationException($"{nameof(_directoryManager)} is null as part of the orchestartor object");
            if (!_directoryManager.Exists(deploymentProjectPath))
                throw new InvalidCliArgumentException(DeployToolErrorCode.DeploymentProjectPathNotFound, $"The path '{deploymentProjectPath}' does not exists on the file system. Please provide a valid deployment project path and try again.");

            var engine = new RecommendationEngine.RecommendationEngine(new List<string> { deploymentProjectPath }, _session);
            return await engine.ComputeRecommendations();
        }

        public async Task ApplyAllReplacementTokens(Recommendation recommendation, string cloudApplicationName)
        {
            if (recommendation.ReplacementTokens.ContainsKey(Constants.RecipeIdentifier.REPLACE_TOKEN_LATEST_DOTNET_BEANSTALK_PLATFORM_ARN))
            {
                if (_awsResourceQueryer == null)
                    throw new InvalidOperationException($"{nameof(_awsResourceQueryer)} is null as part of the Orchestrator object");

                var latestPlatform = await _awsResourceQueryer.GetLatestElasticBeanstalkPlatformArn();
                recommendation.AddReplacementToken(Constants.RecipeIdentifier.REPLACE_TOKEN_LATEST_DOTNET_BEANSTALK_PLATFORM_ARN, latestPlatform.PlatformArn);
            }
            if (recommendation.ReplacementTokens.ContainsKey(Constants.RecipeIdentifier.REPLACE_TOKEN_STACK_NAME))
            {
                // Apply the user entered stack name to the recommendation so that any default settings based on stack name are applied.
                recommendation.AddReplacementToken(Constants.RecipeIdentifier.REPLACE_TOKEN_STACK_NAME, cloudApplicationName);
            }
            if (recommendation.ReplacementTokens.ContainsKey(Constants.RecipeIdentifier.REPLACE_TOKEN_ECR_REPOSITORY_NAME))
            {
                recommendation.AddReplacementToken(Constants.RecipeIdentifier.REPLACE_TOKEN_ECR_REPOSITORY_NAME, cloudApplicationName.ToLower());
            }
            if (recommendation.ReplacementTokens.ContainsKey(Constants.RecipeIdentifier.REPLACE_TOKEN_ECR_IMAGE_TAG))
            {
                recommendation.AddReplacementToken(Constants.RecipeIdentifier.REPLACE_TOKEN_ECR_IMAGE_TAG, DateTime.UtcNow.Ticks.ToString());
            }
        }

        public async Task DeployRecommendation(CloudApplication cloudApplication, Recommendation recommendation)
        {
            var deploymentCommand = DeploymentCommandFactory.BuildDeploymentCommand(recommendation.Recipe.DeploymentType);
            await deploymentCommand.ExecuteAsync(this, cloudApplication, recommendation);
        }

        public async Task<bool> CreateContainerDeploymentBundle(CloudApplication cloudApplication, Recommendation recommendation)
        {
            if (_interactiveService == null)
                throw new InvalidOperationException($"{nameof(_recipeDefinitionPaths)} is null as part of the orchestartor object");
            if (_dockerEngine == null)
                throw new InvalidOperationException($"{nameof(_dockerEngine)} is null as part of the orchestartor object");
            if (_deploymentBundleHandler == null)
                throw new InvalidOperationException($"{nameof(_deploymentBundleHandler)} is null as part of the orchestartor object");

            if (!recommendation.ProjectDefinition.HasDockerFile)
            {
                _interactiveService.LogMessageLine("Generating Dockerfile...");
                try
                {
                    _dockerEngine.GenerateDockerFile();
                }
                catch (DockerEngineExceptionBase ex)
                {
                    throw new FailedToGenerateDockerFileException(DeployToolErrorCode.FailedToGenerateDockerFile, "Failed to generate a docker file", ex);
                }
            }

            _dockerEngine.DetermineDockerExecutionDirectory(recommendation);

            try
            {
                var respositoryName = recommendation.GetOptionSettingValue<string>(recommendation.GetOptionSetting("ECRRepositoryName"));

                string imageTag;
                try
                {
                    var tagSuffix = recommendation.GetOptionSettingValue<string>(recommendation.GetOptionSetting("ImageTag"));
                    imageTag = $"{respositoryName}:{tagSuffix}";
                }
                catch (OptionSettingItemDoesNotExistException)
                {
                    imageTag = $"{respositoryName}:{DateTime.UtcNow.Ticks}";
                }

                await _deploymentBundleHandler.BuildDockerImage(cloudApplication, recommendation, imageTag);
                await _deploymentBundleHandler.PushDockerImageToECR(recommendation, respositoryName, imageTag);
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
            if (_deploymentBundleHandler == null)
                throw new InvalidOperationException($"{nameof(_deploymentBundleHandler)} is null as part of the orchestartor object");
            if (_interactiveService == null)
                throw new InvalidOperationException($"{nameof(_interactiveService)} is null as part of the orchestartor object");

            try
            {
                await _deploymentBundleHandler.CreateDotnetPublishZip(recommendation);
            }
            catch (DotnetPublishFailedException exception)
            {
                _interactiveService.LogErrorMessageLine("We were unable to package the application using 'dotnet publish' due to the following error:");
                _interactiveService.LogErrorMessageLine(exception.Message);
                _interactiveService.LogDebugLine(exception.PrettyPrint());
                return false;
            }
            catch (FailedToCreateZipFileException exception)
            {
                _interactiveService.LogErrorMessageLine("We were unable to create a zip archive of the packaged application.");
                _interactiveService.LogErrorMessageLine("Normally this indicates a problem running the \"zip\" utility. Make sure that application is installed and available in your PATH.");
                _interactiveService.LogDebugLine(exception.PrettyPrint());
                return false;
            }

            return true;
        }

        public CloudApplicationResourceType GetCloudApplicationResourceType(DeploymentTypes deploymentType)
        {
            switch (deploymentType)
            {
                case DeploymentTypes.CdkProject:
                    return CloudApplicationResourceType.CloudFormationStack;

                case DeploymentTypes.BeanstalkEnvironment:
                    return CloudApplicationResourceType.BeanstalkEnvironment;

                case DeploymentTypes.ElasticContainerRegistryImage:
                    return CloudApplicationResourceType.ElasticContainerRegistryImage;

                default:
                    var errorMessage = $"Failed to find ${nameof(CloudApplicationResourceType)} from {nameof(DeploymentTypes)} {deploymentType}";
                    throw new FailedToFindCloudApplicationResourceType(DeployToolErrorCode.FailedToFindCloudApplicationResourceType, errorMessage);
            }
        }

        private async Task<List<string>> LocateCustomRecipePaths(string targetApplicationFullPath, string solutionDirectoryPath)
        {
            if (_customRecipeLocator == null)
                throw new InvalidOperationException($"{nameof(_customRecipeLocator)} is null as part of the orchestartor object");

            var customRecipePaths = new List<string>();
            foreach (var customRecipePath in await _customRecipeLocator.LocateCustomRecipePaths(targetApplicationFullPath, solutionDirectoryPath))
            {
                customRecipePaths.Add(customRecipePath);
            }
            return customRecipePaths;
        }
    }
}
