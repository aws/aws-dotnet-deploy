// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using AWS.Deploy.Common;
using System.Threading.Tasks;
using AWS.Deploy.Orchestration.Data;
using System.Linq;
using AWS.Deploy.Orchestration.DeploymentCommands;
using AWS.Deploy.Common.Data;

namespace AWS.Deploy.Orchestration.DisplayedResources
{
    public interface IDisplayedResourcesHandler
    {
        IAWSResourceQueryer AwsResourceQueryer { get; }
        IDisplayedResourceCommandFactory DisplayedResourcesFactory { get; }
        Task<List<DisplayedResourceItem>> GetDeploymentOutputs(CloudApplication cloudApplication, Recommendation recommendation);
    }

    public class DisplayedResourcesHandler : IDisplayedResourcesHandler
    {
        public IAWSResourceQueryer AwsResourceQueryer { get; }
        public IDisplayedResourceCommandFactory DisplayedResourcesFactory { get; }

        public DisplayedResourcesHandler(IAWSResourceQueryer awsResourceQueryer, IDisplayedResourceCommandFactory displayedResourcesFactory)
        {
            AwsResourceQueryer = awsResourceQueryer;
            DisplayedResourcesFactory = displayedResourcesFactory;
        }

        /// <summary>
        /// Retrieves the displayed resource data for known resource types by executing specific resource commands.
        /// For unknown resource types, this returns the physical resource ID and type.
        /// </summary>
        public async Task<List<DisplayedResourceItem>> GetDeploymentOutputs(CloudApplication cloudApplication, Recommendation recommendation)
        {
            var deploymentCommand = DeploymentCommandFactory.BuildDeploymentCommand(recommendation.Recipe.DeploymentType);
            return await deploymentCommand.GetDeploymentOutputsAsync(this, cloudApplication, recommendation);
        }
    }
}
