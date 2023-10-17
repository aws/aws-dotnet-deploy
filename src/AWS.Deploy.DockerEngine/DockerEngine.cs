// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Utilities;
using Newtonsoft.Json;

namespace AWS.Deploy.DockerEngine
{
    public interface IDockerEngine
    {
        /// <summary>
        /// Generates a docker file
        /// </summary>
        /// <param name="recommendation">The currently selected recommendation</param>
        void GenerateDockerFile(Recommendation recommendation);

        /// <summary>
        /// Inspects the Dockerfile associated with the recommendation
        /// and determines the appropriate Docker Execution Directory,
        /// if one is not set.
        /// </summary>
        /// <param name="recommendation">The currently selected recommendation</param>
        void DetermineDockerExecutionDirectory(Recommendation recommendation);

        /// <summary>
        /// Determines the appropriate HTTP port that the underlying container is using.
        /// </summary>
        /// <param name="recommendation">The currently selected recommendation</param>
        /// <returns>The default HTTP port used by the container</returns>
        int DetermineDefaultDockerPort(Recommendation recommendation);
    }

    /// <summary>
    /// Orchestrates the moving parts involved in creating a dockerfile for a project
    /// </summary>
    public class DockerEngine : IDockerEngine
    {
        private readonly ProjectDefinition _project;
        private readonly IFileManager _fileManager;
        private readonly IDirectoryManager _directoryManager;
        private readonly string _projectPath;

        public DockerEngine(ProjectDefinition project, IFileManager fileManager, IDirectoryManager directoryManager)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project), "Cannot instantiate DockerEngine due to a null ProjectDefinition");
            }

            _project = project;
            _projectPath = project.ProjectPath;
            _fileManager = fileManager;
            _directoryManager = directoryManager;
        }

        /// <summary>
        /// Generates a docker file
        /// </summary>
        /// <param name="recommendation">The currently selected recommendation</param>
        public void GenerateDockerFile(Recommendation recommendation)
        {
            var projectFileName = Path.GetFileName(_projectPath);
            var imageMapping = GetImageMapping();
            if (imageMapping == null)
            {
                throw new UnknownDockerImageException(DeployToolErrorCode.NoValidDockerImageForProject, $"Unable to determine a valid docker base and build image for project of type {_project.SdkType} and Target Framework {_project.TargetFramework}");
            }

            var dockerFile = new DockerFile(
                imageMapping,
                projectFileName,
                _project.AssemblyName,
                recommendation.DeploymentBundle.DockerfileHttpPort,
                UseRootUser(recommendation),
                DetermineHTTPPortEnvironmentVariable(recommendation, recommendation.DeploymentBundle.DockerfileHttpPort));
            var projectDirectory = Path.GetDirectoryName(_projectPath) ?? "";
            var projectList = GetProjectList();
            dockerFile.WriteDockerFile(projectDirectory, projectList);
        }

        /// <summary>
        /// Retrieves a list of projects from a solution file
        /// </summary>
        private List<string>? GetProjectsFromSolutionFile(string solutionFile)
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
                .Where(x => x.EndsWith(".csproj") || x.EndsWith(".fsproj"))
                .Select(x => x.Replace('\\', Path.DirectorySeparatorChar))
                .ToList();

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
        private List<string>? GetProjectList()
        {
            var solutionDirectory = Directory.GetParent(_projectPath);

            // Climb upward from the csproj until we find a directory with one or more slns
            while (solutionDirectory != null)
            {
                var allSolutionFiles = solutionDirectory.GetFiles("*.sln");
                if (allSolutionFiles.Length > 0)
                {
                    foreach (var solutionFile in allSolutionFiles)
                    {
                        var projectList = GetProjectsFromSolutionFile(solutionFile.FullName);

                        if (projectList != null)
                        {
                            // Validate that all referenced projects are at or below the solution, otherwise we'd have to use
                            // a wider Docker execution directory that is potentially sending more than the project to the Docker daemon
                            foreach (var project in projectList)
                            {
                                var absoluteProjectPath = _directoryManager.GetAbsolutePath(solutionDirectory.FullName, project);

                                if (!_directoryManager.ExistsInsideDirectory(solutionDirectory.FullName, absoluteProjectPath))
                                {
                                    throw new DockerEngineException(DeployToolErrorCode.FailedToGenerateDockerFile,
                                        "Unable to generate a Dockerfile for this project becuase project reference(s) were detected above the solution (.sln) file. " +
                                        "Consider crafting your own Dockerfile or deploying to an AWS compute service that does not use Docker.");
                                }
                            }
                            return projectList;
                        }
                    }
                }

                solutionDirectory = solutionDirectory.Parent;
            }

            return null;
        }

        /// <summary>
        /// Gets image mapping specific to this project
        /// </summary>
        private ImageMapping GetImageMapping()
        {
            var content = ProjectUtilities.ReadDockerFileConfig();
            var definitions = JsonConvert.DeserializeObject<List<ImageDefinition>>(content);

            var sdkType = _project.SdkType;

            // For the Microsoft.NET.Sdk.Workersdk type we want to use the same container as Microsoft.NET.Sdk since a project
            // using Microsoft.NET.Sdk.Worker is still just a regular console application.
            if (string.Equals(sdkType, "Microsoft.NET.Sdk.Worker", StringComparison.OrdinalIgnoreCase))
            {
                sdkType = "Microsoft.NET.Sdk";
            }

            var mappings = definitions?.FirstOrDefault(x => x.SdkType.Equals(sdkType));
            if (mappings == null)
                throw new UnsupportedProjectException(DeployToolErrorCode.NoValidDockerMappingForSdkType, $"The project with SDK Type {_project.SdkType} is not supported.");

            return mappings.ImageMapping.FirstOrDefault(x => x.TargetFramework.Equals(_project.TargetFramework))
                ?? throw new UnsupportedProjectException(DeployToolErrorCode.NoValidDockerMappingForTargetFramework, $"The project with Target Framework {_project.TargetFramework} is not supported.");
        }

        /// <summary>
        /// Inspects the Dockerfile associated with the recommendation
        /// and determines the appropriate Docker Execution Directory,
        /// if one is not set.
        /// </summary>
        /// <param name="recommendation">The currently selected recommendation</param>
        public void DetermineDockerExecutionDirectory(Recommendation recommendation)
        {
            if (string.IsNullOrEmpty(recommendation.DeploymentBundle.DockerExecutionDirectory))
            {
                var projectFilename = Path.GetFileName(recommendation.ProjectPath);
                
                if (DockerUtilities.TryGetAbsoluteDockerfile(recommendation, _fileManager, _directoryManager, out var dockerFilePath))
                {
                    using (var stream = File.OpenRead(dockerFilePath))
                    using (var reader = new StreamReader(stream))
                    {
                        string? line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            var noSpaceLine = line.Replace(" ", "");

                            if (noSpaceLine.StartsWith("COPY") && (noSpaceLine.EndsWith(".sln./") || (projectFilename != null && noSpaceLine.Contains("/" + projectFilename))))
                            {
                                recommendation.DeploymentBundle.DockerExecutionDirectory = Path.GetDirectoryName(recommendation.ProjectDefinition.ProjectSolutionPath) ??  "";
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Determines the appropriate HTTP port that the underlying container is using.
        /// </summary>
        /// <param name="recommendation">The currently selected recommendation</param>
        /// <returns>The default HTTP port used by the container</returns>
        public int DetermineDefaultDockerPort(Recommendation recommendation)
        {
            switch (recommendation.ProjectDefinition.TargetFramework)
            {
                case "net7.0":
                case "net6.0":
                case "net5.0":
                case "netcoreapp3.1":
                    return 80;

                default:
                    return 8080;
            }
        }

        /// <summary>
        /// Determines the appropriate HTTP port environment variable to set.
        /// </summary>
        /// <param name="recommendation">The currently selected recommendation</param>
        /// <returns>The appropriate HTTP port environment variable</returns>
        private string DetermineHTTPPortEnvironmentVariable(Recommendation recommendation, int port)
        {
            switch (recommendation.ProjectDefinition.TargetFramework)
            {
                case "net7.0":
                case "net6.0":
                case "net5.0":
                case "netcoreapp3.1":
                    return $"ASPNETCORE_URLS=http://+:{port}";

                default:
                    return $"ASPNETCORE_HTTP_PORTS={port}";
            }
        }

        /// <summary>
        /// Checks whether to use a root or non-root user in the underlying container.
        /// </summary>
        /// <param name="recommendation">The currently selected recommendation</param>
        /// <returns>true if a root user is used, else false</returns>
        private bool UseRootUser(Recommendation recommendation)
        {
            switch (recommendation.ProjectDefinition.TargetFramework)
            {
                case "net7.0":
                case "net6.0":
                case "net5.0":
                case "netcoreapp3.1":
                    return true;

                default:
                    return false;
            }
        }
    }
}
