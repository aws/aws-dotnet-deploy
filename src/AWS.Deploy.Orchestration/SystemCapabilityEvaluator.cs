// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Orchestration.Utilities;

namespace AWS.Deploy.Orchestration
{
    public interface ISystemCapabilityEvaluator
    {
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

        public SystemCapabilityEvaluator(ICommandLineWrapper commandLineWrapper)
        {
            _commandLineWrapper = commandLineWrapper;
        }

        public async Task<SystemCapabilities> Evaluate()
        {
            var dockerTask = HasDockerInstalledAndRunning();
            var nodeTask = GetNodeJsVersion();

            var capabilities = new SystemCapabilities(await nodeTask, await dockerTask);

            return capabilities;
        }

        private async Task<DockerInfo> HasDockerInstalledAndRunning()
        {
            var processExitCode = -1;
            var containerType = "";
            var command = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "docker info -f \"{{.OSType}}\"" : "docker info";

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
                });

            var dockerInfo = new DockerInfo(processExitCode == 0, containerType);

            return dockerInfo;
        }

        /// <summary>
        /// From https://docs.aws.amazon.com/cdk/latest/guide/work-with.html#work-with-prerequisites,
        /// min version is 10.3
        /// </summary>
        private async Task<Version?> GetNodeJsVersion()
        {
            // run node --version to get the version
            var result = await _commandLineWrapper.TryRunWithResult("node --version");

            var versionString = result.StandardOut ?? "";

            if (versionString.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                versionString = versionString.Substring(1, versionString.Length - 1);

            if (!result.Success || !Version.TryParse(versionString, out var version))
                return null;

            return version;
        }

        /// <summary>
        /// Checks if the system meets all the necessary requirements for deployment.
        /// </summary>
        public async Task<List<SystemCapability>> EvaluateSystemCapabilities(Recommendation selectedRecommendation)
        {
            var capabilities = new List<SystemCapability>();
            var systemCapabilities = await Evaluate();
            string? message;
            if (selectedRecommendation.Recipe.DeploymentType == DeploymentTypes.CdkProject)
            {
                if (systemCapabilities.NodeJsVersion == null)
                {

                    message = $"Install Node.js {MinimumNodeJSVersion} or later and restart your IDE/Shell. The latest Node.js LTS version is recommended. This deployment option uses the AWS CDK, which requires Node.js.";

                    capabilities.Add(new SystemCapability(NODEJS_DEPENDENCY_NAME, message, NODEJS_INSTALLATION_URL));
                }
                else if (systemCapabilities.NodeJsVersion < MinimumNodeJSVersion)
                {
                    message = $"Install Node.js {MinimumNodeJSVersion} or later and restart your IDE/Shell. The latest Node.js LTS version is recommended. This deployment option uses the AWS CDK, which requires Node.js version higher than your current installation ({systemCapabilities.NodeJsVersion}). ";


                    capabilities.Add(new SystemCapability(NODEJS_DEPENDENCY_NAME, message, NODEJS_INSTALLATION_URL));
                }
            }

            if (selectedRecommendation.Recipe.DeploymentBundle == DeploymentBundleTypes.Container)
            {
                if (!systemCapabilities.DockerInfo.DockerInstalled)
                {
                    message = "Install and start Docker version appropriate for your OS. This deployment option requires Docker, which was not detected.";
                    capabilities.Add(new SystemCapability(DOCKER_DEPENDENCY_NAME, message, DOCKER_INSTALLATION_URL));
                }
                else if (!systemCapabilities.DockerInfo.DockerContainerType.Equals("linux", StringComparison.OrdinalIgnoreCase))
                {
                    message = "This is Linux-based deployment. Switch your Docker from Windows to Linux container mode.";
                    capabilities.Add(new SystemCapability(DOCKER_DEPENDENCY_NAME, message));
                }
            }

            return capabilities;
        }
    }
}
