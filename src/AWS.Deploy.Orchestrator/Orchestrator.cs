// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation.Model;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Orchestrator.Data;
using AWS.Deploy.Orchestrator.Utilities;
using AWS.Deploy.Recipes.CDK.Common;

namespace AWS.Deploy.Orchestrator
{
    public class Orchestrator
    {
        private const string REPLACE_TOKEN_LATEST_DOTNET_BEANSTALK_PLATFORM_ARN = "{LatestDotnetBeanstalkPlatformArn}";

        private readonly ICdkProjectHandler _cdkProjectHandler;
        private readonly IOrchestratorInteractiveService _interactiveService;
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IList<string> _recipeDefinitionPaths;

        private readonly OrchestratorSession _session;
        private readonly IAWSClientFactory _awsClientFactory;

        public Orchestrator(
            OrchestratorSession session,
            IOrchestratorInteractiveService interactiveService,
            ICdkProjectHandler cdkProjectHandler,
            IAWSResourceQueryer awsResourceQueryer,
            IList<string> recipeDefinitionPaths)
        {
            _session = session;
            _interactiveService = interactiveService;
            _cdkProjectHandler = cdkProjectHandler;
            _recipeDefinitionPaths = recipeDefinitionPaths;
            _awsResourceQueryer = awsResourceQueryer;
            _awsClientFactory = new DefaultAWSClientFactory();
        }

        public async Task<IList<Recommendation>> GenerateDeploymentRecommendations()
        {
            var engine = new RecommendationEngine.RecommendationEngine(_recipeDefinitionPaths);
            var additionalReplacements = await GetReplacements();
            return engine.ComputeRecommendations(_session.ProjectPath, additionalReplacements);
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
            _interactiveService.LogMessageLine($"Initiating deployment: {recommendation.Name}");

            if (recommendation.Recipe.DeploymentBundle == DeploymentBundleTypes.Container &&
                !recommendation.ProjectDefinition.HasDockerFile)
            {
                _interactiveService.LogMessageLine("Generating Dockerfile");
                var dockerEngine =
                    new DockerEngine.DockerEngine(
                        new ProjectDefinition(recommendation.ProjectPath));
                dockerEngine.GenerateDockerFile();
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
        /// <returns></returns>
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
            using var client = _awsClientFactory.GetAWSClient<Amazon.CloudFormation.IAmazonCloudFormation>(_session.AWSCredentials, _session.AWSRegion);

            var apps = new List<CloudApplication>();
            await foreach (var stack in client.Paginators.DescribeStacks(new DescribeStacksRequest()).Stacks)
            {
                // Check to see if stack has AWS Deploy Tool tag and the stack is not deleted or in the process of being deleted.
                var deployTag = stack.Tags.FirstOrDefault(tags => string.Equals(tags.Key, CloudFormationIdentifierContants.StackTag));

                // Skip stacks that don't have AWS Deploy Tool tag
                if (deployTag == null ||

                    // Skip stacks does not have AWS Deploy Tool description prefix. (This is filter out stacks that have the tag propagated to it like the Beanstalk stack)
                    (stack.Description == null || !stack.Description.StartsWith(CloudFormationIdentifierContants.StackDescriptionPrefix)) ||

                    // Skip tags that are deleted or in the process of being deleted
                    stack.StackStatus.ToString().StartsWith("DELETE"))
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
        public async Task<CloudApplicationMetadata> LoadCloudApplicationMetadataAsync(string cloudApplication)
        {
            using var client = _awsClientFactory.GetAWSClient<Amazon.CloudFormation.IAmazonCloudFormation>(_session.AWSCredentials, _session.AWSRegion);

            var response = await client.GetTemplateAsync(new GetTemplateRequest
            {
                StackName = cloudApplication
            });

            var reader = new TemplateMetadataReader(response.TemplateBody);
            return reader.ReadSettings();
        }
    }
}
