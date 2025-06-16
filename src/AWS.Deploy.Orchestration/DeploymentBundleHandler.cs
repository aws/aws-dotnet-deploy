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
        Task BuildContainerImage(CloudApplication cloudApplication, Recommendation recommendation, string imageTag);
        Task<string> CreateDotnetPublishZip(Recommendation recommendation);
        Task PushContainerImageToECR(Recommendation recommendation, string repositoryName, string sourceTag);

        /// <summary>
        /// Inspects the already built container image
        /// to return the environment variables used in the container.
        /// </summary>
        /// <param name="recommendation">The currently selected recommendation</param>
        /// <param name="sourceTag">The tag of the built image</param>
        /// <returns>A dictionary that represents the environment variables from the container</returns>
        Task<Dictionary<string, string>> InspectContainerImageEnvironmentVariables(Recommendation recommendation, string sourceTag);
    }

    public class DeploymentBundleHandler(
        ICommandLineWrapper commandLineWrapper,
        IAWSResourceQueryer awsResourceQueryer,
        IOrchestratorInteractiveService interactiveService,
        IDirectoryManager directoryManager,
        IZipFileManager zipFileManager,
        IFileManager fileManager,
        IOptionSettingHandler optionSettingHandler,
        ISystemCapabilityEvaluator systemCapabilityEvaluator)
        : IDeploymentBundleHandler
    {
        public async Task BuildContainerImage(CloudApplication cloudApplication, Recommendation recommendation, string imageTag)
        {
            interactiveService.LogInfoMessage(string.Empty);
            interactiveService.LogInfoMessage("Building the container image...");

            var installedContainerAppInfo = await systemCapabilityEvaluator.GetInstalledContainerAppInfo(recommendation);
            var commandName = installedContainerAppInfo?.AppName?.ToLower();
            if (string.IsNullOrEmpty(commandName))
                throw new ContainerBuildFailedException(DeployToolErrorCode.ContainerBuildFailed, "No container app (Docker or Podman) is currently installed/running on your system.", -1);

            var containerExecutionDirectory = GetContainerExecutionDirectory(recommendation);
            var buildArgs = GetContainerBuildArgs(recommendation);
            DockerUtilities.TryGetAbsoluteDockerfile(recommendation, fileManager, directoryManager, out var dockerFile);

            var buildCommand = $"{commandName} build -t {imageTag} -f \"{dockerFile}\"{buildArgs} .";
            var currentArchitecture = RuntimeInformation.OSArchitecture == Architecture.Arm64 ? SupportedArchitecture.Arm64 : SupportedArchitecture.X86_64;
            if (currentArchitecture != recommendation.DeploymentBundle.EnvironmentArchitecture)
            {
                var platform = recommendation.DeploymentBundle.EnvironmentArchitecture == SupportedArchitecture.Arm64 ? "linux/arm64" : "linux/amd64";
                buildCommand = $"{commandName} buildx build --platform {platform} -t {imageTag} -f \"{dockerFile}\"{buildArgs} .";
            }

            interactiveService.LogInfoMessage($"Container Execution Directory: {Path.GetFullPath(containerExecutionDirectory)}");
            interactiveService.LogInfoMessage($"Container Build Command: {buildCommand}");

            recommendation.DeploymentBundle.DockerfilePath = dockerFile;
            recommendation.DeploymentBundle.DockerExecutionDirectory = containerExecutionDirectory;

            var result = await commandLineWrapper.TryRunWithResult(buildCommand, containerExecutionDirectory, streamOutputToInteractiveService: true);
            if (result.ExitCode != 0)
            {
                var errorMessage = "We were unable to build the container image.";
                if (!string.IsNullOrEmpty(result.StandardError))
                    errorMessage = $"We were unable to build the container image due to the following error:{Environment.NewLine}{result.StandardError}";

                errorMessage += $"{Environment.NewLine}Container builds usually fail due to executing them from a working directory that is incompatible with the Dockerfile.";
                errorMessage += $"{Environment.NewLine}You can try setting the 'Docker Execution Directory' in the option settings.";
                throw new ContainerBuildFailedException(DeployToolErrorCode.ContainerBuildFailed, errorMessage, result.ExitCode);
            }
        }

        public async Task PushContainerImageToECR(Recommendation recommendation, string repositoryName, string sourceTag)
        {
            interactiveService.LogInfoMessage(string.Empty);
            interactiveService.LogInfoMessage("Pushing the container image to ECR repository...");

            var installedContainerAppInfo = await systemCapabilityEvaluator.GetInstalledContainerAppInfo(recommendation);
            var commandName = installedContainerAppInfo?.AppName?.ToLower();
            if (string.IsNullOrEmpty(commandName))
                throw new ContainerBuildFailedException(DeployToolErrorCode.ContainerBuildFailed, "No container app (Docker or Podman) is currently installed/running on your system.", -1);

            await InitiateContainerLogin(commandName);

            var tagSuffix = sourceTag.Split(":")[1];
            var repository = await SetupECRRepository(repositoryName, recommendation.Recipe.Id);
            var targetTag = $"{repository.RepositoryUri}:{tagSuffix}";

            await TagContainerImage(commandName, sourceTag, targetTag);

            await PushContainerImage(commandName, targetTag);

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

                // Elastic Beanstalk currently has .NET 8 and 9 supported on their platforms.
                var supportedFrameworks = new List<string> { "net8.0", "net9.0" };
                var retiredFrameworks = new List<string> { "netcoreapp3.1", "net5.0", "net6.0", "net7.0" };
                if (!supportedFrameworks.Contains(targetFramework))
                {
                    if (retiredFrameworks.Contains(targetFramework))
                    {
                        interactiveService.LogErrorMessage($"The version of .NET that you are targeting has reached its end-of-support and has been retired by Elastic Beanstalk");
                        interactiveService.LogInfoMessage($"Using self-contained publish to include the out of support version of .NET used by this application with the deployment bundle");
                    }
                    else
                    {
                        interactiveService.LogInfoMessage($"Using self-contained publish since AWS Elastic Beanstalk does not currently have {targetFramework} preinstalled");
                    }
                    recommendation.DeploymentBundle.DotnetPublishSelfContainedBuild = true;
                    return;
                }

                var beanstalkPlatformSetting = recommendation.Recipe.OptionSettings.FirstOrDefault(x => x.Id.Equals("ElasticBeanstalkPlatformArn"));
                if (beanstalkPlatformSetting != null)
                {
                    var beanstalkPlatformSettingValue = optionSettingHandler.GetOptionSettingValue<string>(recommendation, beanstalkPlatformSetting);
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
                                interactiveService.LogInfoMessage($"Using self-contained publish since AWS Elastic Beanstalk does not currently have .NET 8 preinstalled on {beanstalkPlatformName} ({beanstalkPlatformVersion.ToString()})");
                                recommendation.DeploymentBundle.DotnetPublishSelfContainedBuild = true;
                                return;
                            }
                        }
                        else if (!beanstalkPlatformName.Contains(".NET 8"))
                        {
                            interactiveService.LogInfoMessage($"Using self-contained publish since AWS Elastic Beanstalk does not currently have .NET 8 preinstalled on {beanstalkPlatformName} ({beanstalkPlatformVersion.ToString()})");
                            recommendation.DeploymentBundle.DotnetPublishSelfContainedBuild = true;
                            return;
                        }
                    }
                }
            }
        }

        public async Task<string> CreateDotnetPublishZip(Recommendation recommendation)
        {
            interactiveService.LogInfoMessage(string.Empty);
            interactiveService.LogInfoMessage("Creating Dotnet Publish Zip file...");

            SwitchToSelfContainedBuildIfNeeded(recommendation);

            var publishDirectoryInfo = directoryManager.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
            var additionalArguments = recommendation.DeploymentBundle.DotnetPublishAdditionalBuildArguments;
            var windowsPlatform = recommendation.DeploymentBundle.EnvironmentArchitecture == SupportedArchitecture.Arm64 ? "win-arm64" : "win-x64";
            var linuxPlatform = recommendation.DeploymentBundle.EnvironmentArchitecture == SupportedArchitecture.Arm64 ? "linux-arm64" : "linux-x64";
            var runtimeArg =
               recommendation.DeploymentBundle.DotnetPublishSelfContainedBuild &&
               !additionalArguments.Contains("--runtime ") &&
               !additionalArguments.Contains("-r ")
                     ? $"--runtime {(recommendation.Recipe.TargetPlatform == TargetPlatform.Windows ? windowsPlatform : linuxPlatform)}"
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

            var result = await commandLineWrapper.TryRunWithResult(publishCommand, streamOutputToInteractiveService: true);
            if (result.ExitCode != 0)
            {
                var errorMessage = "We were unable to package the application using 'dotnet publish'";
                if (!string.IsNullOrEmpty(result.StandardError))
                    errorMessage = $"We were unable to package the application using 'dotnet publish' due to the following error:{Environment.NewLine}{result.StandardError}";

                throw new DotnetPublishFailedException(DeployToolErrorCode.DotnetPublishFailed, errorMessage, result.ExitCode);
            }

            var zipFilePath = $"{publishDirectoryInfo.FullName}.zip";
            await zipFileManager.CreateFromDirectory(publishDirectoryInfo.FullName, zipFilePath);

            recommendation.DeploymentBundle.DotnetPublishZipPath = zipFilePath;
            recommendation.DeploymentBundle.DotnetPublishOutputDirectory = publishDirectoryInfo.FullName;

            return zipFilePath;
        }

        /// <summary>
        /// Determines the appropriate container execution directory for the project.
        /// In order of precedence:
        /// 1. DeploymentBundle.DockerExecutionDirectory, if already set
        /// 2. The solution level if ProjectDefinition.ProjectSolutionPath is set
        /// 3. The project directory
        /// </summary>
        /// <param name="recommendation"></param>
        private string GetContainerExecutionDirectory(Recommendation recommendation)
        {
            var containerExecutionDirectory = recommendation.DeploymentBundle.DockerExecutionDirectory;
            var projectDirectory = recommendation.GetProjectDirectory();
            var projectSolutionPath = recommendation.ProjectDefinition.ProjectSolutionPath;

            if (string.IsNullOrEmpty(containerExecutionDirectory))
            {
                if (string.IsNullOrEmpty(projectSolutionPath))
                {
                    containerExecutionDirectory = new FileInfo(projectDirectory).FullName;
                }
                else
                {
                    var projectSolutionDirectory = new FileInfo(projectSolutionPath).Directory?.FullName;
                    containerExecutionDirectory = projectSolutionDirectory ?? throw new InvalidSolutionPathException(DeployToolErrorCode.InvalidSolutionPath, "The solution path is invalid.");
                }
            }

            // The docker build command will fail if a relative path is provided
            containerExecutionDirectory = directoryManager.GetAbsolutePath(projectDirectory, containerExecutionDirectory);
            return containerExecutionDirectory;
        }

        private string GetContainerBuildArgs(Recommendation recommendation)
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

        private async Task InitiateContainerLogin(string commandName)
        {
            var authorizationTokens = await awsResourceQueryer.GetECRAuthorizationToken();

            if (authorizationTokens.Count == 0)
                throw new DockerLoginFailedException(DeployToolErrorCode.FailedToGetECRAuthorizationToken, "Failed to login to Docker", null);

            var authTokenBytes = Convert.FromBase64String(authorizationTokens[0].AuthorizationToken);
            var authToken = Encoding.UTF8.GetString(authTokenBytes);
            var decodedTokens = authToken.Split(':');

            var loginCommand = $"{commandName} login --username {decodedTokens[0]} --password-stdin {authorizationTokens[0].ProxyEndpoint}";
            var result = await commandLineWrapper.TryRunWithResult(loginCommand, streamOutputToInteractiveService: true, stdin: decodedTokens[1]);

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
            var existingRepositories = await awsResourceQueryer.GetECRRepositories(new List<string> { ecrRepositoryName }) ?? new List<Repository>();

            if (existingRepositories.Count == 1)
            {
                return existingRepositories[0];
            }
            else
            {
                return await awsResourceQueryer.CreateECRRepository(ecrRepositoryName, recipeId);
            }
        }

        private async Task TagContainerImage(string commandName, string sourceTagName, string targetTagName)
        {
            var tagCommand = $"{commandName} tag {sourceTagName} {targetTagName}";
            var result = await commandLineWrapper.TryRunWithResult(tagCommand, streamOutputToInteractiveService: true);

            if (result.ExitCode != 0)
            {
                var errorMessage = "Failed to tag container image";
                if (!string.IsNullOrEmpty(result.StandardError))
                    errorMessage = $"Failed to tag container Image due to the following reason:{Environment.NewLine}{result.StandardError}";
                throw new DockerTagFailedException(DeployToolErrorCode.ContainerTagFailed, errorMessage, result.ExitCode);
            }
        }

        private async Task PushContainerImage(string commandName, string targetTagName)
        {
            var pushCommand = $"{commandName} push {targetTagName}";
            var result = await commandLineWrapper.TryRunWithResult(pushCommand, streamOutputToInteractiveService: true);

            if (result.ExitCode != 0)
            {
                var errorMessage = "Failed to push container image";
                if (!string.IsNullOrEmpty(result.StandardError))
                    errorMessage = $"Failed to push container image due to the following reason:{Environment.NewLine}{result.StandardError}";
                throw new ContainerPushFailedException(DeployToolErrorCode.ContainerPushFailed, errorMessage, result.ExitCode);
            }
        }

        /// <summary>
        /// Inspects the already built container image
        /// to return the environment variables used in the container.
        /// </summary>
        /// <param name="recommendation">The currently selected recommendation</param>
        /// <param name="sourceTag">The tag of the built image</param>
        /// <returns>A dictionary that represents the environment variables from the container</returns>
        public async Task<Dictionary<string, string>> InspectContainerImageEnvironmentVariables(Recommendation recommendation, string sourceTag)
        {
            var installedContainerAppInfo = await systemCapabilityEvaluator.GetInstalledContainerAppInfo(recommendation);
            var commandName = installedContainerAppInfo?.AppName?.ToLower();
            if (string.IsNullOrEmpty(commandName))
                throw new ContainerBuildFailedException(DeployToolErrorCode.ContainerBuildFailed, "No container app (Docker or Podman) is currently installed/running on your system.", -1);

            var inspectCommand = $"{commandName} inspect --format \"{{{{ index (index .Config.Env) }}}}\" " + sourceTag;
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                inspectCommand = $"{commandName} inspect --format '{{{{ index (index .Config.Env) }}}}' " + sourceTag;
            var result = await commandLineWrapper.TryRunWithResult(inspectCommand, streamOutputToInteractiveService: false);

            if (result.ExitCode != 0)
            {
                var errorMessage = "Failed to inspect container image";
                if (!string.IsNullOrEmpty(result.StandardError))
                    errorMessage = $"Failed to inspect container image due to the following reason:{Environment.NewLine}{result.StandardError}";
                throw new ContainerInspectFailedException(DeployToolErrorCode.ContainerInspectFailed, errorMessage, result.ExitCode);
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
