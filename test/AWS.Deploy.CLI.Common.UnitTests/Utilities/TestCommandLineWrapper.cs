// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AWS.Deploy.Orchestration.Utilities;

namespace AWS.Deploy.CLI.Common.UnitTests.Utilities
{
    /// <summary>
    /// The container that represents a command to be executed by <see cref="TestCommandLineWrapper.Run(string, string, bool, Action{TryRunResult}, CancellationToken)"/>
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
        /// Specifies the input that should be piped into standard input for the process.
        /// </summary>
        public string Stdin { get; set; }

        /// <summary>
        /// The cancellation token for the async task.
        /// </summary>
        public CancellationToken CancellationToken { get; set; }
    }

    public class TestCommandLineWrapper : ICommandLineWrapper
    {
        public List<CommandLineRunObject> CommandsToExecute = new();

        public Dictionary<string, TryRunResult> MockedResults = new();

        public Task Run(
            string command,
            string workingDirectory = "",
            bool streamOutputToInteractiveService = true,
            Action<TryRunResult> onComplete = null,
            bool redirectIO = true,
            string stdin = null,
            IDictionary<string, string> environmentVariables = null,
            bool needAwsCredentials = false,
            CancellationToken cancellationToken = default)
        {
            CommandsToExecute.Add(new CommandLineRunObject
            {
                Command = command,
                WorkingDirectory = workingDirectory,
                StreamOutputToInteractiveService = streamOutputToInteractiveService,
                OnCompleteAction = onComplete,
                RedirectIO = redirectIO,
                Stdin = stdin,
                CancellationToken = cancellationToken
            });


            if (onComplete != null && MockedResults.ContainsKey(command))
            {
                onComplete(MockedResults[command]);
            }

            return Task.CompletedTask;
        }

        public void ConfigureProcess(Action<ProcessStartInfo> processStartInfoAction) => throw new NotImplementedException();
    }
}
