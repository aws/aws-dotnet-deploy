// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.CDK;
using AWS.Deploy.Orchestration.Utilities;
using AWS.Deploy.Shell;

namespace AWS.Deploy.CLI
{
    public interface ISystemCapabilityEvaluator
    {
        Task<SystemCapabilities> Evaluate();
    }

    internal class SystemCapabilityEvaluator : ISystemCapabilityEvaluator
    {
        private readonly ICommandRunner _commandRunner;

        public SystemCapabilityEvaluator(ICommandRunner commandRunner)
        {
            _commandRunner = commandRunner;
        }

        public async Task<SystemCapabilities> Evaluate()
        {
            var dockerTask = HasDockerInstalledAndRunning();
            var nodeTask = HasMinVersionNodeJs();

            var capabilities = new SystemCapabilities(await nodeTask, await dockerTask);

            return capabilities;
        }

        private async Task<DockerInfo> HasDockerInstalledAndRunning()
        {
            var processExitCode = -1;
            var containerType = "";
            var command = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "docker info -f \"{{.OSType}}\"" : "docker info";

            await _commandRunner.Run(
                command,
                streamOutputToInteractiveService: false,
                onComplete: proc =>
                {
                    processExitCode = proc.ExitCode;
                    containerType = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                        proc.StandardOut?.TrimEnd('\n') ??
                            throw new DockerInfoException("Failed to check if Docker is running in Windows or Linux container mode.") :
                        "linux";
                });

            var dockerInfo = new DockerInfo(processExitCode == 0, containerType);

            return dockerInfo;
        }

        /// <summary>
        /// From https://docs.aws.amazon.com/cdk/latest/guide/work-with.html#work-with-prerequisites,
        /// min version is 10.3
        /// </summary>
        private async Task<bool> HasMinVersionNodeJs()
        {
            // run node --version to get the version
            var result = await _commandRunner.TryRunWithResult("node --version");

            var versionString = result.StandardOut ?? "";

            if (versionString.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                versionString = versionString.Substring(1, versionString.Length - 1);

            if (!result.Success || !Version.TryParse(versionString, out var version))
                return false;

            return version.Major > 10 || version.Major == 10 && version.Minor >= 3;
        }
    }
}
