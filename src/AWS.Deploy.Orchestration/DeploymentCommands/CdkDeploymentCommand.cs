// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Orchestration.DisplayedResources;

namespace AWS.Deploy.Orchestration.DeploymentCommands
{
    public class CdkDeploymentCommand : IDeploymentCommand
    {
        public async Task ExecuteAsync(Orchestrator orchestrator, CloudApplication cloudApplication, Recommendation recommendation)
        {
            if (orchestrator._interactiveService == null)
                throw new InvalidOperationException($"{nameof(orchestrator._interactiveService)} is null as part of the orchestartor object");
            if (orchestrator._cdkManager == null)
                throw new InvalidOperationException($"{nameof(orchestrator._cdkManager)} is null as part of the orchestartor object");
            if (orchestrator._cdkProjectHandler == null)
                throw new InvalidOperationException($"{nameof(CdkProjectHandler)} is null as part of the orchestartor object");
            if (orchestrator._localUserSettingsEngine == null)
                throw new InvalidOperationException($"{nameof(orchestrator._localUserSettingsEngine)} is null as part of the orchestartor object");
            if (orchestrator._session == null)
                throw new InvalidOperationException($"{nameof(orchestrator._session)} is null as part of the orchestartor object");
            if (orchestrator._cdkVersionDetector == null)
                throw new InvalidOperationException($"{nameof(orchestrator._cdkVersionDetector)} must not be null.");
            if (orchestrator._directoryManager == null)
                throw new InvalidOperationException($"{nameof(orchestrator._directoryManager)} must not be null.");

            orchestrator._interactiveService.LogMessageLine(string.Empty);
            orchestrator._interactiveService.LogMessageLine($"Initiating deployment: {recommendation.Name}");

            orchestrator._interactiveService.LogMessageLine("Configuring AWS Cloud Development Kit (CDK)...");
            var cdkProject = await orchestrator._cdkProjectHandler.ConfigureCdkProject(orchestrator._session, cloudApplication, recommendation);

            var projFiles = orchestrator._directoryManager.GetProjFiles(cdkProject);
            var cdkVersion = orchestrator._cdkVersionDetector.Detect(projFiles);

            await orchestrator._cdkManager.EnsureCompatibleCDKExists(Constants.CDK.DeployToolWorkspaceDirectoryRoot, cdkVersion);

            try
            {
                await orchestrator._cdkProjectHandler.DeployCdkProject(orchestrator._session, cloudApplication, cdkProject, recommendation);
            }
            finally
            {
                orchestrator._cdkProjectHandler.DeleteTemporaryCdkProject(orchestrator._session, cdkProject);
            }

            await orchestrator._localUserSettingsEngine.UpdateLastDeployedStack(cloudApplication.Name, orchestrator._session.ProjectDefinition.ProjectName, orchestrator._session.AWSAccountId, orchestrator._session.AWSRegion);
        }

        public async Task<List<DisplayedResourceItem>> GetDeploymentOutputsAsync(IDisplayedResourcesHandler displayedResourcesHandler, CloudApplication cloudApplication, Recommendation recommendation)
        {
            var displayedResources = new List<DisplayedResourceItem>();

            if (recommendation.Recipe.DisplayedResources == null)
                return displayedResources;

            var resources = await displayedResourcesHandler.AwsResourceQueryer.DescribeCloudFormationResources(cloudApplication.Name);
            foreach (var displayedResource in recommendation.Recipe.DisplayedResources)
            {
                var resource = resources.FirstOrDefault(x => x.LogicalResourceId.Equals(displayedResource.LogicalId));
                if (resource == null)
                    continue;

                var data = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(resource.ResourceType) && displayedResourcesHandler.DisplayedResourcesFactory.GetResource(resource.ResourceType) is var displayedResourceCommand && displayedResourceCommand != null)
                {
                    data = await displayedResourceCommand.Execute(resource.PhysicalResourceId);
                }
                displayedResources.Add(new DisplayedResourceItem(resource.PhysicalResourceId, displayedResource.Description, resource.ResourceType, data));
            }

            return displayedResources;
        }
    }
}
