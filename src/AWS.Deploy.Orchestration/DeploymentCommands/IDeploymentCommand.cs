// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Orchestration.DisplayedResources;

namespace AWS.Deploy.Orchestration.DeploymentCommands
{
    public interface IDeploymentCommand
    {
        Task ExecuteAsync(Orchestrator orchestrator, CloudApplication cloudApplication, Recommendation recommendation);
        Task<List<DisplayedResourceItem>> GetDeploymentOutputsAsync(IDisplayedResourcesHandler displayedResourcesHandler, CloudApplication cloudApplication, Recommendation recommendation);
    }
}
