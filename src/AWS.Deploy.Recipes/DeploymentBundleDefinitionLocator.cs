// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.IO;

namespace AWS.Deploy.Recipes
{
    public class DeploymentBundleDefinitionLocator
    {
        public static string FindDeploymentBundleDefinitionPath()
        {
            var assemblyPath = typeof(DeploymentBundleDefinitionLocator).Assembly.Location;
            var deploymentBundleDefinitionPath = Path.Combine(Directory.GetParent(assemblyPath).FullName, "DeploymentBundleDefinitions");
            return deploymentBundleDefinitionPath;
        }
    }
}
