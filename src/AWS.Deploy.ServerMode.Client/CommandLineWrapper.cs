// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace AWS.Deploy.ServerMode.Client
{
    public class CommandLineWrapper
    {
        private readonly bool _diagnosticLoggingEnabled;

        public CommandLineWrapper(bool diagnosticLoggingEnabled)
        {
            _diagnosticLoggingEnabled = diagnosticLoggingEnabled;
        }

        public virtual async Task<int> Run(string command, params string[] stdIn)
        {
            var arguments = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"/c {command}" : $"-c \"{command}\"";

            var processStartInfo = new ProcessStartInfo
            {
                FileName = GetSystemShell(),
                Arguments = arguments,
                UseShellExecute = false, // UseShellExecute must be false in allow redirection of StdIn.
                RedirectStandardInput = true,
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

            process.WaitForExit(-1);

            return await Task.FromResult(process.ExitCode).ConfigureAwait(false);
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
}
