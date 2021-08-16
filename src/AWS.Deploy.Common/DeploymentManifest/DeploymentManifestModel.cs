// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace AWS.Deploy.Common.DeploymentManifest
{
    /// <summary>
    /// This class supports serialization and de-serialization of the deployment-manifest file.
    /// </summary>
    public class DeploymentManifestModel
    {
        public List<DeploymentManifestEntry> DeploymentProjects { get; set; }

        public DeploymentManifestModel(List<DeploymentManifestEntry> deploymentProjects)
        {
            DeploymentProjects = deploymentProjects;
        }
    }
}
