// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;
using AWS.Deploy.Orchestrator;
using AWS.Deploy.Orchestrator.Utilities;

namespace AWS.Deploy.CLI
{
    public interface ISystemCapabilityEvaluator
    {
        Task<SystemCapabilities> Evaluate();
    }

    internal class SystemCapabilityEvaluator : ISystemCapabilityEvaluator
    {
        private readonly ICommandLineWrapper _commandLineWrapper;

        public SystemCapabilityEvaluator(ICommandLineWrapper commandLineWrapper)
        {
            _commandLineWrapper = commandLineWrapper;
        }

        public async Task<SystemCapabilities> Evaluate()
        {
            var dockerTask = HasDockerInstalledAndRunning();
            await dockerTask;
            var nodeTask = HasMinVersionNodeJs();
            var cdkTask = HasCdkInstalled();

            var capabilities = new SystemCapabilities
            {
                DockerInstalled = await dockerTask,
                NodeJsMinVersionInstalled = await nodeTask,
                CdkNpmModuleInstalledGlobally = await cdkTask
            };

            return capabilities;
        }

        private async Task<bool> HasDockerInstalledAndRunning()
        {
            var processExitCode = -1;

            await _commandLineWrapper.Run(
                "docker info",
                streamOutputToInteractiveService: false,
                onComplete: proc =>
                {
                    processExitCode = proc.ExitCode;
                    return Task.CompletedTask;
                });

            return processExitCode == 0;
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

        private async Task<bool> HasCdkInstalled()
        {
            var result = await _commandLineWrapper.TryRunWithResult("cdk --version");

            return result.Success;
        }
    }
}
