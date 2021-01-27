// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using AWS.Deploy.Common.Recipes;

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
            SdkType = sdkType.Value;

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

        public string GetMSPropertyValue(string propertyName)
        {
            var propertyValue = _xmlProjectFile.SelectSingleNode($"//PropertyGroup/{propertyName}")?.InnerText;
            return propertyValue;
        }

        private bool CheckIfDockerFileExists(string projectPath)
        {
            var dir = Directory.GetFiles(new FileInfo(projectPath).DirectoryName, "Dockerfile");
            return dir.Length == 1;
        }
    }
}
