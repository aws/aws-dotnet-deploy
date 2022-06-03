// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;

namespace AWS.Deploy.CLI.Utilities
{
    /// <summary>
    /// Sits on top of <see cref="IProjectDefinitionParser"/> and adds UI specific logic
    /// for error handling.
    /// </summary>
    public interface IProjectParserUtility
    {
        Task<ProjectDefinition> Parse(string projectPath);
    }

    public class ProjectParserUtility : IProjectParserUtility
    {
        private readonly IProjectDefinitionParser _projectDefinitionParser;
        private readonly IDirectoryManager _directoryManager;

        public ProjectParserUtility(
            IProjectDefinitionParser projectDefinitionParser,
            IDirectoryManager directoryManager)
        {
            _projectDefinitionParser = projectDefinitionParser;
            _directoryManager = directoryManager;
        }

        public async Task<ProjectDefinition> Parse(string projectPath)
        {
            try
            {
                return await _projectDefinitionParser.Parse(projectPath);
            }
            catch (ProjectFileNotFoundException ex)
            {
                var errorMessage = ex.Message;
                if (_directoryManager.Exists(projectPath))
                {
                    var files = _directoryManager.GetFiles(projectPath, "*.sln").ToList();
                    if (files.Any())
                    {
                        errorMessage = "This directory contains a solution file, but the tool requires a project file. " +
                                                "Please run the tool from the directory that contains a .csproj/.fsproj or provide a path to the .csproj/.fsproj via --project-path flag.";
                    }
                }

                throw new FailedToFindDeployableTargetException(DeployToolErrorCode.FailedToFindDeployableTarget, errorMessage, ex);
            }
        }
    }
}
