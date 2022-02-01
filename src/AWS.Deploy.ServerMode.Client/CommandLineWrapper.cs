// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using AWS.Deploy.ServerMode.Client.Utilities;

namespace AWS.Deploy.ServerMode.Client
{
    public class CommandLineWrapper
    {
        private readonly bool _diagnosticLoggingEnabled;

        public CommandLineWrapper(bool diagnosticLoggingEnabled)
        {
            _diagnosticLoggingEnabled = diagnosticLoggingEnabled;
        }

        public virtual async Task<RunResult> Run(string command, params string[] stdIn)
        {
            var arguments = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"/c {command}" : $"-c \"{command}\"";

            var strOutput = new CappedStringBuilder(100);
            var strError = new CappedStringBuilder(50);

            var processStartInfo = new ProcessStartInfo
            {
                FileName = GetSystemShell(),
                Arguments = arguments,
                UseShellExecute = false, // UseShellExecute must be false in allow redirection of StdIn.
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = !_diagnosticLoggingEnabled, // It allows displaying stdout and stderr on the screen.
            };

            var process = Process.Start(processStartInfo);
            if (process == null)
            {
                throw new InvalidOperationException();
            }

            foreach (var line in stdIn)
            {
                await process.StandardInput.WriteLineAsync(line).ConfigureAwait(false);
            }

            process.OutputDataReceived += (sender, e) =>
            {
                strOutput.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                strError.AppendLine(e.Data);
            };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit(-1);

            var result = new RunResult
            {
                ExitCode = process.ExitCode,
                StandardError = strError.ToString(),
                StandardOut = strOutput.GetLastLines(5),
            };

            return await Task.FromResult(result).ConfigureAwait(false);
        }

        private string GetSystemShell()
        {
            if (TryGetEnvironmentVariable("COMSPEC", out var comspec))
            {
                return comspec!;
            }

            if (TryGetEnvironmentVariable("SHELL", out var shell))
            {
                return shell!;
            }

            // fallback to defaults
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "/bin/sh";
        }

        private bool TryGetEnvironmentVariable(string variable, out string? value)
        {
            value = Environment.GetEnvironmentVariable(variable);
            return !string.IsNullOrEmpty(value);
        }
    }

    public class RunResult
    {
        /// <summary>
        /// Indicates if this command was run successfully.  This checks that
        /// <see cref="StandardError"/> is empty.
        /// </summary>
        public bool Success => string.IsNullOrWhiteSpace(StandardError);

        /// <summary>
        /// Fully read <see cref="Process.StandardOutput"/>
        /// </summary>
        public string StandardOut { get; set; } = string.Empty;

        /// <summary>
        /// Fully read <see cref="Process.StandardError"/>
        /// </summary>
        public string StandardError { get; set; } = string.Empty;

        /// <summary>
        /// Fully read <see cref="Process.ExitCode"/>
        /// </summary>
        public int ExitCode { get; set; }
    }
}
