// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;

namespace AWS.Deploy.Orchestration.CDK
{
    public static class CDKConstants
    {
        /// <summary>
        /// Deployment tool workspace directory to create CDK app during the deployment.
        /// </summary>
        public static readonly string DeployToolWorkspaceDirectoryRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aws-dotnet-deploy");

        /// <summary>
        /// Directory that contains CDK projects
        /// </summary>
        public static string ProjectsDirectory => Path.Combine(DeployToolWorkspaceDirectoryRoot, "Projects");

        /// <summary>
        /// Minimum version of CDK CLI to check before starting the deployment
        /// </summary>
        /// <remarks>
        /// Currently the version is hardcoded by design.
        /// In coming iterations, this will be dynamically calculated based on the package references used in the CDK App csproj files.
        /// </remarks>
        public static readonly Version MinimumCDKVersion = Version.Parse("1.89.0");
    }
}
