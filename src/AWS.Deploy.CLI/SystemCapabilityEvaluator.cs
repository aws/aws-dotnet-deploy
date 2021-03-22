// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.CDK;
using AWS.Deploy.Orchestration.Utilities;

namespace AWS.Deploy.CLI
{
    public interface ISystemCapabilityEvaluator
    {
        Task<SystemCapabilities> Evaluate();
    }

    internal class SystemCapabilityEvaluator : ISystemCapabilityEvaluator
    {
        private readonly ICommandLineWrapper _commandLineWrapper;
        private readonly CDKManager _cdkManager;

        public SystemCapabilityEvaluator(ICommandLineWrapper commandLineWrapper, CDKManager cdkManager)
        {
            _commandLineWrapper = commandLineWrapper;
            _cdkManager = cdkManager;
        }

        public async Task<SystemCapabilities> Evaluate()
        {
            var dockerTask = HasDockerInstalledAndRunning();
            var nodeTask = HasMinVersionNodeJs();

            var capabilities = new SystemCapabilities
            {
                DockerInfo = await dockerTask,
                NodeJsMinVersionInstalled = await nodeTask,
            };

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
                    containerType = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? proc.StandardOut.TrimEnd('\n') : "linux";
                });

            var dockerInfo = new DockerInfo
            {
                DockerInstalled = processExitCode == 0,
                DockerContainerType = containerType
            };
            return dockerInfo;
        }

        /// <summary>
        /// From https://docs.aws.amazon.com/cdk/latest/guide/work-with.html#work-with-prerequisites,
        /// min version is 10.3
        /// </summary>
        private async Task<bool> HasMinVersionNodeJs()
        {
            // run node --version to get the version
            var result = await _commandLineWrapper.TryRunWithResult("node --version");

            var versionString = result.StandardOut;

            if (versionString.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                versionString = versionString.Substring(1, versionString.Length - 1);

            if (!result.Success || !Version.TryParse(versionString, out var version))
                return false;

            return version.Major > 10 || version.Major == 10 && version.Minor >= 3;
        }
    }
}
