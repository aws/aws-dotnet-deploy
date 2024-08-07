// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
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
    }

    public class SystemCapabilityEvaluator : ISystemCapabilityEvaluator
    {
        private const string NODEJS_DEPENDENCY_NAME = "Node.js";
        private const string NODEJS_INSTALLATION_URL = "https://nodejs.org/en/download/";

        private const string DOCKER_DEPENDENCY_NAME = "Docker";
        private const string DOCKER_INSTALLATION_URL = "https://docs.docker.com/engine/install/";

        private readonly ICommandLineWrapper _commandLineWrapper;
        private static readonly Version MinimumNodeJSVersion = new Version(14,17,0);

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

        public SystemCapabilityEvaluator(ICommandLineWrapper commandLineWrapper)
        {
            _commandLineWrapper = commandLineWrapper;
        }

        /// <summary>
        /// Attempt to determine whether Docker is running and its current OS type
        /// </summary>
        private async Task<DockerInfo> HasDockerInstalledAndRunningAsync()
        {
            var processExitCode = -1;
            var containerType = "";
            var command = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "docker info -f \"{{.OSType}}\"" : "docker info";

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(CAPABILITY_EVALUATION_TIMEOUT_MS);

            try
            {
                await _commandLineWrapper.Run(
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


                var dockerInfo = new DockerInfo(processExitCode == 0, containerType);

                return dockerInfo;
            }
            catch (TaskCanceledException)
            {
                // If the check timed out, treat Docker as not installed
                return new DockerInfo(false, "");
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
                var result = await _commandLineWrapper.TryRunWithResult("node --version", cancellationToken: cancellationTokenSource.Token);

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
                    var dockerInfo = await HasDockerInstalledAndRunningAsync();

                    if (!dockerInfo.DockerInstalled)
                    {
                        message = "Install and start Docker version appropriate for your OS. This deployment option requires Docker, which was not detected.";
                        missingCapabilitiesForRecipe.Add(new SystemCapability(DOCKER_DEPENDENCY_NAME, message, DOCKER_INSTALLATION_URL));
                    }
                    else if (!dockerInfo.DockerContainerType.Equals("linux", StringComparison.OrdinalIgnoreCase))
                    {
                        message = "This is Linux-based deployment. Switch your Docker from Windows to Linux container mode.";
                        missingCapabilitiesForRecipe.Add(new SystemCapability(DOCKER_DEPENDENCY_NAME, message));
                    }
                    else // It is valid, so update the cache interval
                    {
                        _dockerDependencyValidUntilUtc = DateTime.UtcNow.Add(DEPENDENCY_CACHE_INTERVAL);
                    }
                }
            }

            return missingCapabilitiesForRecipe;
        }

        public void ClearCachedCapabilityChecks()
        {
            _nodeDependencyValidUntilUtc = DateTime.MinValue;
            _dockerDependencyValidUntilUtc = DateTime.MinValue;
        }
    }
}
