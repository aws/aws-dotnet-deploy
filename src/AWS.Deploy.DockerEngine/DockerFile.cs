// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AWS.Deploy.DockerEngine
{
    /// <summary>
    /// Encapsulates a DockerFile object
    /// </summary>
    public class DockerFile
    {
        private const string DockerFileName = "Dockerfile";

        private readonly ImageMapping _imageMapping;
        private readonly string _projectName;
        private readonly string _assemblyName;

        public DockerFile(ImageMapping imageMapping, string projectName, string? assemblyName)
        {
            if (imageMapping == null)
            {
                throw new ArgumentNullException(nameof(imageMapping), "Cannot instantiate a DockerFile with a null ImageMapping.");
            }

            if (string.IsNullOrWhiteSpace(projectName))
            {
                throw new ArgumentNullException(nameof(projectName), "Cannot instantiate a DockerFile with an empty Project Name.");
            }

            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                throw new ArgumentNullException(nameof(assemblyName), "Cannot instantiate a DockerFile with an empty AssemblyName.");
            }

            _imageMapping = imageMapping;
            _projectName = projectName;
            _assemblyName = assemblyName;
        }

        /// <summary>
        /// Writes a docker file based on project information
        /// </summary>
        public void WriteDockerFile(string projectDirectory, List<string>? projectList)
        {
            var dockerFileTemplate = ProjectUtilities.ReadTemplate();
            var projects = "";
            var projectPath = "";
            var projectFolder = "";
            if (projectList == null)
            {
                projects = $"COPY [\"{_projectName}\", \"\"]";
                projectPath = _projectName;
            }
            else
            {
                projectList = projectList.Select(x => x.Replace("\\", "/")).ToList();
                for (int i = 0; i < projectList.Count; i++)
                {
                    projects += $"COPY [\"{projectList[i]}\", \"{projectList[i].Substring(0, projectList[i].LastIndexOf("/") + 1)}\"]" + (i < projectList.Count - 1 ? Environment.NewLine : "");
                }

                projectPath = projectList.First(x => x.EndsWith(_projectName));
                if (projectPath.LastIndexOf("/") > -1)
                {
                    projectFolder = projectPath.Substring(0, projectPath.LastIndexOf("/"));
                }
            }

            var dockerFile = dockerFileTemplate
                .Replace("{docker-base-image}", _imageMapping.BaseImage)
                .Replace("{docker-build-image}", _imageMapping.BuildImage)
                .Replace("{project-path-list}", projects)
                .Replace("{project-path}", projectPath)
                .Replace("{project-folder}", projectFolder)
                .Replace("{project-name}", _projectName)
                .Replace("{assembly-name}", _assemblyName);

            File.WriteAllText(Path.Combine(projectDirectory, DockerFileName), dockerFile);
        }
    }
}
