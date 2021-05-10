// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace AWS.Deploy.Common
{
    /// <summary>
    /// Models metadata about a parsed .csproj or .fsproj project.
    /// Use <see cref="IProjectDefinitionParser.Parse"/> to build
    /// </summary>
    public class ProjectDefinition
    {
        /// <summary>
        /// Xml file contents of the Project file.
        /// </summary>
        public XmlDocument Contents { get; set; }

        /// <summary>
        /// Full path to the project file
        /// </summary>
        public string ProjectPath { get; set; }

        /// <summary>
        /// The Solution file path of the project.
        /// </summary>
        public string ProjectSolutionPath { get;set; }

        /// <summary>
        /// Value of the Sdk property of the root project element in a .csproj
        /// </summary>
        public string SdkType { get; set; }

        /// <summary>
        /// Value of the TargetFramework property of the project
        /// </summary>
        public string? TargetFramework { get; set; }

        /// <summary>
        /// Value of the AssemblyName property of the project
        /// </summary>
        public string? AssemblyName { get; set; }

        /// <summary>
        /// True if we found a docker file corresponding to the .csproj
        /// </summary>
        public bool HasDockerFile => CheckIfDockerFileExists(ProjectPath);

        public ProjectDefinition(
            XmlDocument contents,
            string projectPath,
            string projectSolutionPath,
            string sdkType)
        {
            Contents = contents;
            ProjectPath = projectPath;
            ProjectSolutionPath = projectSolutionPath;
            SdkType = sdkType;
        }

        public string? GetMSPropertyValue(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return null;
            var propertyValue = Contents.SelectSingleNode($"//PropertyGroup/{propertyName}")?.InnerText;
            return propertyValue;
        }

        public string? GetPackageReferenceVersion(string? packageName)
        {
            if (string.IsNullOrEmpty(packageName))
                return null;
            var packageReference = Contents.SelectSingleNode($"//ItemGroup/PackageReference[@Include='{packageName}']") as XmlElement;
            return packageReference?.GetAttribute("Version");
        }

        private bool CheckIfDockerFileExists(string projectPath)
        {
            var dir = Directory.GetFiles(new FileInfo(projectPath).DirectoryName, "Dockerfile");
            return dir.Length == 1;
        }
    }
}
