// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Diagnostics;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Shell;

namespace AWS.Deploy.CLI
{
    public class ConsoleOrchestratorLogger : IOrchestratorInteractiveService, ICommandRunnerDelegate
    {
        private readonly IToolInteractiveService _interactiveService;
        public Action<ProcessStartInfo> BeforeStart { get; set; }

        public ConsoleOrchestratorLogger(IToolInteractiveService interactiveService)
        {
            _interactiveService = interactiveService;
            BeforeStart = _ => throw new NotImplementedException();
        }

        public void LogErrorMessageLine(string message)
        {
            _interactiveService.WriteErrorLine(message);
        }

        public void LogMessageLine(string message)
        {
            _interactiveService.WriteLine(message);
        }

        public void LogDebugLine(string message)
        {
            _interactiveService.WriteDebugLine(message);
        }

        public void ErrorDataReceived(ProcessStartInfo processStartInfo, string data)
        {
            LogMessageLine(data);
        }

        public void OutputDataReceived(ProcessStartInfo processStartInfo, string data)
        {
            LogMessageLine(data);
        }
    }
}
