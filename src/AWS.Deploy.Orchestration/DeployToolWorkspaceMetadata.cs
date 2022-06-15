// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Orchestration.Utilities;

namespace AWS.Deploy.Orchestration
{
    public interface IDeployToolWorkspaceMetadata
    {
        /// <summary>
        /// Deployment tool workspace directory to create CDK app during the deployment.
        /// </summary>
        string DeployToolWorkspaceDirectoryRoot { get; }

        /// <summary>
        /// Directory that contains CDK projects
        /// </summary>
        string ProjectsDirectory { get; }

        /// <summary>
        /// The file path of the CDK bootstrap template to be used
        /// </summary>
        string CDKBootstrapTemplatePath { get; }
    }

    public class DeployToolWorkspaceMetadata : IDeployToolWorkspaceMetadata
    {
        private readonly IDirectoryManager _directoryManager;
        private readonly IEnvironmentVariableManager _environmentVariableManager;

        public string DeployToolWorkspaceDirectoryRoot
        {
            get
            {
                var workspace = Helpers.GetDeployToolWorkspaceDirectoryRoot(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), _directoryManager, _environmentVariableManager);
                if (!_directoryManager.Exists(workspace))
                    _directoryManager.CreateDirectory(workspace);
                return workspace;
            }
        }

        public string ProjectsDirectory => Path.Combine(DeployToolWorkspaceDirectoryRoot, "Projects");
        public string CDKBootstrapTemplatePath => Path.Combine(DeployToolWorkspaceDirectoryRoot, "CDKBootstrapTemplate.yaml");

        public DeployToolWorkspaceMetadata(IDirectoryManager directoryManager, IEnvironmentVariableManager environmentVariableManager)
        {
            _directoryManager = directoryManager;
            _environmentVariableManager = environmentVariableManager;
        }
    }
}
