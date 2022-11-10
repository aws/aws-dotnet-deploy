// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AWS.Deploy.Orchestration.Utilities;

namespace AWS.Deploy.Orchestration.UnitTests
{
    public class TestCommandLineWrapper : ICommandLineWrapper
    {
        public List<(string command, string workingDirectory, bool streamOutputToInteractiveService)> Commands { get; } = new();
        public List<TryRunResult> Results { get; } = new();

        public Task Run(
            string command,
            string workingDirectory = "",
            bool streamOutputToInteractiveService = true,
            Action<TryRunResult> onComplete = null,
            bool redirectIO = true,
            string stdin = null,
            IDictionary<string, string> environmentVariables = null,
            CancellationToken cancelToken = default,
            bool needAwsCredentials = false)
        {
            Commands.Add((command, workingDirectory, streamOutputToInteractiveService));
            onComplete?.Invoke(Results.Last());
            return Task.CompletedTask;
        }

        public void ConfigureProcess(Action<ProcessStartInfo> processStartInfoAction) => throw new NotImplementedException();
    }
}
