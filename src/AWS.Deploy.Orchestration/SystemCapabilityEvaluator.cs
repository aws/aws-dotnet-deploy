// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Orchestration.Utilities;

namespace AWS.Deploy.Orchestration
{
    public interface ISystemCapabilityEvaluator
    {
        /// <summary>
        /// Clears the cache of successful capability checks, to ensure
        /// that next time <see cref="EvaluateSystemCapabilities(Recommendation)"/>
        /// is called they will be evaluated again.
        /// </summary>
        void ClearCachedCapabilityChecks();

        Task<List<SystemCapability>> EvaluateSystemCapabilities(Recommendation selectedRecommendation);

        Task<ContainerAppInfo?> GetInstalledContainerAppInfo(Recommendation selectedRecommendation);
    }

    public class SystemCapabilityEvaluator(ICommandLineWrapper commandLineWrapper) : ISystemCapabilityEvaluator
    {
        private const string NODEJS_DEPENDENCY_NAME = "Node.js";
        private const string NODEJS_INSTALLATION_URL = "https://nodejs.org/en/download/";

        private const string DOCKER_DEPENDENCY_NAME = "Docker";
        private const string DOCKER_INSTALLATION_URL = "https://docs.docker.com/engine/install/";

        private const string PODMAN_DEPENDENCY_NAME = "Podman";
        private const string PODMAN_INSTALLATION_URL = "https://podman.io/docs/installation";

        private static readonly Version MinimumNodeJSVersion = new Version(18,0,0);

        /// <summary>
        /// How long to wait for the commands we run to determine if Node/Docker/etc. are installed to finish
        /// </summary>
        private const int CAPABILITY_EVALUATION_TIMEOUT_MS = 60000; // one minute

        /// <summary>
        /// How long to cache the results of a VALID Node/Docker/etc. check
        /// </summary>
        private static readonly TimeSpan DEPENDENCY_CACHE_INTERVAL = TimeSpan.FromHours(1);

        /// <summary>
        /// If we ran a successful Node evaluation, this is the timestamp until which that result
        /// is valid and we will skip subsequent evaluations
        /// </summary>
        private DateTime _nodeDependencyValidUntilUtc = DateTime.MinValue;

        /// <summary>
        /// If we ran a successful Docker evaluation, this is the timestamp until which that result
        /// is valid and we will skip subsequent evaluations
        /// </summary>
        private DateTime _dockerDependencyValidUntilUtc = DateTime.MinValue;

        private ContainerAppInfo? _installedContainerAppInfo;

        /// <summary>
        /// Attempt to determine whether Docker is running and its current OS type
        /// </summary>
        private async Task<ContainerAppInfo> HasDockerInstalledAndRunningAsync()
        {
            var processExitCode = -1;
            var containerType = "";
            var command = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "docker info -f \"{{.OSType}}\"" : "docker info";

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(CAPABILITY_EVALUATION_TIMEOUT_MS);

            try
            {
                await commandLineWrapper.Run(
                    command,
                    streamOutputToInteractiveService: false,
                    onComplete: proc =>
                    {
                        processExitCode = proc.ExitCode;
                        containerType = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                            proc.StandardOut?.TrimEnd('\n') ??
                                throw new DockerInfoException(DeployToolErrorCode.FailedToCheckDockerInfo, "Failed to check if Docker is running in Windows or Linux container mode.") :
                            "linux";
                    },
                    cancellationToken: cancellationTokenSource.Token);


                var dockerInfo = new ContainerAppInfo(DOCKER_DEPENDENCY_NAME, DOCKER_INSTALLATION_URL, processExitCode == 0, containerType);

                return dockerInfo;
            }
            catch (TaskCanceledException)
            {
                // If the check timed out, treat Docker as not installed
                return new ContainerAppInfo(DOCKER_DEPENDENCY_NAME, DOCKER_INSTALLATION_URL, false, "");
            }
        }

        /// <summary>
        /// Attempt to determine whether Podman is running and its current OS type
        /// </summary>
        private async Task<ContainerAppInfo> HasPodmanInstalledAndRunningAsync()
        {
            var processExitCode = -1;
            var containerType = "";
            var command = "podman info --format=json | jq -r '.host.os'";

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(CAPABILITY_EVALUATION_TIMEOUT_MS);

            try
            {
                await commandLineWrapper.Run(
                    command,
                    streamOutputToInteractiveService: false,
                    onComplete: proc =>
                    {
                        processExitCode = proc.ExitCode;
                        containerType = proc.StandardOut?.TrimEnd('\n') ??
                                        throw new DockerInfoException(DeployToolErrorCode.FailedToCheckDockerInfo, $"Failed to check if {PODMAN_DEPENDENCY_NAME} is running in Windows or Linux container mode.");
                    },
                    cancellationToken: cancellationTokenSource.Token);


                var dockerInfo = new ContainerAppInfo(PODMAN_DEPENDENCY_NAME, PODMAN_INSTALLATION_URL, processExitCode == 0, containerType);

                return dockerInfo;
            }
            catch (TaskCanceledException)
            {
                // If the check timed out, treat Docker as not installed
                return new ContainerAppInfo(PODMAN_DEPENDENCY_NAME, PODMAN_INSTALLATION_URL, false, "");
            }
        }

        /// <summary>
        /// Attempt to determine the installed Node.js version
        /// </summary>
        private async Task<NodeInfo> GetNodeJsVersionAsync()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(CAPABILITY_EVALUATION_TIMEOUT_MS);

            try
            {
                // run node --version to get the version
                var result = await commandLineWrapper.TryRunWithResult("node --version", cancellationToken: cancellationTokenSource.Token);

                var versionString = result.StandardOut ?? "";

                if (versionString.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                    versionString = versionString.Substring(1, versionString.Length - 1);

                if (!result.Success || !Version.TryParse(versionString, out var version))
                    return new NodeInfo(null);

                return new NodeInfo(version);

            }
            catch (TaskCanceledException)
            {
                // If the check timed out, treat Node as not installed
                return new NodeInfo(null);
            }
        }

        /// <summary>
        /// Checks if the system meets all the necessary requirements for deployment.
        /// </summary>
        public async Task<List<SystemCapability>> EvaluateSystemCapabilities(Recommendation selectedRecommendation)
        {
            var missingCapabilitiesForRecipe = new List<SystemCapability>();
            string? message;

            // We only need to check that Node is installed if the user is deploying a recipe that uses CDK
            if (selectedRecommendation.Recipe.DeploymentType == DeploymentTypes.CdkProject)
            {
                // If we haven't cached that NodeJS installation is valid, or the cache is expired
                if (DateTime.UtcNow >= _nodeDependencyValidUntilUtc)
                {
                    var nodeInfo = await GetNodeJsVersionAsync();

                    if (nodeInfo.NodeJsVersion == null)
                    {
                        message = $"Install Node.js {MinimumNodeJSVersion} or later and restart your IDE/Shell. The latest Node.js LTS version is recommended. This deployment option uses the AWS CDK, which requires Node.js.";

                        missingCapabilitiesForRecipe.Add(new SystemCapability(NODEJS_DEPENDENCY_NAME, message, NODEJS_INSTALLATION_URL));
                    }
                    else if (nodeInfo.NodeJsVersion < MinimumNodeJSVersion)
                    {
                        message = $"Install Node.js {MinimumNodeJSVersion} or later and restart your IDE/Shell. The latest Node.js LTS version is recommended. This deployment option uses the AWS CDK, which requires Node.js version higher than your current installation ({nodeInfo.NodeJsVersion}). ";

                        missingCapabilitiesForRecipe.Add(new SystemCapability(NODEJS_DEPENDENCY_NAME, message, NODEJS_INSTALLATION_URL));
                    }
                    else // It is valid, so update the cache interval
                    {
                        _nodeDependencyValidUntilUtc = DateTime.UtcNow.Add(DEPENDENCY_CACHE_INTERVAL);
                    }
                }
            }

            // We only need to check that Docker is installed if the user is deploying a recipe that uses Docker
            if (selectedRecommendation.Recipe.DeploymentBundle == DeploymentBundleTypes.Container)
            {
                if (DateTime.UtcNow >= _dockerDependencyValidUntilUtc)
                {
                    var dockerTask = HasDockerInstalledAndRunningAsync();
                    var podmanTask = HasPodmanInstalledAndRunningAsync();

                    await Task.WhenAll(dockerTask, podmanTask);

                    var dockerInfo = await dockerTask;
                    var podmanInfo = await podmanTask;

                    if (!dockerInfo.IsInstalled && !podmanInfo.IsInstalled)
                    {
                        message = "Install and start Docker version appropriate for your OS. This deployment option requires Docker, which was not detected.";
                        missingCapabilitiesForRecipe.Add(new SystemCapability(DOCKER_DEPENDENCY_NAME, message, DOCKER_INSTALLATION_URL));
                    }
                    else if (dockerInfo.IsInstalled || podmanInfo.IsInstalled)
                    {
                        _installedContainerAppInfo = dockerInfo.IsInstalled ? dockerInfo : podmanInfo;
                        if (!_installedContainerAppInfo.ContainerType.Equals("linux", StringComparison.OrdinalIgnoreCase))
                        {
                            message = $"This is Linux-based deployment. Switch your {_installedContainerAppInfo.AppName} from Windows to Linux container mode.";
                            missingCapabilitiesForRecipe.Add(new SystemCapability(_installedContainerAppInfo.AppName, message));
                        }
                        else // It is valid, so update the cache interval
                        {
                            _dockerDependencyValidUntilUtc = DateTime.UtcNow.Add(DEPENDENCY_CACHE_INTERVAL);
                        }
                    }
                }
            }

            return missingCapabilitiesForRecipe;
        }

        public async Task<ContainerAppInfo?> GetInstalledContainerAppInfo(Recommendation selectedRecommendation)
        {
            if (_installedContainerAppInfo is null)
                await EvaluateSystemCapabilities(selectedRecommendation);
            return _installedContainerAppInfo;
        }

        public void ClearCachedCapabilityChecks()
        {
            _nodeDependencyValidUntilUtc = DateTime.MinValue;
            _dockerDependencyValidUntilUtc = DateTime.MinValue;
        }
    }
}
