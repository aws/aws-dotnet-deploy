// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using AWS.Deploy.Orchestrator;
using AWS.Deploy.Orchestrator.Utilities;

namespace AWS.Deploy.CLI.Utilities
{
    public class CommandLineWrapper : ICommandLineWrapper
    {
        private readonly IOrchestratorInteractiveService _interactiveService;
        private readonly AWSCredentials _awsCredentials;
        private readonly string _awsRegion;

        public CommandLineWrapper(
            IOrchestratorInteractiveService interactiveService,
            AWSCredentials awsCredentials,
            string awsRegion)
        {
            _interactiveService = interactiveService;
            _awsCredentials = awsCredentials;
            _awsRegion = awsRegion;
        }

        /// <inheritdoc />
        public async Task Run(
            string command,
            string workingDirectory = "",
            bool streamOutputToInteractiveService = true,
            Func<Process, Task> onComplete = null,
            CancellationToken cancelToken = default)
        {
            var credentials = await _awsCredentials.GetCredentialsAsync();

            var processStartInfo = new ProcessStartInfo
            {
                FileName = GetSystemShell(),

                Arguments =
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                        ? $"/c {command}"
                        : $"-c \"{command}\"",

                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            };

            // environment variables could already be set at the machine level,
            // use this syntax to make sure we don't create duplicate entries
            processStartInfo.EnvironmentVariables["AWS_ACCESS_KEY_ID"] = credentials.AccessKey;
            processStartInfo.EnvironmentVariables["AWS_SECRET_ACCESS_KEY"] = credentials.SecretKey;
            processStartInfo.EnvironmentVariables["AWS_REGION"] = _awsRegion;

            if (credentials.UseToken)
            {
                processStartInfo.EnvironmentVariables["AWS_SESSION_TOKEN"] = credentials.Token;
            }

            var process = Process.Start(processStartInfo);
            if (null == process)
                throw new Exception("Process.Start failed to return a non-null process");

            if (streamOutputToInteractiveService)
            {
                process.OutputDataReceived += (sender, e) => { _interactiveService.LogMessageLine(e.Data); };
                process.ErrorDataReceived += (sender, e) => { _interactiveService.LogMessageLine(e.Data); };
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }

            // poll for process to prevent blocking the main thread
            // as opposed to using process.WaitForExit()
            // in .net5 we can use process.WaitForExitAsync()
            while (true)
            {
                if (process.HasExited)
                    break;

                await Task.Delay(TimeSpan.FromMilliseconds(50), cancelToken);
            }

            if (onComplete != null)
            {
                await onComplete(process);
            }
        }

        public async Task<TryRunResult> TryRunWithResult(
            string command,
            string workingDirectory = "",
            bool streamOutputToInteractiveService = false,
            CancellationToken cancelToken = default)
        {
            var result = new TryRunResult();

            await Run(
                command,
                workingDirectory,
                streamOutputToInteractiveService: streamOutputToInteractiveService,
                onComplete:
                async process =>
                {
                    result.StandardError = await process.StandardError.ReadToEndAsync();
                    result.StandardOut = await process.StandardOutput.ReadToEndAsync();
                },
                cancelToken: cancelToken);

            return result;
        }

        private string GetSystemShell()
        {
            if (TryGetEnvironmentVariable("COMSPEC", out var comspec))
                return comspec;

            if (TryGetEnvironmentVariable("SHELL", out var shell))
                return shell;

            // fall back to defaults
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "cmd.exe"
                : "/bin/sh";
        }

        private bool TryGetEnvironmentVariable(string variable, out string value)
        {
            value = Environment.GetEnvironmentVariable(variable);

            return !string.IsNullOrEmpty(value);
        }
    }
}
