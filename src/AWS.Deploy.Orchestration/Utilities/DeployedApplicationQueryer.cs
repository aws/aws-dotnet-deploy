// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Orchestration.Data;
using AWS.Deploy.Orchestration.LocalUserSettings;
using AWS.Deploy.Recipes.CDK.Common;

namespace AWS.Deploy.Orchestration.Utilities
{
    public interface IDeployedApplicationQueryer
    {
        /// <summary>
        /// Get the list of existing deployed <see cref="CloudApplication"/> based on the deploymentTypes filter.
        /// </summary>
        Task<List<CloudApplication>> GetExistingDeployedApplications(List<DeploymentTypes> deploymentTypes);

        /// <summary>
        /// Get the list of compatible applications by matching elements of the CloudApplication RecipeId and the recommendation RecipeId.
        /// </summary>
        Task<List<CloudApplication>> GetCompatibleApplications(List<Recommendation> recommendations, List<CloudApplication>? allDeployedApplications = null, OrchestratorSession? session = null);

        /// <summary>
        /// Checks if the given recommendation can be used for a redeployment to an existing cloudformation stack.
        /// </summary>
        bool IsCompatible(CloudApplication application, Recommendation recommendation);

        /// <summary>
        /// Gets the current option settings associated with the cloud application. This method is only used for non-CloudFormation based cloud applications.
        /// </summary>
        Task<IDictionary<string, object>> GetPreviousSettings(CloudApplication application);
    }

    public class DeployedApplicationQueryer : IDeployedApplicationQueryer
    {
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly ILocalUserSettingsEngine _localUserSettingsEngine;
        private readonly IOrchestratorInteractiveService _orchestratorInteractiveService;

        public DeployedApplicationQueryer(
            IAWSResourceQueryer awsResourceQueryer,
            ILocalUserSettingsEngine localUserSettingsEngine,
            IOrchestratorInteractiveService orchestratorInteractiveService)
        {
            _awsResourceQueryer = awsResourceQueryer;
            _localUserSettingsEngine = localUserSettingsEngine;
            _orchestratorInteractiveService = orchestratorInteractiveService;
        }

        public async Task<List<CloudApplication>> GetExistingDeployedApplications(List<DeploymentTypes> deploymentTypes)
        {
            var existingApplications = new List<CloudApplication>();

            if (deploymentTypes.Contains(DeploymentTypes.CdkProject))
                existingApplications.AddRange(await GetExistingCloudFormationStacks());

            if (deploymentTypes.Contains(DeploymentTypes.BeanstalkEnvironment))
                existingApplications.AddRange(await GetExistingBeanstalkEnvironments());

            return existingApplications;
        }

        /// <summary>
        /// Filters the applications that can be re-deployed using the current set of available recommendations.
        /// </summary>
        /// <returns>A list of <see cref="CloudApplication"/> that are compatible for a re-deployment</returns>
        public async Task<List<CloudApplication>> GetCompatibleApplications(List<Recommendation> recommendations, List<CloudApplication>? allDeployedApplications = null, OrchestratorSession? session = null)
        {
            var compatibleApplications = new List<CloudApplication>();
            if (allDeployedApplications == null)
                allDeployedApplications = await GetExistingDeployedApplications(recommendations.Select(x => x.Recipe.DeploymentType).ToList());

            foreach (var application in allDeployedApplications)
            {
                if (recommendations.Any(rec => IsCompatible(application, rec)))
                {
                    compatibleApplications.Add(application);
                }
            }

            if (session != null)
            {
                try
                {
                    await _localUserSettingsEngine.CleanOrphanStacks(allDeployedApplications.Select(x => x.Name).ToList(), session.ProjectDefinition.ProjectName, session.AWSAccountId, session.AWSRegion);
                    var deploymentManifest = await _localUserSettingsEngine.GetLocalUserSettings();
                    var lastDeployedStack = deploymentManifest?.LastDeployedStacks?
                    .FirstOrDefault(x => x.Exists(session.AWSAccountId, session.AWSRegion, session.ProjectDefinition.ProjectName));

                    return compatibleApplications
                        .Select(x => {
                            x.UpdatedByCurrentUser = lastDeployedStack?.Stacks?.Contains(x.Name) ?? false;
                            return x;
                            })
                        .OrderByDescending(x => x.UpdatedByCurrentUser)
                        .ThenByDescending(x => x.LastUpdatedTime)
                        .ToList();
                }
                catch (FailedToUpdateLocalUserSettingsFileException ex)
                {
                    _orchestratorInteractiveService.LogErrorMessage(ex.Message);
                    _orchestratorInteractiveService.LogDebugMessage(ex.PrettyPrint());
                }
                catch (InvalidLocalUserSettingsFileException ex)
                {
                    _orchestratorInteractiveService.LogErrorMessage(ex.Message);
                    _orchestratorInteractiveService.LogDebugMessage(ex.PrettyPrint());
                }
            }

            return compatibleApplications
                .OrderByDescending(x => x.LastUpdatedTime)
                .ToList();
        }

        /// <summary>
        /// Checks if the given recommendation can be used for a redeployment to an existing cloudformation stack.
        /// </summary>
        public bool IsCompatible(CloudApplication application, Recommendation recommendation)
        {
            // For persisted projects check both the recipe id and the base recipe id for compatibility. The base recipe id check is for stacks that
            // were first created by a system recipe and then later moved to a persisted deployment project.
            if (recommendation.Recipe.PersistedDeploymentProject)
            {
                return string.Equals(recommendation.Recipe.Id, application.RecipeId, StringComparison.Ordinal) || string.Equals(recommendation.Recipe.BaseRecipeId, application.RecipeId, StringComparison.Ordinal);
            }
            return string.Equals(recommendation.Recipe.Id, application.RecipeId, StringComparison.Ordinal);
        }

        /// <summary>
        /// Gets the current option settings associated with the cloud application.This method is only used for non-CloudFormation based cloud applications.
        /// </summary>
        public async Task<IDictionary<string, object>> GetPreviousSettings(CloudApplication application)
        {
            IDictionary<string, object> previousSettings;
            switch (application.ResourceType)
            {
                case CloudApplicationResourceType.BeanstalkEnvironment:
                    previousSettings = await GetBeanstalkEnvironmentConfigurationSettings(application.Name);
                    break;
                default:
                    throw new InvalidOperationException($"Cannot fetch existing option settings for the following {nameof(CloudApplicationResourceType)}: {application.ResourceType}");
            }
            return previousSettings;
        }

        /// <summary>
        /// Fetches existing CloudFormation stacks created by the AWS .NET deployment tool
        /// </summary>
        /// <returns>A list of <see cref="CloudApplication"/></returns>
        private async Task<List<CloudApplication>> GetExistingCloudFormationStacks()
        {
            var stacks = await _awsResourceQueryer.GetCloudFormationStacks();
            var apps = new List<CloudApplication>();

            foreach (var stack in stacks)
            {
                // Check to see if stack has AWS .NET deployment tool tag and the stack is not deleted or in the process of being deleted.
                var deployTag = stack.Tags.FirstOrDefault(tags => string.Equals(tags.Key, Constants.CloudFormationIdentifier.STACK_TAG));

                // Skip stacks that don't have AWS .NET deployment tool tag
                if (deployTag == null ||

                    // Skip stacks does not have AWS .NET deployment tool description prefix. (This is filter out stacks that have the tag propagated to it like the Beanstalk stack)
                    (stack.Description == null || !stack.Description.StartsWith(Constants.CloudFormationIdentifier.STACK_DESCRIPTION_PREFIX)) ||

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

                apps.Add(new CloudApplication(stack.StackName, stack.StackId, CloudApplicationResourceType.CloudFormationStack, recipeId, stack.LastUpdatedTime));
            }

            return apps;
        }

        /// <summary>
        /// Fetches existing Elastic Beanstalk environments that can serve as a deployment target.
        /// These environments must have a valid dotnet specific platform arn.
        /// Any environment that was created via the AWS .NET deployment tool as part of a CloudFormation stack is not included.
        /// </summary>
        /// <returns>A list of <see cref="CloudApplication"/></returns>
        private async Task<List<CloudApplication>> GetExistingBeanstalkEnvironments()
        {
            var validEnvironments = new List<CloudApplication>();
            var environments = await _awsResourceQueryer.ListOfElasticBeanstalkEnvironments();

            if (!environments.Any())
                return validEnvironments;

            var dotnetPlatformArns = (await _awsResourceQueryer.GetElasticBeanstalkPlatformArns()).Select(x => x.PlatformArn).ToList();

            // only select environments that have a dotnet specific platform ARN.
            environments = environments.Where(x => x.Status == EnvironmentStatus.Ready && dotnetPlatformArns.Contains(x.PlatformArn)).ToList();

            foreach (var env in environments)
            {
                var tags = await _awsResourceQueryer.ListElasticBeanstalkResourceTags(env.EnvironmentArn);

                // skips all environments that were created via the deploy tool.
                if (tags.Any(x => string.Equals(x.Key, Constants.CloudFormationIdentifier.STACK_TAG)))
                    continue;

                validEnvironments.Add(new CloudApplication(env.EnvironmentName, env.EnvironmentId, CloudApplicationResourceType.BeanstalkEnvironment, Constants.RecipeIdentifier.EXISTING_BEANSTALK_ENVIRONMENT_RECIPE_ID, env.DateUpdated));
            }

            return validEnvironments;
        }

        private async Task<IDictionary<string, object>> GetBeanstalkEnvironmentConfigurationSettings(string environmentName)
        {
            IDictionary<string, object> optionSettings = new Dictionary<string, object>();
            var configurationSettings = await _awsResourceQueryer.GetBeanstalkEnvironmentConfigurationSettings(environmentName);

            foreach (var tuple in Constants.ElasticBeanstalk.OptionSettingQueryList)
            {
                var configurationSetting = GetBeanstalkEnvironmentConfigurationSetting(configurationSettings, tuple.OptionSettingNameSpace, tuple.OptionSettingName);

                if (string.IsNullOrEmpty(configurationSetting?.Value))
                    continue;

                optionSettings[tuple.OptionSettingId] = configurationSetting.Value;
            }

            return optionSettings;
        }

        private ConfigurationOptionSetting GetBeanstalkEnvironmentConfigurationSetting(List<ConfigurationOptionSetting> configurationSettings, string optionNameSpace, string optionName)
        {
            var configurationSetting = configurationSettings
                .FirstOrDefault(x => string.Equals(optionNameSpace, x.Namespace) && string.Equals(optionName, x.OptionName));

            return configurationSetting;
        }
    }
}
