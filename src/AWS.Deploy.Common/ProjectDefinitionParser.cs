// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.ProjectEvaluation;

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
        private readonly IMSBuildEvaluator _msBuildEvaluator;

        public ProjectDefinitionParser(IFileManager fileManager, IDirectoryManager directoryManager, IMSBuildEvaluator msBuildEvaluator)
        {
            _fileManager = fileManager;
            _directoryManager = directoryManager;
            _msBuildEvaluator = msBuildEvaluator;
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
                throw new ProjectFileNotFoundException(DeployToolErrorCode.ProjectPathNotFound, $"Failed to find a valid .csproj or .fsproj file at path {projectPath}");
            }

            var extension = Path.GetExtension(projectPath);
            if (!string.Equals(extension, ".csproj") && !string.Equals(extension, ".fsproj"))
            {
                var errorMeesage = $"Invalid project path {projectPath}. The project path must point to a .csproj or .fsproj file";
                throw new ProjectFileNotFoundException(DeployToolErrorCode.ProjectPathNotFound, errorMeesage);
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

            // Run MSBuild evaluation for accurate property/item resolution.
            // This handles Directory.Build.props, Central Package Management, conditions, etc.
            // Falls back gracefully to raw XML when evaluation is unavailable.
            projectDefinition.Evaluation = await _msBuildEvaluator.EvaluateAsync(projectPath);

            // Populate TargetFramework — prefer evaluated value over raw XML
            projectDefinition.TargetFramework = projectDefinition.GetMSPropertyValue("TargetFramework")
                ?? xmlProjectFile.GetElementsByTagName("TargetFramework")[0]?.InnerText;

            // Populate AssemblyName — prefer evaluated value over raw XML
            var evaluatedAssemblyName = projectDefinition.GetMSPropertyValue("AssemblyName");
            if (!string.IsNullOrWhiteSpace(evaluatedAssemblyName))
            {
                projectDefinition.AssemblyName = evaluatedAssemblyName;
            }
            else
            {
                var assemblyName = xmlProjectFile.GetElementsByTagName("AssemblyName");
                projectDefinition.AssemblyName = assemblyName.Count > 0 && !string.IsNullOrWhiteSpace(assemblyName[0]?.InnerText)
                    ? assemblyName[0]!.InnerText
                    : Path.GetFileNameWithoutExtension(projectPath);
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
