// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AWS.Deploy.Orchestrator.Utilities;

namespace AWS.Deploy.CLI.UnitTests.Utilities
{
    /// <summary>
    /// The container that represents a command to be executed by <see cref="TestToolCommandLineWrapper.Run(string, string, bool, Action{TryRunResult}, CancellationToken)"/>
    /// </summary>
    public class CommandLineRunObject
    {
        /// <summary>
        /// The command to be executed via the command line wrapper.
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

    public class TestToolCommandLineWrapper : ICommandLineWrapper
    {
        public List<CommandLineRunObject> CommandsToExecute = new List<CommandLineRunObject>();

        public async Task Run(string command, string workingDirectory = "", bool streamOutputToInteractiveService = true, Action<TryRunResult> onComplete = null, bool redirectIO = true, CancellationToken cancelToken = default)
        {
            CommandsToExecute.Add(new CommandLineRunObject
            {
                Command = command,
                WorkingDirectory = workingDirectory,
                StreamOutputToInteractiveService = streamOutputToInteractiveService,
                OnCompleteAction = onComplete,
                RedirectIO = redirectIO,
                CancelToken = cancelToken
            });
        }
    }
}
