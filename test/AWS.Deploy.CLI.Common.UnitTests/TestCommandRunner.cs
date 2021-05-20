// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AWS.Deploy.Shell;

namespace AWS.Deploy.CLI.Common.UnitTests
{
    public class TestCommandRunner : ICommandRunner
    {
        /// <summary>
        /// The container that represents a command to be executed by <see cref="TestCommandRunner.Run"/>
        /// </summary>
        public class RunParams
        {
            /// <summary>
            /// The command to be executed via <see cref="CommandRunner"/>.
            /// </summary>
            public string Command { get; set; }

            /// <summary>
            /// The working directory from which the command will be executed.
            /// </summary>
            public string WorkingDirectory { get; set; }

            /// <summary>
            /// Specifies whether to stream the output of the command execution to the interactive service.
            /// </summary>
            public bool StreamOutputToInteractiveService { get; set; }

            /// <summary>
            /// The action to run upon the completion of the command execution.
            /// </summary>
            public Action<TryRunResult> OnCompleteAction { get; set; }

            /// <summary>
            /// Specifies whether to redirect standard input, output and error.
            /// </summary>
            public bool RedirectIO { get; set; }

            /// <summary>
            /// The cancellation token for the async task.
            /// </summary>
            public CancellationToken CancelToken { get; set; }
        }

        public List<RunParams> CommandsToExecute = new();
        public List<TryRunResult> Results { get; } = new();

        public ICommandRunnerDelegate Delegate { get; set; }

        public Task Run(
            string command,
            string workingDirectory = "",
            bool streamOutputToInteractiveService = true,
            Action<TryRunResult> onComplete = null,
            bool redirectIO = true,
            IDictionary<string, string> environmentVariables = null,
            CancellationToken cancelToken = default)
        {
            CommandsToExecute.Add(new RunParams
            {
                Command = command,
                WorkingDirectory = workingDirectory,
                StreamOutputToInteractiveService = streamOutputToInteractiveService,
                OnCompleteAction = onComplete,
                RedirectIO = redirectIO,
                CancelToken = cancelToken
            });

            if (CommandsToExecute.Count <= Results.Count)
            {
                onComplete?.Invoke(Results[CommandsToExecute.Count-1]);
            }

            return Task.CompletedTask;
        }
    }
}
