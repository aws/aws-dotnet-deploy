// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;

namespace AWS.Deploy.Constants
{
    internal static class CDK
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
        /// Default version of CDK CLI
        /// </summary>
        public static readonly Version DefaultCDKVersion = Version.Parse("2.13.0");

        /// <summary>
        /// The file path of the CDK bootstrap template to be used
        /// </summary>
        public static string CDKBootstrapTemplatePath => Path.Combine(DeployToolWorkspaceDirectoryRoot, "CDKBootstrapTemplate.yaml");
    }
}
