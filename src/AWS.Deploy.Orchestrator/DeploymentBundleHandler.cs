// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.ECR.Model;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Orchestrator.Data;
using AWS.Deploy.Orchestrator.Utilities;

namespace AWS.Deploy.Orchestrator
{
    public interface IDeploymentBundleHandler
    {
        Task<string> BuildDockerImage(CloudApplication cloudApplication, Recommendation recommendation);
        Task<string> CreateDotnetPublishZip(Recommendation recommendation);
        Task PushDockerImageToECR(CloudApplication cloudApplication, Recommendation recommendation, string sourceTag);
    }

    public class DeploymentBundleHandler : IDeploymentBundleHandler
    {
        private readonly ICommandLineWrapper _commandLineWrapper;
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IOrchestratorInteractiveService _interactiveService;
        private readonly OrchestratorSession _session;
        private readonly IDirectoryManager _directoryManager;
        private readonly IZipFileManager _zipFileManager;

        public DeploymentBundleHandler(
            OrchestratorSession session,
            ICommandLineWrapper commandLineWrapper,
            IAWSResourceQueryer awsResourceQueryer,
            IOrchestratorInteractiveService interactiveService,
            IDirectoryManager directoryManager,
            IZipFileManager zipFileManager)
        {
            _session = session;
            _commandLineWrapper = commandLineWrapper;
            _awsResourceQueryer = awsResourceQueryer;
            _interactiveService = interactiveService;
            _directoryManager = directoryManager;
            _zipFileManager = zipFileManager;
        }

        public async Task<string> BuildDockerImage(CloudApplication cloudApplication, Recommendation recommendation)
        {
            _interactiveService.LogMessageLine(string.Empty);
            _interactiveService.LogMessageLine("Building the docker image...");

            var dockerExecutionDirectory = GetDockerExecutionDirectory(recommendation);
            var tagSuffix = DateTime.UtcNow.Ticks;
            var imageTag = $"{cloudApplication.StackName.ToLower()}:{tagSuffix}";
            var dockerFile = GetDockerFilePath(recommendation);
            var buildArgs = GetDockerBuildArgs(recommendation);

            var dockerBuildCommand = $"docker build -t {imageTag} -f \"{dockerFile}\"{buildArgs} .";

            recommendation.DeploymentBundle.DockerExecutionDirectory = dockerExecutionDirectory;

            var result = await _commandLineWrapper.TryRunWithResult(dockerBuildCommand, dockerExecutionDirectory, redirectIO: false);
            if (result.ExitCode != 0)
            {
                throw new DockerBuildFailedException(result.StandardError);
            }

            return imageTag;
        }

        public async Task PushDockerImageToECR(CloudApplication cloudApplication, Recommendation recommendation, string sourceTag)
        {
            _interactiveService.LogMessageLine(string.Empty);
            _interactiveService.LogMessageLine("Pushing the docker image to ECR repository...");

            await InitiateDockerLogin();

            var tagSuffix = sourceTag.Split(":")[1];
            var repository = await SetupECRRepository(cloudApplication.StackName.ToLower());
            var targetTag = $"{repository.RepositoryUri}:{tagSuffix}";

            await TagDockerImage(sourceTag, targetTag);

            await PushDockerImage(targetTag);

            recommendation.DeploymentBundle.ECRRepositoryName = repository.RepositoryName;
            recommendation.DeploymentBundle.ECRImageTag = tagSuffix;
        }

        public async Task<string> CreateDotnetPublishZip(Recommendation recommendation)
        {
            _interactiveService.LogMessageLine(string.Empty);
            _interactiveService.LogMessageLine("Creating Dotnet Publish Zip file...");

            var publishDirectoryInfo = _directoryManager.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
            var additionalArguments = recommendation.DeploymentBundle.DotnetPublishAdditionalBuildArguments;
            var runtimeArg =
               recommendation.DeploymentBundle.DotnetPublishSelfContainedBuild &&
               !additionalArguments.Contains("--runtime ") &&
               !additionalArguments.Contains("-r ")
                     ? "--runtime linux-x64"
                     : "";
            var publishCommand =
                $"dotnet publish \"{recommendation.ProjectPath}\"" +
                $" -o \"{publishDirectoryInfo}\"" +
                $" -c {recommendation.DeploymentBundle.DotnetPublishBuildConfiguration}" +
                $" {runtimeArg}" +
                $" {additionalArguments}";

            // Blazor applications do not build with the default of setting self-contained to false.
            // So only add the --self-contained true if the user explicitly sets it to true.
            if(recommendation.DeploymentBundle.DotnetPublishSelfContainedBuild)
            {
                publishCommand += " --self-contained true";
            }

            var result = await _commandLineWrapper.TryRunWithResult(publishCommand, redirectIO: false);
            if (result.ExitCode != 0)
            {
                throw new DotnetPublishFailedException(result.StandardError);
            }

            var zipFilePath = $"{publishDirectoryInfo.FullName}.zip";
            await _zipFileManager.CreateFromDirectory(publishDirectoryInfo.FullName, zipFilePath);

            recommendation.DeploymentBundle.DotnetPublishZipPath = zipFilePath;
            recommendation.DeploymentBundle.DotnetPublishOutputDirectory = publishDirectoryInfo.FullName;

            return zipFilePath;
        }

        /// <summary>
        /// Determines the appropriate docker execution directory for the project.
        /// By default, the docker execution directory is at solution level.
        /// If no solution is available, the dockerfile directory is used.
        /// </summary>
        /// <param name="recommendation"></param>
        private string GetDockerExecutionDirectory(Recommendation recommendation)
        {
            var dockerExecutionDirectory = recommendation.DeploymentBundle.DockerExecutionDirectory;
            var dockerFileDirectory = new FileInfo(recommendation.ProjectPath).Directory.FullName;
            var projectSolutionPath = GetProjectSolutionFile(recommendation.ProjectPath);

            if (string.IsNullOrEmpty(dockerExecutionDirectory))
            {
                if (string.IsNullOrEmpty(projectSolutionPath))
                {
                    dockerExecutionDirectory = new FileInfo(dockerFileDirectory).FullName;
                }
                else
                {
                    dockerExecutionDirectory = new FileInfo(projectSolutionPath).Directory.FullName;
                }
            }

            return dockerExecutionDirectory;
        }

        private string GetDockerFilePath(Recommendation recommendation)
        {
            var dockerFileDirectory = new FileInfo(recommendation.ProjectPath).Directory.FullName;

            return Path.Combine(dockerFileDirectory, "Dockerfile");
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

            var lines = File.ReadAllLines(solutionFile);
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
            return projectPaths.Any(x => Path.GetFileName(x).Equals(projectFileName));
        }

        private string GetDockerBuildArgs(Recommendation recommendation)
        {
            var buildArgs = string.Empty;
            var argsDictionary = recommendation.DeploymentBundle.DockerBuildArgs
                .Split(',')
                .Where(x => x.Contains("="))
                .ToDictionary(
                    k => k.Split('=')[0],
                    v => v.Split('=')[1]
                );

            foreach (var arg in argsDictionary.Keys)
            {
                buildArgs += $" --build-arg {arg}={argsDictionary[arg]}";
            }

            return buildArgs;
        }

        private async Task InitiateDockerLogin()
        {
            var authorizationTokens = await _awsResourceQueryer.GetECRAuthorizationToken(_session);

            if (authorizationTokens.Count == 0)
                throw new DockerLoginFailedException();

            var authTokenBytes = Convert.FromBase64String(authorizationTokens[0].AuthorizationToken);
            var authToken = Encoding.UTF8.GetString(authTokenBytes);
            var decodedTokens = authToken.Split(':');

            var dockerLoginCommand = $"docker login --username {decodedTokens[0]} --password {decodedTokens[1]} {authorizationTokens[0].ProxyEndpoint}";
            var result = await _commandLineWrapper.TryRunWithResult(dockerLoginCommand);

            if (result.ExitCode != 0)
                throw new DockerLoginFailedException();
        }

        private async Task<Repository> SetupECRRepository(string ecrRepositoryName)
        {
            var existingRepositories = await _awsResourceQueryer.GetECRRepositories(_session, new List<string> { ecrRepositoryName });

            if (existingRepositories.Count == 1)
            {
                return existingRepositories[0];
            }
            else
            {
                return await _awsResourceQueryer.CreateECRRepository(_session, ecrRepositoryName);
            }
        }

        private async Task TagDockerImage(string sourceTagName, string targetTagName)
        {
            var dockerTagCommand = $"docker tag {sourceTagName} {targetTagName}";
            var result = await _commandLineWrapper.TryRunWithResult(dockerTagCommand);

            if (result.ExitCode != 0)
                throw new DockerTagFailedException();
        }

        private async Task PushDockerImage(string targetTagName)
        {
            var dockerPushCommand = $"docker push {targetTagName}";
            var result = await _commandLineWrapper.TryRunWithResult(dockerPushCommand, redirectIO: false);

            if (result.ExitCode != 0)
                throw new DockerPushFailedException();
        }
    }
}
