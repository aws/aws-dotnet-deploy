// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Common.Extensions;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Utilities;
using AWS.Deploy.DockerEngine;
using AWS.Deploy.Orchestration.CDK;
using AWS.Deploy.Orchestration.Data;
using AWS.Deploy.Orchestration.DeploymentCommands;
using AWS.Deploy.Orchestration.LocalUserSettings;
using AWS.Deploy.Orchestration.ServiceHandlers;
using AWS.Deploy.Orchestration.Utilities;
using AWS.Deploy.Recipes;

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
        internal readonly IRecipeHandler? _recipeHandler;
        internal readonly IFileManager? _fileManager;
        internal readonly IDirectoryManager? _directoryManager;
        internal readonly OrchestratorSession? _session;
        internal readonly IAWSServiceHandler? _awsServiceHandler;
        private readonly IOptionSettingHandler? _optionSettingHandler;
        internal readonly IDeployToolWorkspaceMetadata? _workspaceMetadata;

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
            IRecipeHandler recipeHandler,
            IFileManager fileManager,
            IDirectoryManager directoryManager,
            IAWSServiceHandler awsServiceHandler,
            IOptionSettingHandler optionSettingHandler,
            IDeployToolWorkspaceMetadata deployToolWorkspaceMetadata)
        {
            _session = session;
            _interactiveService = interactiveService;
            _cdkProjectHandler = cdkProjectHandler;
            _cdkManager = cdkManager;
            _cdkVersionDetector = cdkVersionDetector;
            _awsResourceQueryer = awsResourceQueryer;
            _deploymentBundleHandler = deploymentBundleHandler;
            _dockerEngine = dockerEngine;
            _recipeHandler = recipeHandler;
            _localUserSettingsEngine = localUserSettingsEngine;
            _fileManager = fileManager;
            _directoryManager = directoryManager;
            _awsServiceHandler = awsServiceHandler;
            _optionSettingHandler = optionSettingHandler;
            _workspaceMetadata = deployToolWorkspaceMetadata;
        }

        public Orchestrator(OrchestratorSession session, IRecipeHandler recipeHandler)
        {
            _session = session;
            _recipeHandler = recipeHandler;
        }

        /// <summary>
        /// Method that generates the list of recommendations to deploy with.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<List<Recommendation>> GenerateDeploymentRecommendations()
        {
            if (_session == null)
                throw new InvalidOperationException($"{nameof(_session)} is null as part of the orchestartor object");
            if (_recipeHandler == null)
                throw new InvalidOperationException($"{nameof(_recipeHandler)} is null as part of the orchestartor object");
            
            var engine = new RecommendationEngine.RecommendationEngine(_session, _recipeHandler);
            var recipePaths = new HashSet<string> { RecipeLocator.FindRecipeDefinitionsPath() };
            var customRecipePaths = await _recipeHandler.LocateCustomRecipePaths(_session.ProjectDefinition);
            return await engine.ComputeRecommendations(recipeDefinitionPaths: recipePaths.Union(customRecipePaths).ToList());
        }

        /// <summary>
        /// Method to generate the list of recommendations to create deployment projects for.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<List<Recommendation>> GenerateRecommendationsToSaveDeploymentProject()
        {
            if (_session == null)
                throw new InvalidOperationException($"{nameof(_session)} is null as part of the orchestartor object");
            if (_recipeHandler == null)
                throw new InvalidOperationException($"{nameof(_recipeHandler)} is null as part of the orchestartor object");
            
            var engine = new RecommendationEngine.RecommendationEngine(_session, _recipeHandler);
            var compatibleRecommendations = await engine.ComputeRecommendations();
            var cdkRecommendations = compatibleRecommendations.Where(x => x.Recipe.DeploymentType == DeploymentTypes.CdkProject).ToList();
            return cdkRecommendations;
        }

        /// <summary>
        /// Include in the list of recommendations the recipe the deploymentProjectPath implements.
        /// </summary>
        /// <param name="deploymentProjectPath"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="InvalidCliArgumentException"></exception>
        public async Task<List<Recommendation>> GenerateRecommendationsFromSavedDeploymentProject(string deploymentProjectPath)
        {
            if (_session == null)
                throw new InvalidOperationException($"{nameof(_session)} is null as part of the orchestartor object");
            if (_recipeHandler == null)
                throw new InvalidOperationException($"{nameof(_recipeHandler)} is null as part of the orchestartor object");
            if (_directoryManager == null)
                throw new InvalidOperationException($"{nameof(_directoryManager)} is null as part of the orchestartor object");
            if (!_directoryManager.Exists(deploymentProjectPath))
                throw new InvalidCliArgumentException(DeployToolErrorCode.DeploymentProjectPathNotFound, $"The path '{deploymentProjectPath}' does not exists on the file system. Please provide a valid deployment project path and try again.");

            var engine = new RecommendationEngine.RecommendationEngine(_session, _recipeHandler);
            return await engine.ComputeRecommendations(recipeDefinitionPaths: new List<string> { deploymentProjectPath });
        }

        /// <summary>
        /// Creates a deep copy of the recommendation object and applies the previous settings to that recommendation.
        /// </summary>
        public async Task<Recommendation> ApplyRecommendationPreviousSettings(Recommendation recommendation, IDictionary<string, object> previousSettings)
        {
            if (_optionSettingHandler == null)
                throw new InvalidOperationException($"{nameof(_optionSettingHandler)} is null as part of the orchestartor object");
            if (_interactiveService == null)
                throw new InvalidOperationException($"{nameof(_interactiveService)} is null as part of the orchestrator object");

            var recommendationCopy = recommendation.DeepCopy();
            recommendationCopy.IsExistingCloudApplication = true;

            foreach (var optionSetting in recommendationCopy.Recipe.OptionSettings)
            {
                if (previousSettings.TryGetValue(optionSetting.Id, out var value))
                {
                    try
                    {
                        await _optionSettingHandler.SetOptionSettingValue(recommendationCopy, optionSetting, value, skipValidation: true);
                    }
                    catch (UnsupportedOptionSettingType ex)
                    {
                        _interactiveService.LogErrorMessage($"Unable to retrieve value of '{optionSetting.Name}' from previous deployment. Make sure to set it again prior to redeployment.");
                        _interactiveService.LogDebugMessage(ex.Message);
                    }
                }
            }

            return recommendationCopy;
        }

        public async Task ApplyAllReplacementTokens(Recommendation recommendation, string cloudApplicationName)
        {
            if (recommendation.ReplacementTokens.ContainsKey(Constants.RecipeIdentifier.REPLACE_TOKEN_LATEST_DOTNET_BEANSTALK_PLATFORM_ARN))
            {
                if (_awsResourceQueryer == null)
                    throw new InvalidOperationException($"{nameof(_awsResourceQueryer)} is null as part of the Orchestrator object");

                var latestPlatform = await _awsResourceQueryer.GetLatestElasticBeanstalkPlatformArn(BeanstalkPlatformType.Linux);
                recommendation.AddReplacementToken(Constants.RecipeIdentifier.REPLACE_TOKEN_LATEST_DOTNET_BEANSTALK_PLATFORM_ARN, latestPlatform.PlatformArn);
            }
            if (recommendation.ReplacementTokens.ContainsKey(Constants.RecipeIdentifier.REPLACE_TOKEN_LATEST_DOTNET_WINDOWS_BEANSTALK_PLATFORM_ARN))
            {
                if (_awsResourceQueryer == null)
                    throw new InvalidOperationException($"{nameof(_awsResourceQueryer)} is null as part of the Orchestrator object");

                var latestPlatform = await _awsResourceQueryer.GetLatestElasticBeanstalkPlatformArn(BeanstalkPlatformType.Windows);
                recommendation.AddReplacementToken(Constants.RecipeIdentifier.REPLACE_TOKEN_LATEST_DOTNET_WINDOWS_BEANSTALK_PLATFORM_ARN, latestPlatform.PlatformArn);
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
            if (recommendation.ReplacementTokens.ContainsKey(Constants.RecipeIdentifier.REPLACE_TOKEN_DOCKERFILE_PATH))
            {
                if (_deploymentBundleHandler != null && DockerUtilities.TryGetDefaultDockerfile(recommendation, _fileManager, out var defaultDockerfilePath))
                {
                    recommendation.AddReplacementToken(Constants.RecipeIdentifier.REPLACE_TOKEN_DOCKERFILE_PATH, defaultDockerfilePath);
                }
            }
            if (recommendation.ReplacementTokens.ContainsKey(Constants.RecipeIdentifier.REPLACE_TOKEN_DEFAULT_VPC_ID))
            {
                if (_awsResourceQueryer == null)
                    throw new InvalidOperationException($"{nameof(_awsResourceQueryer)} is null as part of the Orchestrator object");

                var defaultVPC = await _awsResourceQueryer.GetDefaultVpc();
                recommendation.AddReplacementToken(Constants.RecipeIdentifier.REPLACE_TOKEN_DEFAULT_VPC_ID, defaultVPC?.VpcId ?? string.Empty);
            }
            if (recommendation.ReplacementTokens.ContainsKey(Constants.RecipeIdentifier.REPLACE_TOKEN_HAS_DEFAULT_VPC))
            {
                if (_awsResourceQueryer == null)
                    throw new InvalidOperationException($"{nameof(_awsResourceQueryer)} is null as part of the Orchestrator object");

                var defaultVPC = await _awsResourceQueryer.GetDefaultVpc();
                recommendation.AddReplacementToken(Constants.RecipeIdentifier.REPLACE_TOKEN_HAS_DEFAULT_VPC, defaultVPC != null);
            }
            if (recommendation.ReplacementTokens.ContainsKey(Constants.RecipeIdentifier.REPLACE_TOKEN_HAS_NOT_VPCS))
            {
                if (_awsResourceQueryer == null)
                    throw new InvalidOperationException($"{nameof(_awsResourceQueryer)} is null as part of the Orchestrator object");

                var vpcs = await _awsResourceQueryer.GetListOfVpcs();
                recommendation.AddReplacementToken(Constants.RecipeIdentifier.REPLACE_TOKEN_HAS_NOT_VPCS, !vpcs.Any());
            }
        }

        public async Task DeployRecommendation(CloudApplication cloudApplication, Recommendation recommendation)
        {
            var deploymentCommand = DeploymentCommandFactory.BuildDeploymentCommand(recommendation.Recipe.DeploymentType);
            await deploymentCommand.ExecuteAsync(this, cloudApplication, recommendation);
        }

        public async Task CreateDeploymentBundle(CloudApplication cloudApplication, Recommendation recommendation)
        {
            if (_interactiveService == null)
                throw new InvalidOperationException($"{nameof(_interactiveService)} is null as part of the orchestrator object");

            if (recommendation.Recipe.DeploymentBundle == DeploymentBundleTypes.Container)
            {
                _interactiveService.LogSectionStart("Creating deployment image",
                    "Using the docker CLI to perform a docker build to create a container image.");
                try
                {
                    await CreateContainerDeploymentBundle(cloudApplication, recommendation);
                }
                catch (DeployToolException ex)
                {
                    throw new FailedToCreateDeploymentBundleException(ex.ErrorCode, ex.Message, ex.ProcessExitCode, ex);
                }
            }
            else if (recommendation.Recipe.DeploymentBundle == DeploymentBundleTypes.DotnetPublishZipFile)
            {
                _interactiveService.LogSectionStart("Creating deployment zip bundle",
                    "Using the dotnet CLI build the project and zip the publish artifacts.");
                try
                {
                    await CreateDotnetPublishDeploymentBundle(recommendation);
                }
                catch (DeployToolException ex)
                {
                    throw new FailedToCreateDeploymentBundleException(ex.ErrorCode, ex.Message, ex.ProcessExitCode, ex);
                }
            }
        }

        private async Task CreateContainerDeploymentBundle(CloudApplication cloudApplication, Recommendation recommendation)
        {
            if (_interactiveService == null)
                throw new InvalidOperationException($"{nameof(_interactiveService)} is null as part of the orchestartor object");
            if (_dockerEngine == null)
                throw new InvalidOperationException($"{nameof(_dockerEngine)} is null as part of the orchestartor object");
            if (_deploymentBundleHandler == null)
                throw new InvalidOperationException($"{nameof(_deploymentBundleHandler)} is null as part of the orchestrator object");
            if (_optionSettingHandler == null)
                throw new InvalidOperationException($"{nameof(_optionSettingHandler)} is null as part of the orchestrator object");
            if (_fileManager == null)
                throw new InvalidOperationException($"{nameof(_fileManager)} is null as part of the orchestrator object");

            if (!DockerUtilities.TryGetDockerfile(recommendation, _fileManager, out _))
            {
                _interactiveService.LogInfoMessage("Generating Dockerfile...");
                try
                {
                    _dockerEngine.GenerateDockerFile();
                }
                catch (DockerEngineExceptionBase ex)
                {
                    var errorMessage = "Failed to generate a docker file due to the following error:" + Environment.NewLine + ex.Message;
                    throw new FailedToGenerateDockerFileException(DeployToolErrorCode.FailedToGenerateDockerFile, errorMessage, ex);
                }
            }

            _dockerEngine.DetermineDockerExecutionDirectory(recommendation);

            // Read this from the OptionSetting instead of recommendation.DeploymentBundle.
            // When its value comes from a replacement token, it wouldn't have been set back to the DeploymentBundle 
            var respositoryName = _optionSettingHandler.GetOptionSettingValue<string>(recommendation, _optionSettingHandler.GetOptionSetting(recommendation, Constants.Docker.ECRRepositoryNameOptionId));
            if (respositoryName == null)
                throw new InvalidECRRepositoryNameException(DeployToolErrorCode.ECRRepositoryNameIsNull, "The ECR Repository Name is null.");

            string imageTag;
            try
            {
                var tagSuffix = _optionSettingHandler.GetOptionSettingValue<string>(recommendation, _optionSettingHandler.GetOptionSetting(recommendation, Constants.Docker.ImageTagOptionId));
                imageTag = $"{respositoryName}:{tagSuffix}";
            }
            catch (OptionSettingItemDoesNotExistException)
            {
                imageTag = $"{respositoryName}:{DateTime.UtcNow.Ticks}";
            }

            await _deploymentBundleHandler.BuildDockerImage(cloudApplication, recommendation, imageTag);

            _interactiveService.LogSectionStart("Pushing container image to Elastic Container Registry (ECR)", "Using the docker CLI to log on to ECR and push the local image to ECR.");
            await _deploymentBundleHandler.PushDockerImageToECR(recommendation, respositoryName, imageTag);
        }

        private async Task CreateDotnetPublishDeploymentBundle(Recommendation recommendation)
        {
            if (_deploymentBundleHandler == null)
                throw new InvalidOperationException($"{nameof(_deploymentBundleHandler)} is null as part of the orchestartor object");
            if (_interactiveService == null)
                throw new InvalidOperationException($"{nameof(_interactiveService)} is null as part of the orchestartor object");

            await _deploymentBundleHandler.CreateDotnetPublishZip(recommendation);
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
    }
}
