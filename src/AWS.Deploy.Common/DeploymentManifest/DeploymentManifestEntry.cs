// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Newtonsoft.Json;

namespace AWS.Deploy.Common.DeploymentManifest
{
    /// <summary>
    /// This class supports serialization and de-serialization of the deployment-manifest file.
    /// </summary>
    public class DeploymentManifestEntry
    {
        [JsonProperty("Path")]
        public string SaveCdkDirectoryRelativePath { get; set; }

        public DeploymentManifestEntry(string saveCdkDirectoryRelativePath)
        {
            SaveCdkDirectoryRelativePath = saveCdkDirectoryRelativePath;
        }
    }
}
