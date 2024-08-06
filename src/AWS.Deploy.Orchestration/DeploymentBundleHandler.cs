// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Amazon.ECR.Model;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Utilities;
using AWS.Deploy.Constants;
using AWS.Deploy.Orchestration.Utilities;
using Recommendation = AWS.Deploy.Common.Recommendation;

namespace AWS.Deploy.Orchestration
{
    public interface IDeploymentBundleHandler
    {
        Task BuildDockerImage(CloudApplication cloudApplication, Recommendation recommendation, string imageTag);
        Task<string> CreateDotnetPublishZip(Recommendation recommendation);
        Task PushDockerImageToECR(Recommendation recommendation, string repositoryName, string sourceTag);

        /// <summary>
        /// Inspects the already built docker image by using 'docker inspect'
        /// to return the environment variables used in the container.
        /// </summary>
        /// <param name="recommendation">The currently selected recommendation</param>
        /// <param name="sourceTag">The docker tag of the built image</param>
        /// <returns>A dictionary that represents the environment varibales from the container</returns>
        Task<Dictionary<string, string>> InspectDockerImageEnvironmentVariables(Recommendation recommendation, string sourceTag);
    }

    public class DeploymentBundleHandler : IDeploymentBundleHandler
    {
        private readonly ICommandLineWrapper _commandLineWrapper;
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IOrchestratorInteractiveService _interactiveService;
        private readonly IDirectoryManager _directoryManager;
        private readonly IZipFileManager _zipFileManager;
        private readonly IFileManager _fileManager;
        private readonly IOptionSettingHandler _optionSettingHandler;

        public DeploymentBundleHandler(
            ICommandLineWrapper commandLineWrapper,
            IAWSResourceQueryer awsResourceQueryer,
            IOrchestratorInteractiveService interactiveService,
            IDirectoryManager directoryManager,
            IZipFileManager zipFileManager,
            IFileManager fileManager,
            IOptionSettingHandler optionSettingHandler)
        {
            _commandLineWrapper = commandLineWrapper;
            _awsResourceQueryer = awsResourceQueryer;
            _interactiveService = interactiveService;
            _directoryManager = directoryManager;
            _zipFileManager = zipFileManager;
            _fileManager = fileManager;
            _optionSettingHandler = optionSettingHandler;
        }

        public async Task BuildDockerImage(CloudApplication cloudApplication, Recommendation recommendation, string imageTag)
        {
            _interactiveService.LogInfoMessage(string.Empty);
            _interactiveService.LogInfoMessage("Building the docker image...");

            var dockerExecutionDirectory = GetDockerExecutionDirectory(recommendation);
            var buildArgs = GetDockerBuildArgs(recommendation);
            DockerUtilities.TryGetAbsoluteDockerfile(recommendation, _fileManager, _directoryManager, out var dockerFile);

            var dockerBuildCommand = $"docker build -t {imageTag} -f \"{dockerFile}\"{buildArgs} .";
            _interactiveService.LogInfoMessage($"Docker Execution Directory: {Path.GetFullPath(dockerExecutionDirectory)}");
            _interactiveService.LogInfoMessage($"Docker Build Command: {dockerBuildCommand}");

            recommendation.DeploymentBundle.DockerfilePath = dockerFile;
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
            var repository = await SetupECRRepository(repositoryName, recommendation.Recipe.Id);
            var targetTag = $"{repository.RepositoryUri}:{tagSuffix}";

            await TagDockerImage(sourceTag, targetTag);

            await PushDockerImage(targetTag);

            recommendation.DeploymentBundle.ECRRepositoryName = repository.RepositoryName;
            recommendation.DeploymentBundle.ECRImageTag = tagSuffix;
        }

        /// <summary>
        /// The supported .NET versions on Elastic Beanstalk are dependent on the available platform versions.
        /// These versions do not always have the required .NET runtimes installed so we need to perform extra checks
        /// and perform a self-contained publish when creating the deployment bundle if needed.
        /// </summary>
        private void SwitchToSelfContainedBuildIfNeeded(Recommendation recommendation)
        {
            if (recommendation.Recipe.TargetService == RecipeIdentifier.TARGET_SERVICE_ELASTIC_BEANSTALK)
            {
                if (recommendation.DeploymentBundle.DotnetPublishSelfContainedBuild)
                    return;

                var targetFramework = recommendation.ProjectDefinition.TargetFramework ?? string.Empty;
                if (string.IsNullOrEmpty(targetFramework))
                    return;

                // Elastic Beanstalk doesn't currently have .NET 7 preinstalled.
                var unavailableFramework = new List<string> { "net7.0" };
                var frameworkNames = new Dictionary<string, string> { { "net7.0", ".NET 7" } };
                if (unavailableFramework.Contains(targetFramework))
                {
                    _interactiveService.LogInfoMessage($"Using self-contained publish since AWS Elastic Beanstalk does not currently have {frameworkNames[targetFramework]} preinstalled");
                    recommendation.DeploymentBundle.DotnetPublishSelfContainedBuild = true;
                    return;
                }

                var beanstalkPlatformSetting = recommendation.Recipe.OptionSettings.FirstOrDefault(x => x.Id.Equals("ElasticBeanstalkPlatformArn"));
                if (beanstalkPlatformSetting != null)
                {
                    var beanstalkPlatformSettingValue = _optionSettingHandler.GetOptionSettingValue<string>(recommendation, beanstalkPlatformSetting);
                    var beanstalkPlatformSettingValueSplit = beanstalkPlatformSettingValue?.Split("/");
                    if (beanstalkPlatformSettingValueSplit?.Length != 3)
                        // If the platform is not in the expected format, we will proceed normally to allow users to manually set the self-contained build to true.
                        return;
                    var beanstalkPlatformName = beanstalkPlatformSettingValueSplit[1];
                    if (!Version.TryParse(beanstalkPlatformSettingValueSplit[2], out var beanstalkPlatformVersion))
                        // If the platform is not in the expected format, we will proceed normally to allow users to manually set the self-contained build to true.
                        return;

                    // Elastic Beanstalk recently added .NET8 support in
                    // platform '.NET 8 on AL2023 version 3.1.1' and '.NET Core on AL2 version 2.8.0'.
                    // If users are using platform versions other than the above or older than '2.8.0' for '.NET Core'
                    // we need to perform a self-contained publish.
                    if (targetFramework.Equals("net8.0"))
                    {
                        if (beanstalkPlatformName.Contains(".NET Core"))
                        {
                            if (beanstalkPlatformVersion < new Version(2, 8, 0))
                            {
                                _interactiveService.LogInfoMessage($"Using self-contained publish since AWS Elastic Beanstalk does not currently have .NET 8 preinstalled on {beanstalkPlatformName} ({beanstalkPlatformVersion.ToString()})");
                                recommendation.DeploymentBundle.DotnetPublishSelfContainedBuild = true;
                                return;
                            }
                        }
                        else if (!beanstalkPlatformName.Contains(".NET 8"))
                        {
                            _interactiveService.LogInfoMessage($"Using self-contained publish since AWS Elastic Beanstalk does not currently have .NET 8 preinstalled on {beanstalkPlatformName} ({beanstalkPlatformVersion.ToString()})");
                            recommendation.DeploymentBundle.DotnetPublishSelfContainedBuild = true;
                            return;
                        }
                    }
                }
            }
        }

        public async Task<string> CreateDotnetPublishZip(Recommendation recommendation)
        {
            _interactiveService.LogInfoMessage(string.Empty);
            _interactiveService.LogInfoMessage("Creating Dotnet Publish Zip file...");

            SwitchToSelfContainedBuildIfNeeded(recommendation);

            var publishDirectoryInfo = _directoryManager.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
            var additionalArguments = recommendation.DeploymentBundle.DotnetPublishAdditionalBuildArguments;
            var runtimeArg =
               recommendation.DeploymentBundle.DotnetPublishSelfContainedBuild &&
               !additionalArguments.Contains("--runtime ") &&
               !additionalArguments.Contains("-r ")
                     ? $"--runtime {(recommendation.Recipe.TargetPlatform == TargetPlatform.Windows ? "win-x64" : "linux-x64")}"
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
        /// In order of precedence:
        /// 1. DeploymentBundle.DockerExecutionDirectory, if already set
        /// 2. The solution level if ProjectDefinition.ProjectSolutionPath is set
        /// 3. The project directory
        /// </summary>
        /// <param name="recommendation"></param>
        private string GetDockerExecutionDirectory(Recommendation recommendation)
        {
            var dockerExecutionDirectory = recommendation.DeploymentBundle.DockerExecutionDirectory;
            var projectDirectory = recommendation.GetProjectDirectory();
            var projectSolutionPath = recommendation.ProjectDefinition.ProjectSolutionPath;

            if (string.IsNullOrEmpty(dockerExecutionDirectory))
            {
                if (string.IsNullOrEmpty(projectSolutionPath))
                {
                    dockerExecutionDirectory = new FileInfo(projectDirectory).FullName;
                }
                else
                {
                    var projectSolutionDirectory = new FileInfo(projectSolutionPath).Directory?.FullName;
                    dockerExecutionDirectory = projectSolutionDirectory ?? throw new InvalidSolutionPathException(DeployToolErrorCode.InvalidSolutionPath, "The solution path is invalid.");
                }
            }

            // The docker build command will fail if a relative path is provided
            dockerExecutionDirectory = _directoryManager.GetAbsolutePath(projectDirectory, dockerExecutionDirectory);
            return dockerExecutionDirectory;
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

            var dockerLoginCommand = $"docker login --username {decodedTokens[0]} --password-stdin {authorizationTokens[0].ProxyEndpoint}";
            var result = await _commandLineWrapper.TryRunWithResult(dockerLoginCommand, streamOutputToInteractiveService: true, stdin: decodedTokens[1]);

            if (result.ExitCode != 0)
            {
                var errorMessage = "Failed to login to Docker";
                if (!string.IsNullOrEmpty(result.StandardError))
                    errorMessage = $"Failed to login to Docker due to the following reason:{Environment.NewLine}{result.StandardError}";
                throw new DockerLoginFailedException(DeployToolErrorCode.DockerLoginFailed, errorMessage, result.ExitCode);
            }
        }

        private async Task<Repository> SetupECRRepository(string ecrRepositoryName, string recipeId)
        {
            var existingRepositories = await _awsResourceQueryer.GetECRRepositories(new List<string> { ecrRepositoryName });

            if (existingRepositories.Count == 1)
            {
                return existingRepositories[0];
            }
            else
            {
                return await _awsResourceQueryer.CreateECRRepository(ecrRepositoryName, recipeId);
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

        /// <summary>
        /// Inspects the already built docker image by using 'docker inspect'
        /// to return the environment variables used in the container.
        /// </summary>
        /// <param name="recommendation">The currently selected recommendation</param>
        /// <param name="sourceTag">The docker tag of the built image</param>
        /// <returns>A dictionary that represents the environment varibales from the container</returns>
        public async Task<Dictionary<string, string>> InspectDockerImageEnvironmentVariables(Recommendation recommendation, string sourceTag)
        {
            var dockerInspectCommand = "docker inspect --format \"{{ index (index .Config.Env) }}\" " + sourceTag;
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                dockerInspectCommand = "docker inspect --format '{{ index (index .Config.Env) }}' " + sourceTag;
            var result = await _commandLineWrapper.TryRunWithResult(dockerInspectCommand, streamOutputToInteractiveService: false);

            if (result.ExitCode != 0)
            {
                var errorMessage = "Failed to inspect Docker Image";
                if (!string.IsNullOrEmpty(result.StandardError))
                    errorMessage = $"Failed to inspect Docker Image due to the following reason:{Environment.NewLine}{result.StandardError}";
                throw new DockerInspectFailedException(DeployToolErrorCode.DockerInspectFailed, errorMessage, result.ExitCode);
            }

            var environmentVariables = new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(result.StandardOut))
            {
                var lines = result.StandardOut.TrimStart('[').TrimEnd(']').Split(' ');
                foreach (var line in lines)
                {
                    var keyValuePair = line.Split('=');
                    if (keyValuePair.Length < 2)
                        continue;

                    environmentVariables[keyValuePair[0].Trim()] = keyValuePair[1].Trim();
                }
            }

            return environmentVariables;
        }
    }
}
