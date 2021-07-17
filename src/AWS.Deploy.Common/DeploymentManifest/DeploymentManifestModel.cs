// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Newtonsoft.Json;

namespace AWS.Deploy.Common.DeploymentManifest
{
    /// <summary>
    /// This class supports serialization and de-serialization of the deployment-manifest file.
    /// </summary>
    public class DeploymentManifestModel
    {
        [JsonProperty("deployment-projects")]
        public List<DeploymentManifestEntry> DeploymentManifestEntries { get; set; }

        public DeploymentManifestModel(List<DeploymentManifestEntry> deploymentManifestEntries)
        {
            DeploymentManifestEntries = deploymentManifestEntries;
        }
    }
}
