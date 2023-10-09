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
        private readonly ImageMapping _imageMapping;
        private readonly string _projectName;
        private readonly string _assemblyName;
        private readonly int _port;
        private readonly bool _useRootUser;
        private readonly string _httpPortEnvironmentVariable;

        public DockerFile(ImageMapping imageMapping, string projectName, string? assemblyName, int port, bool useRootUser, string httpPortEnvironmentVariable)
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
            _port = port;
            _useRootUser = useRootUser;
            _httpPortEnvironmentVariable = httpPortEnvironmentVariable;
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

            // Microsoft exposes 8081 along with 8080 in their .NET8 templates. I am preserving that behavior here when port 8080 is used.
            if (_port == 8080)
            {
                dockerFile = dockerFile
                    .Replace("{exposed-ports}", $"EXPOSE {_port}\r\nEXPOSE 8081");
                dockerFile = dockerFile
                    .Replace("{http-port-env-variable}", string.Empty);
            }
            // Microsoft exposes 443 along with 80 in their .NET7 and older templates. I am preserving that behavior here when port 80 is used.
            else if (_port == 80)
            {
                dockerFile = dockerFile
                    .Replace("{exposed-ports}", $"EXPOSE {_port}\r\nEXPOSE 443");
                dockerFile = dockerFile
                    .Replace("{http-port-env-variable}", string.Empty);
            }
            // For all other ports, it is up to the user to expose the HTTPS port in the dockerfile. 
            else
            {
                dockerFile = dockerFile
                    .Replace("{exposed-ports}", $"EXPOSE {_port}");
                dockerFile = dockerFile
                    .Replace("{http-port-env-variable}", $"\r\nENV {_httpPortEnvironmentVariable}");
            }

            if (_useRootUser)
            {
                dockerFile = dockerFile
                    .Replace("{non-root-user}", string.Empty);
            }
            else
            {
                dockerFile = dockerFile
                    .Replace("{non-root-user}", "\r\nUSER app");
            }

            // ProjectDefinitionParser will have transformed projectDirectory to an absolute path, 
            // and DockerFileName is static so traversal should not be possible here.
            // nosemgrep: csharp.lang.security.filesystem.unsafe-path-combine.unsafe-path-combine
            File.WriteAllText(Path.Combine(projectDirectory, Constants.Docker.DefaultDockerfileName), dockerFile);
        }
    }
}
