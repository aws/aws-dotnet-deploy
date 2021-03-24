// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

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
    public class ProjectParserUtility
    {
        private readonly IToolInteractiveService _toolInteractiveService;
        private readonly IProjectDefinitionParser _projectDefinitionParser;
        private readonly IDirectoryManager _directoryManager;

        public ProjectParserUtility(
            IToolInteractiveService toolInteractiveService,
            IProjectDefinitionParser projectDefinitionParser,
            IDirectoryManager directoryManager)
        {
            _toolInteractiveService = toolInteractiveService;
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
                var files = _directoryManager.GetFiles(projectPath, "*.sln");

                if (files.Any())
                    _toolInteractiveService.WriteErrorLine(
                        "This directory contains a solution file, but the tool requires a project file. " +
                        "Please run the tool from the directory that contains a .csproj/.fsproj or provide a path to the .csproj/.fsproj via --project-path flag.");
                else
                    _toolInteractiveService.WriteErrorLine($"A project was not found at the path {projectPath}");

                throw new FailedToFindDeployableTargetException(ex);
            }
        }
    }
}
