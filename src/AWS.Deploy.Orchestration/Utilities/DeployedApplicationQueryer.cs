// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using AWS.Deploy.Common;
using AWS.Deploy.Orchestration.Data;
using AWS.Deploy.Recipes.CDK.Common;

namespace AWS.Deploy.Orchestration.Utilities
{
    public interface IDeployedApplicationQueryer
    {
        /// <summary>
        /// Get the list of existing deployed applications by describe the CloudFormation stacks and filtering the stacks to the
        /// ones that have the AWS .NET deployment tool tag and description.
        ///
        /// If <paramref name="compatibleRecommendations"/> has any values that only existing applications that were deployed with any of the recipes
        /// identified by the recommendations will be returned.
        /// </summary>
        Task<List<CloudApplication>> GetExistingDeployedApplications(IList<Recommendation>? compatibleRecommendations = null);
    }

    public class DeployedApplicationQueryer : IDeployedApplicationQueryer
    {
        private readonly IAWSResourceQueryer _awsResourceQueryer;

        public DeployedApplicationQueryer(IAWSResourceQueryer awsResourceQueryer)
        {
            _awsResourceQueryer = awsResourceQueryer;
        }

        public async Task<List<CloudApplication>> GetExistingDeployedApplications(IList<Recommendation>? compatibleRecommendations = null)
        {
            var stacks = await _awsResourceQueryer.GetCloudFormationStacks();
            var apps = new List<CloudApplication>();

            foreach (var stack in stacks)
            {
                // Check to see if stack has AWS .NET deployment tool tag and the stack is not deleted or in the process of being deleted.
                var deployTag = stack.Tags.FirstOrDefault(tags => string.Equals(tags.Key, CloudFormationIdentifierConstants.STACK_TAG));

                // Skip stacks that don't have AWS .NET deployment tool tag
                if (deployTag == null ||

                    // Skip stacks does not have AWS .NET deployment tool description prefix. (This is filter out stacks that have the tag propagated to it like the Beanstalk stack)
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
                if (
                    compatibleRecommendations != null &&
                    !compatibleRecommendations.Any(rec => string.Equals(rec.Recipe.Id, recipeId)))
                {
                    continue;
                }

                apps.Add(new CloudApplication(stack.StackName)
                {
                    RecipeId = recipeId
                });
            }

            return apps;
        }
    }
}
