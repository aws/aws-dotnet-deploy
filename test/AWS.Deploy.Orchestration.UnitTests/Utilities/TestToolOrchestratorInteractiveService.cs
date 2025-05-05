// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace AWS.Deploy.Orchestration.UnitTests.Utilities
{
    public class TestToolOrchestratorInteractiveService : IOrchestratorInteractiveService
    {
        public IList<string?> SectionStartMessages { get; } = new List<string?>();
        public IList<string?> DebugMessages { get; } = new List<string?>();
        public IList<string?> OutputMessages { get; } = new List<string?>();
        public IList<string?> ErrorMessages { get; } = new List<string?>();

        public void LogSectionStart(string message, string? description) => SectionStartMessages.Add(message);
        public void LogDebugMessage(string? message) => DebugMessages.Add(message);
        public void LogErrorMessage(string? message) => ErrorMessages.Add(message);
        public void LogInfoMessage(string? message) => OutputMessages.Add(message);
    }
}
