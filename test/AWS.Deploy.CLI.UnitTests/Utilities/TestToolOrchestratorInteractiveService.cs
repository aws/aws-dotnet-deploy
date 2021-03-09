// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using AWS.Deploy.Orchestrator;

namespace AWS.Deploy.CLI.UnitTests.Utilities
{
    public class TestToolOrchestratorInteractiveService : IOrchestratorInteractiveService
    {
        public IList<string> DebugMessages { get; } = new List<string>();
        public IList<string> OutputMessages { get; } = new List<string>();
        public IList<string> ErrorMessages { get; } = new List<string>();

        public void LogDebugLine(string message) => DebugMessages.Add(message);
        public void LogErrorMessageLine(string message) => ErrorMessages.Add(message);
        public void LogMessageLine(string message) => OutputMessages.Add(message);
    }
}
