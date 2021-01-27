// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using AWS.Deploy.Common;

namespace AWS.Deploy.DockerEngine
{
    /// <summary>
    /// Orchestrates the moving parts involved in creating a dockerfile for a project
    /// </summary>
    public class DockerEngine
    {
        private readonly ProjectDefinition _project;
        private readonly string _projectPath;

        public DockerEngine(ProjectDefinition project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project), "Cannot instantiate DockerEngine due to a null ProjectDefinition");
            }

            _project = project;
            _projectPath = project.ProjectPath;
        }

        /// <summary>
        /// Generates a docker file
        /// </summary>
        public void GenerateDockerFile()
        {
            var projectFileName = Path.GetFileName(_projectPath);
            var imageMapping = GetImageMapping();
            if (imageMapping == null)
            {
                throw new UnknownDockerImageException($"Unable to determine a valid docker base and build image for project of type {_project.SdkType} and Target Framework {_project.TargetFramework}");
            }

            var dockerFile = new DockerFile(imageMapping, projectFileName, _project.AssemblyName);
            var projectDirectory = Path.GetDirectoryName(_projectPath);
            var projectList = GetProjectList();
            dockerFile.WriteDockerFile(projectDirectory, projectList);
        }

        /// <summary>
        /// Retrieves a list of projects from a solution file
        /// </summary>
        private List<string> GetProjectsFromSolutionFile(string solutionFile)
        {
            var projectFileName = Path.GetFileName(_projectPath);
            if (string.IsNullOrWhiteSpace(solutionFile) ||
                string.IsNullOrWhiteSpace(projectFileName))
            {
                return null;
            }

            List<string> lines = File.ReadAllLines(solutionFile).ToList();
            var projectLines = lines.Where(x => x.StartsWith("Project"));
            var projectPaths = projectLines
                .Select(x => x.Split(',')[1].Replace('\"', ' ').Trim())
                .Where(x => x.EndsWith(".csproj") || x.EndsWith(".fsproj")).ToList();

            //Validate project exists in solution
            if (projectPaths.Select(x => Path.GetFileName(x)).Where(x => x.Equals(projectFileName)).ToList().Count == 0)
            {
                return null;
            }

            return projectPaths;
        }

        /// <summary>
        /// Finds the project solution file (if one exists) and retrieves a list of projects that are part of one solution
        /// </summary>
        private List<string> GetProjectList()
        {
            var projectDirectory = Directory.GetParent(_projectPath);
            
            while (projectDirectory != null)
            {
                var files = projectDirectory.GetFiles("*.sln");
                if (files.Length > 0)
                {
                    foreach (var solutionFile in files)
                    {
                        var projectList = GetProjectsFromSolutionFile(solutionFile.FullName);
                        if (projectList != null)
                        {
                            return projectList;
                        }
                    }
                }

                projectDirectory = projectDirectory.Parent;
            }

            return null;
        }

        /// <summary>
        /// Gets image mapping specific to this project
        /// </summary>
        private ImageMapping GetImageMapping()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

            var content = ProjectUtilities.ReadDockerFileConfig();
            var definitions = JsonSerializer.Deserialize<List<ImageDefinition>>(content, options);
            var mappings = definitions.Where(x => x.SdkType.Equals(_project.SdkType)).FirstOrDefault();

            return mappings.ImageMapping.FirstOrDefault(x => x.TargetFramework.Equals(_project.TargetFramework));
        }
    }
}
