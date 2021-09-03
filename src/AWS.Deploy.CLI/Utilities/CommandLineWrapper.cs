// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using AWS.Deploy.Common;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.Utilities;

namespace AWS.Deploy.CLI.Utilities
{
    public class CommandLineWrapper : ICommandLineWrapper
    {
        private readonly IOrchestratorInteractiveService _interactiveService;
        private readonly bool _useSeparateWindow;
        private Action<ProcessStartInfo>? _processStartInfoAction;

        public CommandLineWrapper(
            IOrchestratorInteractiveService interactiveService)
        {
            _interactiveService = interactiveService;
        }

        public CommandLineWrapper(
            IOrchestratorInteractiveService interactiveService,
            bool useSeparateWindow)
            : this(interactiveService)
        {
            _useSeparateWindow = useSeparateWindow;
        }

        /// <inheritdoc />
        public async Task Run(
            string command,
            string workingDirectory = "",
            bool streamOutputToInteractiveService = true,
            Action<TryRunResult>? onComplete = null,
            bool redirectIO = true,
            IDictionary<string, string>? environmentVariables = null,
            CancellationToken cancelToken = default,
            bool needAwsCredentials = false)
        {
            StringBuilder strOutput = new StringBuilder();
            StringBuilder strError = new StringBuilder();

            var processStartInfo = new ProcessStartInfo
            {
                FileName = GetSystemShell(),

                Arguments =
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                        ? $"/c {command}"
                        : $"-c \"{command}\"",

                RedirectStandardInput = redirectIO,
                RedirectStandardOutput = redirectIO,
                RedirectStandardError = redirectIO,
                UseShellExecute = false,
                CreateNoWindow = redirectIO,
                WorkingDirectory = workingDirectory
            };

            // If the command output is not being redirected check to see if
            // the output should go to a separate console window. This is important when run from
            // an IDE which won't have a console window by default.
            if (!streamOutputToInteractiveService && !redirectIO)
            {
                processStartInfo.UseShellExecute = _useSeparateWindow;
            }

            UpdateEnvironmentVariables(processStartInfo, environmentVariables);

            if (needAwsCredentials)
                _processStartInfoAction?.Invoke(processStartInfo);

            var process = Process.Start(processStartInfo);
            if (null == process)
                throw new Exception("Process.Start failed to return a non-null process");

            if (redirectIO)
            {
                process.OutputDataReceived += (sender, e) =>
                {
                    if(streamOutputToInteractiveService)
                    {
                        _interactiveService.LogMessageLine(e.Data);
                    }

                    strOutput.Append(e.Data);
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if(streamOutputToInteractiveService)
                    {
                        _interactiveService.LogMessageLine(e.Data);
                    }

                    strError.Append(e.Data);
                };
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
                var result = new TryRunResult
                {
                    ExitCode = process.ExitCode
                };

                if (redirectIO)
                {
                    result.StandardError = strError.ToString();
                    result.StandardOut = strOutput.ToString();
                }

                onComplete(result);
            }
        }

        private static void UpdateEnvironmentVariables(ProcessStartInfo processStartInfo, IDictionary<string, string>? environmentVariables)
        {
            if (environmentVariables == null)
            {
                return;
            }

            foreach (var (key, value) in environmentVariables)
            {
                if (key == EnvironmentVariableKeys.AWS_EXECUTION_ENV)
                {
                    var awsExecutionEnvValue = BuildAWSExecutionEnvValue(processStartInfo, value);
                    processStartInfo.EnvironmentVariables[key] = awsExecutionEnvValue;
                }
                else
                {
                    processStartInfo.EnvironmentVariables[key] = value;
                }
            }
        }

        private static string BuildAWSExecutionEnvValue(ProcessStartInfo processStartInfo, string awsExecutionEnv)
        {
            var awsExecutionEnvBuilder = new StringBuilder();
            if (processStartInfo.EnvironmentVariables.ContainsKey(EnvironmentVariableKeys.AWS_EXECUTION_ENV)
                && !string.IsNullOrEmpty(processStartInfo.EnvironmentVariables[EnvironmentVariableKeys.AWS_EXECUTION_ENV]))
            {
                awsExecutionEnvBuilder.Append(processStartInfo.EnvironmentVariables[EnvironmentVariableKeys.AWS_EXECUTION_ENV]);
            }

            if (!string.IsNullOrEmpty(awsExecutionEnv))
            {
                if (awsExecutionEnvBuilder.Length != 0)
                {
                    awsExecutionEnvBuilder.Append("_");
                }

                awsExecutionEnvBuilder.Append(awsExecutionEnv);
            }

            return awsExecutionEnvBuilder.ToString();
        }

        public void ConfigureProcess(Action<ProcessStartInfo>? processStartInfoAction)
        {
            _processStartInfoAction = processStartInfoAction;
        }

        private string GetSystemShell()
        {
            if (TryGetEnvironmentVariable("COMSPEC", out var comspec))
                return comspec!;

            if (TryGetEnvironmentVariable("SHELL", out var shell))
                return shell!;

            // fall back to defaults
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "cmd.exe"
                : "/bin/sh";
        }

        private bool TryGetEnvironmentVariable(string variable, out string? value)
        {
            value = Environment.GetEnvironmentVariable(variable);

            return !string.IsNullOrEmpty(value);
        }
    }
}
