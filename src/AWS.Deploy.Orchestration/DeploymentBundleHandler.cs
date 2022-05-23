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
using AWS.Deploy.Common.Data;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Orchestration.Data;
using AWS.Deploy.Orchestration.Utilities;

namespace AWS.Deploy.Orchestration
{
    public interface IDeploymentBundleHandler
    {
        Task BuildDockerImage(CloudApplication cloudApplication, Recommendation recommendation, string imageTag);
        Task<string> CreateDotnetPublishZip(Recommendation recommendation);
        Task PushDockerImageToECR(Recommendation recommendation, string repositoryName, string sourceTag);
    }

    public class DeploymentBundleHandler : IDeploymentBundleHandler
    {
        private readonly ICommandLineWrapper _commandLineWrapper;
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IOrchestratorInteractiveService _interactiveService;
        private readonly IDirectoryManager _directoryManager;
        private readonly IZipFileManager _zipFileManager;

        public DeploymentBundleHandler(
            ICommandLineWrapper commandLineWrapper,
            IAWSResourceQueryer awsResourceQueryer,
            IOrchestratorInteractiveService interactiveService,
            IDirectoryManager directoryManager,
            IZipFileManager zipFileManager)
        {
            _commandLineWrapper = commandLineWrapper;
            _awsResourceQueryer = awsResourceQueryer;
            _interactiveService = interactiveService;
            _directoryManager = directoryManager;
            _zipFileManager = zipFileManager;
        }

        public async Task BuildDockerImage(CloudApplication cloudApplication, Recommendation recommendation, string imageTag)
        {
            _interactiveService.LogInfoMessage(string.Empty);
            _interactiveService.LogInfoMessage("Building the docker image...");

            var dockerExecutionDirectory = GetDockerExecutionDirectory(recommendation);
            var dockerFile = GetDockerFilePath(recommendation);
            var buildArgs = GetDockerBuildArgs(recommendation);

            var dockerBuildCommand = $"docker build -t {imageTag} -f \"{dockerFile}\"{buildArgs} .";
            _interactiveService.LogInfoMessage($"Docker Execution Directory: {Path.GetFullPath(dockerExecutionDirectory)}");
            _interactiveService.LogInfoMessage($"Docker Build Command: {dockerBuildCommand}");

            recommendation.DeploymentBundle.DockerExecutionDirectory = dockerExecutionDirectory;

            var result = await _commandLineWrapper.TryRunWithResult(dockerBuildCommand, dockerExecutionDirectory, streamOutputToInteractiveService: true);
            if (result.ExitCode != 0)
            {
                var errorMessage = "We were unable to build the docker image.";
                if (!string.IsNullOrEmpty(result.StandardError))
                    errorMessage = $"We were unable to build the docker image due to the following error:{Environment.NewLine}{result.StandardError}";

                errorMessage += $"{Environment.NewLine}Docker builds usually fail due to executing them from a working directory that is incompatible with the Dockerfile.";
                errorMessage += $"{Environment.NewLine}You can try setting the 'Docker Execution Directory' in the option settings.";
                throw new DockerBuildFailedException(DeployToolErrorCode.DockerBuildFailed, errorMessage, result.ExitCode);
            }
        }

        public async Task PushDockerImageToECR(Recommendation recommendation, string repositoryName, string sourceTag)
        {
            _interactiveService.LogInfoMessage(string.Empty);
            _interactiveService.LogInfoMessage("Pushing the docker image to ECR repository...");

            await InitiateDockerLogin();

            var tagSuffix = sourceTag.Split(":")[1];
            var repository = await SetupECRRepository(repositoryName);
            var targetTag = $"{repository.RepositoryUri}:{tagSuffix}";

            await TagDockerImage(sourceTag, targetTag);

            await PushDockerImage(targetTag);

            recommendation.DeploymentBundle.ECRRepositoryName = repository.RepositoryName;
            recommendation.DeploymentBundle.ECRImageTag = tagSuffix;
        }

        public async Task<string> CreateDotnetPublishZip(Recommendation recommendation)
        {
            _interactiveService.LogInfoMessage(string.Empty);
            _interactiveService.LogInfoMessage("Creating Dotnet Publish Zip file...");

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

            var result = await _commandLineWrapper.TryRunWithResult(publishCommand, streamOutputToInteractiveService: true);
            if (result.ExitCode != 0)
            {
                var errorMessage = "We were unable to package the application using 'dotnet publish'";
                if (!string.IsNullOrEmpty(result.StandardError))
                    errorMessage = $"We were unable to package the application using 'dotnet publish' due to the following error:{Environment.NewLine}{result.StandardError}";

                throw new DotnetPublishFailedException(DeployToolErrorCode.DotnetPublishFailed, errorMessage, result.ExitCode);
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
            var dockerFileDirectory = new FileInfo(recommendation.ProjectPath).Directory?.FullName;
            if (dockerFileDirectory == null)
                throw new InvalidProjectPathException(DeployToolErrorCode.ProjectPathNotFound, "The project path is invalid.");
            var projectSolutionPath = recommendation.ProjectDefinition.ProjectSolutionPath;

            if (string.IsNullOrEmpty(dockerExecutionDirectory))
            {
                if (string.IsNullOrEmpty(projectSolutionPath))
                {
                    dockerExecutionDirectory = new FileInfo(dockerFileDirectory).FullName;
                }
                else
                {
                    var projectSolutionDirectory = new FileInfo(projectSolutionPath).Directory?.FullName;
                    dockerExecutionDirectory = projectSolutionDirectory ?? throw new InvalidSolutionPathException(DeployToolErrorCode.InvalidSolutionPath, "The solution path is invalid.");
                }
            }

            return dockerExecutionDirectory;
        }

        private string GetDockerFilePath(Recommendation recommendation)
        {
            var dockerFileDirectory = new FileInfo(recommendation.ProjectPath).Directory?.FullName;
            if (dockerFileDirectory == null)
                throw new InvalidProjectPathException(DeployToolErrorCode.ProjectPathNotFound, "The project path is invalid.");

            return Path.Combine(dockerFileDirectory, "Dockerfile");
        }

        private string GetDockerBuildArgs(Recommendation recommendation)
        {
            var buildArgs = recommendation.DeploymentBundle.DockerBuildArgs;

            if (string.IsNullOrEmpty(buildArgs))
                return buildArgs;

            // Ensure it starts with a space so it doesn't collide with the previous option
            if (!char.IsWhiteSpace(buildArgs[0]))
                return $" {buildArgs}";
            else
                return buildArgs;
        }

        private async Task InitiateDockerLogin()
        {
            var authorizationTokens = await _awsResourceQueryer.GetECRAuthorizationToken();

            if (authorizationTokens.Count == 0)
                throw new DockerLoginFailedException(DeployToolErrorCode.FailedToGetECRAuthorizationToken, "Failed to login to Docker", null);

            var authTokenBytes = Convert.FromBase64String(authorizationTokens[0].AuthorizationToken);
            var authToken = Encoding.UTF8.GetString(authTokenBytes);
            var decodedTokens = authToken.Split(':');

            var dockerLoginCommand = $"docker login --username {decodedTokens[0]} --password {decodedTokens[1]} {authorizationTokens[0].ProxyEndpoint}";
            var result = await _commandLineWrapper.TryRunWithResult(dockerLoginCommand, streamOutputToInteractiveService: true);

            if (result.ExitCode != 0)
            {
                var errorMessage = "Failed to login to Docker";
                if (!string.IsNullOrEmpty(result.StandardError))
                    errorMessage = $"Failed to login to Docker due to the following reason:{Environment.NewLine}{result.StandardError}";
                throw new DockerLoginFailedException(DeployToolErrorCode.DockerLoginFailed, errorMessage, result.ExitCode);
            }
        }

        private async Task<Repository> SetupECRRepository(string ecrRepositoryName)
        {
            var existingRepositories = await _awsResourceQueryer.GetECRRepositories(new List<string> { ecrRepositoryName });

            if (existingRepositories.Count == 1)
            {
                return existingRepositories[0];
            }
            else
            {
                return await _awsResourceQueryer.CreateECRRepository(ecrRepositoryName);
            }
        }

        private async Task TagDockerImage(string sourceTagName, string targetTagName)
        {
            var dockerTagCommand = $"docker tag {sourceTagName} {targetTagName}";
            var result = await _commandLineWrapper.TryRunWithResult(dockerTagCommand, streamOutputToInteractiveService: true);

            if (result.ExitCode != 0)
            {
                var errorMessage = "Failed to tag Docker image";
                if (!string.IsNullOrEmpty(result.StandardError))
                    errorMessage = $"Failed to tag Docker Image due to the following reason:{Environment.NewLine}{result.StandardError}";
                throw new DockerTagFailedException(DeployToolErrorCode.DockerTagFailed, errorMessage, result.ExitCode);
            }
        }

        private async Task PushDockerImage(string targetTagName)
        {
            var dockerPushCommand = $"docker push {targetTagName}";
            var result = await _commandLineWrapper.TryRunWithResult(dockerPushCommand, streamOutputToInteractiveService: true);

            if (result.ExitCode != 0)
            {
                var errorMessage = "Failed to push Docker Image";
                if (!string.IsNullOrEmpty(result.StandardError))
                    errorMessage = $"Failed to push Docker Image due to the following reason:{Environment.NewLine}{result.StandardError}";
                throw new DockerPushFailedException(DeployToolErrorCode.DockerPushFailed, errorMessage, result.ExitCode);
            }
        }
    }
}
