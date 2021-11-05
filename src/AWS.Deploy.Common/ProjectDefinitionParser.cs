// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using AWS.Deploy.Common.IO;

namespace AWS.Deploy.Common
{
    public interface IProjectDefinitionParser
    {
        /// <summary>
        /// Scans<paramref name="projectPath"/> for a valid project file and reads it to
        /// fully populate a <see cref="ProjectDefinition"/>
        /// </summary>
        /// <exception cref="ProjectFileNotFoundException">
        /// Thrown if no project can be found at <paramref name="projectPath"/>
        /// </exception>
        Task<ProjectDefinition> Parse(string projectPath);
    }

    public class ProjectDefinitionParser : IProjectDefinitionParser
    {
        private readonly IFileManager _fileManager;
        private readonly IDirectoryManager _directoryManager;

        public ProjectDefinitionParser(IFileManager fileManager, IDirectoryManager directoryManager)
        {
            _fileManager = fileManager;
            _directoryManager = directoryManager;
        }

        /// <summary>
        /// This method parses the target application project and sets the
        /// appropriate metadata as part of the <see cref="ProjectDefinition"/>
        /// </summary>
        /// <param name="projectPath">The project path can be an absolute or a relative path to the
        /// target application project directory or the application project file.</param>
        /// <returns><see cref="ProjectDefinition"/></returns>
        public async Task<ProjectDefinition> Parse(string projectPath)
        {
            if (_directoryManager.Exists(projectPath))
            {
                projectPath = _directoryManager.GetDirectoryInfo(projectPath).FullName;
                var files = _directoryManager.GetFiles(projectPath, "*.csproj");
                if (files.Length == 1)
                {
                    projectPath = Path.Combine(projectPath, files[0]);
                }
                else if (files.Length == 0)
                {
                    files = _directoryManager.GetFiles(projectPath, "*.fsproj");
                    if (files.Length == 1)
                    {
                        projectPath = Path.Combine(projectPath, files[0]);
                    }
                }
            }

            if (!_fileManager.Exists(projectPath))
            {
                throw new ProjectFileNotFoundException(DeployToolErrorCode.ProjectPathNotFound, $"A project was not found at the path {projectPath}.");
            }

            var xmlProjectFile = new XmlDocument();
            xmlProjectFile.LoadXml(await _fileManager.ReadAllTextAsync(projectPath));

            var projectDefinition =  new ProjectDefinition(
                xmlProjectFile,
                projectPath,
                await GetProjectSolutionFile(projectPath),
                xmlProjectFile.DocumentElement?.Attributes["Sdk"]?.Value ??
                    throw new InvalidProjectDefinitionException(DeployToolErrorCode.ProjectParserNoSdkAttribute,
                        "The project file that is being referenced does not contain and 'Sdk' attribute.")
                );

            var targetFramework = xmlProjectFile.GetElementsByTagName("TargetFramework");
            if (targetFramework.Count > 0)
            {
                projectDefinition.TargetFramework = targetFramework[0]?.InnerText;
            }

            var assemblyName = xmlProjectFile.GetElementsByTagName("AssemblyName");
            if (assemblyName.Count > 0)
            {
                projectDefinition.AssemblyName = (string.IsNullOrWhiteSpace(assemblyName[0]?.InnerText) ? Path.GetFileNameWithoutExtension(projectPath) : assemblyName[0]?.InnerText);
            }
            else
            {
                projectDefinition.AssemblyName = Path.GetFileNameWithoutExtension(projectPath);
            }

            return projectDefinition;
        }

        private async Task<string> GetProjectSolutionFile(string projectPath)
        {
            var projectDirectory = Directory.GetParent(projectPath);

            while (projectDirectory != null)
            {
                var files = _directoryManager.GetFiles(projectDirectory.FullName, "*.sln");
                foreach (var solutionFile in files)
                {
                    if (await ValidateProjectInSolution(projectPath, solutionFile))
                    {
                        return solutionFile;
                    }
                }
                projectDirectory = projectDirectory.Parent;
            }
            return string.Empty;
        }

        private async Task<bool> ValidateProjectInSolution(string projectPath, string solutionFile)
        {
            var projectFileName = Path.GetFileName(projectPath);
            if (string.IsNullOrWhiteSpace(solutionFile) ||
                string.IsNullOrWhiteSpace(projectFileName))
            {
                return false;
            }

            var lines = await _fileManager.ReadAllLinesAsync(solutionFile);
            var projectLines = lines.Where(x => x.StartsWith("Project"));
            var projectPaths =
                projectLines
                    .Select(x => x.Split(','))
                    .Where(x => x.Length > 1)
                    .Select(x =>
                            x[1]
                                .Replace('\"', ' ')
                                .Trim())
                    .Select(x => x.Replace('\\', Path.DirectorySeparatorChar))
                    .ToList();

            //Validate project exists in solution
            return projectPaths.Select(x => Path.GetFileName(x)).Any(x => x.Equals(projectFileName));
        }
    }
}
