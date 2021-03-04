// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AWS.Deploy.Orchestrator.Utilities;

namespace AWS.Deploy.Orchestrator.UnitTest
{
    public class TestCommandLineWrapper : ICommandLineWrapper
    {
        public readonly List<(string, string, bool)> Commands = new List<(string, string, bool)>();
        public readonly List<TryRunResult> Results = new List<TryRunResult>();

        public Task Run(string command, string workingDirectory = "", bool streamOutputToInteractiveService = true, Func<Process, Task> onComplete = null, CancellationToken cancelToken = default)
        {
            Commands.Add((command, workingDirectory, streamOutputToInteractiveService));
            return Task.CompletedTask;
        }

        public Task<TryRunResult> TryRunWithResult(string command, string workingDirectory = "", bool streamOutputToInteractiveService = false, CancellationToken cancelToken = default)
        {
            Commands.Add((command, workingDirectory, streamOutputToInteractiveService));
            return Task.FromResult(Results[Commands.Count-1]);
        }
    }
}
