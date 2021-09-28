// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Orchestration.Data;
using AWS.Deploy.Orchestration.LocalUserSettings;
using AWS.Deploy.Recipes.CDK.Common;

namespace AWS.Deploy.Orchestration.Utilities
{
    public interface IDeployedApplicationQueryer
    {
        /// <summary>
        /// Get the list of existing deployed applications by describe the CloudFormation stacks and filtering the stacks to the
        /// ones that have the AWS .NET deployment tool tag and description.
        /// </summary>
        Task<List<CloudApplication>> GetExistingDeployedApplications();

        /// <summary>
        /// Get the list of compatible applications based on the matching elements of the deployed stack and recommendation, such as Recipe Id.
        /// </summary>
        Task<List<CloudApplication>> GetCompatibleApplications(List<Recommendation> recommendations, List<CloudApplication>? allDeployedApplications = null, OrchestratorSession? session = null);
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

        public async Task<List<CloudApplication>> GetExistingDeployedApplications()
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

                apps.Add(new CloudApplication(stack.StackName, recipeId, stack.LastUpdatedTime));
            }

            return apps;
        }

        /// <summary>
        /// Filters the applications that can be re-deployed using the current set of available recommendations.
        /// </summary>
        /// <param name="allDeployedApplications"></param>
        /// <param name="recommendations"></param>
        /// <returns>A list of <see cref="CloudApplication"/> that are compatible for a re-deployment</returns>
        public async Task<List<CloudApplication>> GetCompatibleApplications(List<Recommendation> recommendations, List<CloudApplication>? allDeployedApplications = null, OrchestratorSession? session = null)
        {
            var compatibleApplications = new List<CloudApplication>();
            if (allDeployedApplications == null)
                allDeployedApplications = await GetExistingDeployedApplications();

            foreach (var app in allDeployedApplications)
            {
                if (recommendations.Any(rec => string.Equals(rec.Recipe.Id, app.RecipeId, StringComparison.Ordinal)))
                    compatibleApplications.Add(app);
            }

            if (session != null)
            {
                try
                {
                    await _localUserSettingsEngine.CleanOrphanStacks(allDeployedApplications.Select(x => x.StackName).ToList(), session.ProjectDefinition.ProjectName, session.AWSAccountId, session.AWSRegion);
                    var deploymentManifest = await _localUserSettingsEngine.GetLocalUserSettings();
                    var lastDeployedStack = deploymentManifest?.LastDeployedStacks?
                    .FirstOrDefault(x => x.Exists(session.AWSAccountId, session.AWSRegion, session.ProjectDefinition.ProjectName));

                    return compatibleApplications
                        .Select(x => {
                            x.UpdatedByCurrentUser = lastDeployedStack?.Stacks?.Contains(x.StackName) ?? false;
                            return x;
                            })
                        .OrderByDescending(x => x.UpdatedByCurrentUser)
                        .ThenByDescending(x => x.LastUpdatedTime)
                        .ToList();
                }
                catch (FailedToUpdateLocalUserSettingsFileException ex)
                {
                    _orchestratorInteractiveService.LogErrorMessageLine(ex.Message);
                    _orchestratorInteractiveService.LogDebugLine(ex.PrettyPrint());
                }
                catch (InvalidLocalUserSettingsFileException ex)
                {
                    _orchestratorInteractiveService.LogErrorMessageLine(ex.Message);
                    _orchestratorInteractiveService.LogDebugLine(ex.PrettyPrint());
                }
            }

            return compatibleApplications
                .OrderByDescending(x => x.LastUpdatedTime)
                .ToList();
        }
    }
}
