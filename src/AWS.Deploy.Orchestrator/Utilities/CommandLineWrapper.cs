// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Amazon.Runtime;

namespace AWS.Deploy.Orchestrator.Utilities
{
    public class CommandLineWrapper
    {
        private readonly IOrchestratorInteractiveService _interactiveService;
        private readonly AWSCredentials _awsCredentials;
        private readonly string _awsRegion;

        public CommandLineWrapper(IOrchestratorInteractiveService interactiveService, AWSCredentials awsCredentials, string awsRegion)
        {
            _interactiveService = interactiveService;
            _awsCredentials = awsCredentials;
            _awsRegion = awsRegion;
        }

        public void Run(IEnumerable<string> commands, string workingDirectory = "")
        {
            var process = new Process();
            var shell = GetSystemShell();
            var processStartInfo = new ProcessStartInfo
            {
                FileName = shell,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = workingDirectory,
                EnvironmentVariables = { { "AWS_ACCESS_KEY_ID", _awsCredentials.GetCredentials().AccessKey }, { "AWS_SECRET_ACCESS_KEY", _awsCredentials.GetCredentials().SecretKey }, { "AWS_REGION", _awsRegion } }
            };

            if (_awsCredentials.GetCredentials().UseToken)
            {
                processStartInfo.EnvironmentVariables.Add("AWS_SESSION_TOKEN", _awsCredentials.GetCredentials().Token);
            }

            process.StartInfo = processStartInfo;
            process.Start();
            process.OutputDataReceived += (sender, e) => { _interactiveService.LogMessageLine(e.Data); };
            process.ErrorDataReceived += (sender, e) => { _interactiveService.LogMessageLine(e.Data); };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            using (var streamWriter = process.StandardInput)
            {
                foreach (var command in commands)
                {
                    streamWriter.WriteLine(command);
                }
            }

            process.WaitForExit();
        }

        private string GetSystemShell()
        {
            var comspec = Environment.GetEnvironmentVariable("COMSPEC");
            if (!string.IsNullOrEmpty(comspec))
            {
                _interactiveService.LogMessageLine($"OS Version {Environment.OSVersion}. Using {comspec} as default shell.");
                return comspec;
            }

            var shell = Environment.GetEnvironmentVariable("SHELL");
            if (!string.IsNullOrEmpty(shell))
            {
                _interactiveService.LogMessageLine($"OS Version {Environment.OSVersion}. Using {shell} as default shell.");
                return shell;
            }

            throw new NotSupportedException($"{Environment.OSVersion} isn't supported");
        }
    }
}
