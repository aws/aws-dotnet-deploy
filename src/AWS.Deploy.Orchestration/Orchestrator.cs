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
using AWS.Deploy.Orchestration.LocalUserSettings;

namespace AWS.Deploy.Orchestration
{
    /// <summary>
    /// The Orchestrator holds all the metadata that the CLI and the AWS toolkit for Visual studio interact with to perform a deployment.
    /// It is responsible for generating deployment recommendations, creating deployment bundles and also acts as a mediator
    /// between the client UI and the CDK.
    /// </summary>
    public class Orchestrator
    {
        private const string REPLACE_TOKEN_LATEST_DOTNET_BEANSTALK_PLATFORM_ARN = "{LatestDotnetBeanstalkPlatformArn}";

        private readonly ICdkProjectHandler? _cdkProjectHandler;
        private readonly ICDKManager? _cdkManager;
        private readonly ICDKVersionDetector? _cdkVersionDetector;
        private readonly IOrchestratorInteractiveService? _interactiveService;
        private readonly IAWSResourceQueryer? _awsResourceQueryer;
        private readonly IDeploymentBundleHandler? _deploymentBundleHandler;
        private readonly ILocalUserSettingsEngine? _localUserSettingsEngine;
        private readonly IDockerEngine? _dockerEngine;
        private readonly IList<string>? _recipeDefinitionPaths;
        private readonly IDirectoryManager? _directoryManager;
        private readonly ICustomRecipeLocator? _customRecipeLocator;
        private readonly OrchestratorSession? _session;

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
            IDirectoryManager directoryManager)
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
            _directoryManager = directoryManager;
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
            var additionalReplacements = await GetReplacements();
            return await engine.ComputeRecommendations(additionalReplacements);
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
            var additionalReplacements = await GetReplacements();
            return await engine.ComputeRecommendations(additionalReplacements);
        }

        public async Task<Dictionary<string, string>> GetReplacements()
        {
            var replacements = new Dictionary<string, string>();

            if (_awsResourceQueryer == null)
                throw new InvalidOperationException($"{nameof(_awsResourceQueryer)} is null as part of the Orchestrator object");

            var latestPlatform = await _awsResourceQueryer.GetLatestElasticBeanstalkPlatformArn();
            replacements[REPLACE_TOKEN_LATEST_DOTNET_BEANSTALK_PLATFORM_ARN] = latestPlatform.PlatformArn;

            return replacements;
        }

        public async Task DeployRecommendation(CloudApplication cloudApplication, Recommendation recommendation)
        {
            if (_interactiveService == null)
                throw new InvalidOperationException($"{nameof(_interactiveService)} is null as part of the orchestartor object");
            if (_cdkManager == null)
                throw new InvalidOperationException($"{nameof(_cdkManager)} is null as part of the orchestartor object");
            if (_cdkProjectHandler == null)
                throw new InvalidOperationException($"{nameof(_cdkProjectHandler)} is null as part of the orchestartor object");
            if (_localUserSettingsEngine == null)
                throw new InvalidOperationException($"{nameof(_localUserSettingsEngine)} is null as part of the orchestartor object");
            if (_session == null)
                throw new InvalidOperationException($"{nameof(_session)} is null as part of the orchestartor object");

            _interactiveService.LogMessageLine(string.Empty);
            _interactiveService.LogMessageLine($"Initiating deployment: {recommendation.Name}");

            switch (recommendation.Recipe.DeploymentType)
            {
                case DeploymentTypes.CdkProject:
                    if (_cdkVersionDetector == null)
                    {
                        throw new InvalidOperationException($"{nameof(_cdkVersionDetector)} must not be null.");
                    }

                    if (_directoryManager == null)
                    {
                        throw new InvalidOperationException($"{nameof(_directoryManager)} must not be null.");
                    }

                    _interactiveService.LogMessageLine("Configuring AWS Cloud Development Kit (CDK)...");
                    var cdkProject = await _cdkProjectHandler.ConfigureCdkProject(_session, cloudApplication, recommendation);

                    var projFiles = _directoryManager.GetProjFiles(cdkProject);
                    var cdkVersion = _cdkVersionDetector.Detect(projFiles);

                    await _cdkManager.EnsureCompatibleCDKExists(Constants.CDK.DeployToolWorkspaceDirectoryRoot, cdkVersion);

                    try
                    {
                        await _cdkProjectHandler.DeployCdkProject(_session, cdkProject, recommendation);
                    }
                    finally
                    {
                        _cdkProjectHandler.DeleteTemporaryCdkProject(_session, cdkProject);
                    }
                    break;
                default:
                    _interactiveService.LogErrorMessageLine($"Unknown deployment type {recommendation.Recipe.DeploymentType} specified in recipe.");
                    return;
            }

            await _localUserSettingsEngine.UpdateLastDeployedStack(cloudApplication.StackName, _session.ProjectDefinition.ProjectName, _session.AWSAccountId, _session.AWSRegion);
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
