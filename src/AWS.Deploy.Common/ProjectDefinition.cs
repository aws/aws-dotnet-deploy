// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace AWS.Deploy.Common
{
    /// <summary>
    /// Stores metadata about a parsed project
    /// </summary>
    public class ProjectDefinition
    {
        private readonly XmlDocument _xmlProjectFile;
        public string ProjectPath { get; private set; }

        public ProjectDefinition(string projectPath)
        {
            if (Directory.Exists(projectPath))
            {
                var files = Directory.GetFiles(projectPath, "*.csproj");
                if (files.Length == 1)
                {
                    projectPath = Path.Combine(projectPath, files[0]);
                }
                else if (files.Length == 0)
                {
                    files = Directory.GetFiles(projectPath, "*.fsproj");
                    if (files.Length == 1)
                    {
                        projectPath = Path.Combine(projectPath, files[0]);
                    }
                }
            }

            if (!File.Exists(projectPath))
            {
                throw new ProjectFileNotFoundException(projectPath);
            }

            ProjectPath = projectPath;
            _xmlProjectFile = new XmlDocument();
            _xmlProjectFile.LoadXml(File.ReadAllText(projectPath));

            var sdkType = _xmlProjectFile.DocumentElement.Attributes["Sdk"];
            SdkType = sdkType?.Value;

            var targetFramework = _xmlProjectFile.GetElementsByTagName("TargetFramework");
            if (targetFramework.Count > 0)
            {
                TargetFramework = targetFramework[0].InnerText;
            }

            var assemblyName = _xmlProjectFile.GetElementsByTagName("AssemblyName");
            if (assemblyName.Count > 0)
            {
                AssemblyName = (string.IsNullOrWhiteSpace(assemblyName[0].InnerText) ? Path.GetFileNameWithoutExtension(projectPath) : assemblyName[0].InnerText);
            }
            else
            {
                AssemblyName = Path.GetFileNameWithoutExtension(projectPath);
            }
        }

        /// <summary>
        /// Value of the Sdk property of the root project element in a .csproj
        /// </summary>
        public string SdkType { get; private set; }

        /// <summary>
        /// Value of the TargetFramework property of the project
        /// </summary>
        public string TargetFramework { get; private set; }

        /// <summary>
        /// Value of the AssemblyName property of the project
        /// </summary>
        public string AssemblyName { get; private set; }

        /// <summary>
        /// True if we found a docker file corresponding to the .csproj
        /// </summary>
        public bool HasDockerFile => CheckIfDockerFileExists(ProjectPath);

        /// <summary>
        /// The Solution file path of the project.
        /// </summary>
        public string ProjectSolutionPath => GetProjectSolutionFile(ProjectPath);

        public string GetMSPropertyValue(string propertyName)
        {
            var propertyValue = _xmlProjectFile.SelectSingleNode($"//PropertyGroup/{propertyName}")?.InnerText;
            return propertyValue;
        }

        public string GetPackageReferenceVersion(string packageName)
        {
            var packageReference = _xmlProjectFile.SelectSingleNode($"//ItemGroup/PackageReference[@Include='{packageName}']") as XmlElement;
            return packageReference?.GetAttribute("Version");
        }

        private bool CheckIfDockerFileExists(string projectPath)
        {
            var dir = Directory.GetFiles(new FileInfo(projectPath).DirectoryName, "Dockerfile");
            return dir.Length == 1;
        }

        public static bool TryParse(string path, out ProjectDefinition projectDefinition)
        {
            projectDefinition = null;

            try
            {
                projectDefinition = new ProjectDefinition(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string GetProjectSolutionFile(string projectPath)
        {
            var projectDirectory = Directory.GetParent(projectPath);
            var solutionExists = false;
            while (solutionExists == false && projectDirectory != null)
            {
                var files = projectDirectory.GetFiles("*.sln");
                foreach (var solutionFile in files)
                {
                    if (ValidateProjectInSolution(projectPath, solutionFile.FullName))
                    {
                        return solutionFile.FullName;
                    }
                }
                projectDirectory = projectDirectory.Parent;
            }
            return string.Empty;
        }

        private bool ValidateProjectInSolution(string projectPath, string solutionFile)
        {
            var projectFileName = Path.GetFileName(projectPath);
            if (string.IsNullOrWhiteSpace(solutionFile) ||
                string.IsNullOrWhiteSpace(projectFileName))
            {
                return false;
            }
            List<string> lines = File.ReadAllLines(solutionFile).ToList();
            var projectLines = lines.Where(x => x.StartsWith("Project"));
            var projectPaths = projectLines.Select(x => x.Split(',')[1].Replace('\"', ' ').Trim()).ToList();

            //Validate project exists in solution
            return projectPaths.Select(x => Path.GetFileName(x)).Where(x => x.Equals(projectFileName)).Any();
        }
    }
}
