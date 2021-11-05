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
        private readonly ICommandLineWrapper _commandLineWrapper;
        private static readonly Version MinimumNodeJSVersion = new Version(10,13,0);

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
            if (selectedRecommendation.Recipe.DeploymentType == DeploymentTypes.CdkProject)
            {
                if (systemCapabilities.NodeJsVersion == null)
                {
                    capabilities.Add(new SystemCapability("NodeJS", false, false) {
                        InstallationUrl = "https://nodejs.org/en/download/",
                        Message = "The selected deployment uses the AWS CDK, which requires Node.js. The latest LTS version of Node.js is recommended and can be installed from https://nodejs.org/en/download/. Specifically, AWS CDK requires 10.13.0+ to work properly."
                    });
                }
                else if (systemCapabilities.NodeJsVersion < MinimumNodeJSVersion)
                {
                    capabilities.Add(new SystemCapability("NodeJS", false, false) {
                        InstallationUrl = "https://nodejs.org/en/download/",
                        Message = $"The selected deployment uses the AWS CDK, which requires version of Node.js higher than your current installation ({systemCapabilities.NodeJsVersion}). The latest LTS version of Node.js is recommended and can be installed from https://nodejs.org/en/download/. Specifically, AWS CDK requires 10.3+ to work properly."
                    });
                }
            }

            if (selectedRecommendation.Recipe.DeploymentBundle == DeploymentBundleTypes.Container)
            {
                if (!systemCapabilities.DockerInfo.DockerInstalled)
                {
                    capabilities.Add(new SystemCapability("Docker", false, false)
                    {
                        InstallationUrl = "https://docs.docker.com/engine/install/",
                        Message = "The selected deployment option requires Docker, which was not detected. Please install and start the appropriate version of Docker for you OS: https://docs.docker.com/engine/install/"
                    });
                }
                else if (!systemCapabilities.DockerInfo.DockerContainerType.Equals("linux", StringComparison.OrdinalIgnoreCase))
                {
                    capabilities.Add(new SystemCapability("Docker", true, false)
                    {
                        Message = "The deployment tool requires Docker to be running in linux mode. Please switch Docker to linux mode to continue."
                    });
                }
            }

            return capabilities;
        }
    }
}
