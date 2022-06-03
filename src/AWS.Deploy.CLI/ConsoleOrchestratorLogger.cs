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

        public void LogSectionStart(string message, string? description)
        {
            var sectionBreak = new string('*', message.Length);
            _interactiveService.WriteLine(string.Empty);
            _interactiveService.WriteLine(sectionBreak);
            _interactiveService.WriteLine(message);
            if(description != null)
            {
                _interactiveService.WriteLine(new string('-', message.Length));
                _interactiveService.WriteLine(description);
            }
            _interactiveService.WriteLine(sectionBreak);
            _interactiveService.WriteLine(string.Empty);
        }

        public void LogErrorMessage(string? message)
        {
            _interactiveService.WriteErrorLine(message);
        }

        public void LogInfoMessage(string? message)
        {
            _interactiveService.WriteLine(message);
        }

        public void LogDebugMessage(string? message)
        {
            _interactiveService.WriteDebugLine(message);
        }
    }
}
