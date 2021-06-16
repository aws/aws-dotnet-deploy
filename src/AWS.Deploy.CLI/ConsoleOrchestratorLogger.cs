// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Deploy.Orchestration;

namespace AWS.Deploy.CLI
{
    public class ConsoleOrchestratorLogger : IOrchestratorInteractiveService
    {
        private readonly IToolInteractiveService _interactiveService;

        public ConsoleOrchestratorLogger(IToolInteractiveService interactiveService)
        {
            _interactiveService = interactiveService;
        }

        public void LogErrorMessageLine(string? message)
        {
            _interactiveService.WriteErrorLine(message);
        }

        public void LogMessageLine(string? message)
        {
            _interactiveService.WriteLine(message);
        }

        public void LogDebugLine(string? message)
        {
            _interactiveService.WriteDebugLine(message);
        }
    }
}
