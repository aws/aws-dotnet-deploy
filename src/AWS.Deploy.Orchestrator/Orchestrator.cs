// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Orchestrator.CDK;
using AWS.Deploy.Orchestrator.Data;
using AWS.Deploy.Orchestrator.Utilities;
using AWS.Deploy.Recipes;
using AWS.Deploy.Recipes.CDK.Common;
using Newtonsoft.Json;

namespace AWS.Deploy.Orchestrator
{
    public class Orchestrator
    {
        private const string REPLACE_TOKEN_LATEST_DOTNET_BEANSTALK_PLATFORM_ARN = "{LatestDotnetBeanstalkPlatformArn}";

        private readonly ICdkProjectHandler _cdkProjectHandler;
        private readonly IOrchestratorInteractiveService _interactiveService;
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IDeploymentBundleHandler _deploymentBundleHandler;
        private readonly IList<string> _recipeDefinitionPaths;

        private readonly OrchestratorSession _session;
        private readonly IAWSClientFactory _awsClientFactory;

        public Orchestrator(
            OrchestratorSession session,
            IOrchestratorInteractiveService interactiveService,
            ICdkProjectHandler cdkProjectHandler,
            IAWSResourceQueryer awsResourceQueryer,
            IDeploymentBundleHandler deploymentBundleHandler,
            IList<string> recipeDefinitionPaths)
        {
            _session = session;
            _interactiveService = interactiveService;
            _cdkProjectHandler = cdkProjectHandler;
            _deploymentBundleHandler = deploymentBundleHandler;
            _recipeDefinitionPaths = recipeDefinitionPaths;
            _awsResourceQueryer = awsResourceQueryer;
            _awsClientFactory = new DefaultAWSClientFactory();
        }

        public async Task<IList<Recommendation>> GenerateDeploymentRecommendations()
        {
            var engine = new RecommendationEngine.RecommendationEngine(_recipeDefinitionPaths, _session);
            var additionalReplacements = await GetReplacements();
            return await engine.ComputeRecommendations(_session.ProjectPath, additionalReplacements);
        }

        public async Task<Dictionary<string, string>> GetReplacements()
        {
            var replacements = new Dictionary<string, string>();

            var latestPlatform = await _awsResourceQueryer.GetLatestElasticBeanstalkPlatformArn(_session);
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
                await _session.CdkManager.EnsureCompatibleCDKExists(CDKConstants.DeployToolWorkspaceDirectoryRoot, CDKConstants.MinimumCDKVersion);
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

        /// <summary>
        /// Get the list of existing deployed applications by describe the CloudFormation stacks and filtering the stacks to the
        /// ones that have the AWS Deploy Tool tag and description.
        /// </summary>
        public Task<IList<CloudApplication>> GetExistingDeployedApplications()
        {
            return GetExistingDeployedApplications(null);
        }

        /// <summary>
        /// Get the list of existing deployed applications by describe the CloudFormation stacks and filtering the stacks to the
        /// ones that have the AWS Deploy Tool tag and description.
        ///
        /// If compatibleRecommendations has any values that only existing applications that were deployed with any of the recipes
        /// identified by the recommendations will be returned.
        /// </summary>
        /// <returns></returns>
        public async Task<IList<CloudApplication>> GetExistingDeployedApplications(IList<Recommendation> compatibleRecommendations)
        {
            var stacks = await _awsResourceQueryer.GetCloudFormationStacks(_session);
            var apps = new List<CloudApplication>();

            foreach (var stack in stacks)
            {
                // Check to see if stack has AWS Deploy Tool tag and the stack is not deleted or in the process of being deleted.
                var deployTag = stack.Tags.FirstOrDefault(tags => string.Equals(tags.Key, CloudFormationIdentifierConstants.STACK_TAG));

                // Skip stacks that don't have AWS Deploy Tool tag
                if (deployTag == null ||

                    // Skip stacks does not have AWS Deploy Tool description prefix. (This is filter out stacks that have the tag propagated to it like the Beanstalk stack)
                    (stack.Description == null || !stack.Description.StartsWith(CloudFormationIdentifierConstants.STACK_DESCRIPTION_PREFIX)) ||

                    // Skip tags that are deleted or in the process of being deleted
                    stack.StackStatus.ToString().StartsWith("DELETE"))
                {
                    continue;
                }

                // ROLLBACK_COMPLETE occurs when a stack creation fails and successfully rollbacks with cleaning partially created resources.
                // In this state, only a delete operation can be performed. (https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/using-cfn-describing-stacks.html)
                // We don't want to include ROLLBACK_COMPLETE because it never succeeded to deploy.
                // However, a customer can give name of new application same as ROLLBACK_COMPLETE stack, which will trigger the re-deployment flow on the ROLLBACK_COMPLETE stack.
                if (stack.StackStatus == StackStatus.ROLLBACK_COMPLETE)
                {
                    continue;
                }

                // If a list of compatible recommendations was given then skip existing applications that were used with a
                // recipe that is not compatible.
                var recipeId = deployTag.Value;
                if (compatibleRecommendations?.Count > 0 && !compatibleRecommendations.Any(rec => string.Equals(rec.Recipe.Id, recipeId)))
                {
                    continue;
                }

                apps.Add(new CloudApplication
                {
                    Name = stack.StackName,
                    RecipeId = recipeId
                });
            }

            return apps;
        }

        /// <summary>
        /// For a given Cloud Application loads the metadata for it. This includes the settings used to deploy and the recipe information.
        /// </summary>
        /// <param name="cloudApplication"></param>
        /// <returns></returns>
        public async Task<CloudApplicationMetadata> LoadCloudApplicationMetadata(string cloudApplication)
        {
            using var client = _awsClientFactory.GetAWSClient<Amazon.CloudFormation.IAmazonCloudFormation>(_session.AWSCredentials, _session.AWSRegion);

            var response = await client.GetTemplateAsync(new GetTemplateRequest
            {
                StackName = cloudApplication
            });

            return TemplateMetadataReader.ReadSettings(response.TemplateBody);
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
                throw new NoDeploymentBundleDefinitionsFoundException();
            }

            throw new NoDeploymentBundleDefinitionsFoundException();
        }

        public async Task<bool> CreateContainerDeploymentBundle(CloudApplication cloudApplication, Recommendation recommendation)
        {
            var dockerEngine =
                    new DockerEngine.DockerEngine(
                        new ProjectDefinition(recommendation.ProjectPath));

            if (!recommendation.ProjectDefinition.HasDockerFile)
            {
                _interactiveService.LogMessageLine("Generating Dockerfile...");
                dockerEngine.GenerateDockerFile();
            }

            dockerEngine.DetermineDockerExecutionDirectory(recommendation);

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
