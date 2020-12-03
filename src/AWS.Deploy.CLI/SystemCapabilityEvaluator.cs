// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AWS.Deploy.Orchestrator;

namespace AWS.Deploy.CLI
{
    public interface ISystemCapabilityEvaluator
    {
        Task<SystemCapabilities> Evaluate();
    }

    internal class SystemCapabilityEvaluator : ISystemCapabilityEvaluator
    {
        public async Task<SystemCapabilities> Evaluate()
        {
            var dockerTask = HasDockerInstalled();
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

        private async Task<bool> HasDockerInstalled()
        {
            var (success, _) = await TryRunShellCommand("docker --version");

            return success;
        }

        /// <summary>
        /// From https://docs.aws.amazon.com/cdk/latest/guide/work-with.html#work-with-prerequisites,
        /// min version is 10.3
        /// </summary>
        private async Task<bool> HasMinVersionNodeJs()
        {
            // run node --version to get the version
            var (success, versionString) = await TryRunShellCommand("node --version");

            if (versionString.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                versionString = versionString.Substring(1, versionString.Length - 1);

            if (!success || !Version.TryParse(versionString, out var version))
                return false;

            return version.Major > 10 || version.Major == 10 && version.Minor >= 3;
        }

        private async Task<bool> HasCdkInstalled()
        {
            var (success, _) = await TryRunShellCommand("cdk --version");

            return success;
        }

        private async Task<(bool success, string output)> TryRunShellCommand(string command)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName =
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                        ? "cmd.exe"
                        : "/bin/sh",
                Arguments =
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? $"/c {command}"
                    : $"-c \"{command}\"",

                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var process = Process.Start(startInfo);

            if (null == process)
                throw new Exception($"Failed to start cmd to execute [{command}]");

            await process.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);

            return
            (
                success: (await process.StandardError.ReadToEndAsync()).Length == 0,
                output: await process.StandardOutput.ReadToEndAsync()
            );
        }
    }
}
