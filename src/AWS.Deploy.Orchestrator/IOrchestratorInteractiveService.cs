// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.Orchestrator
{
    public interface IOrchestratorInteractiveService
    {
        void LogErrorMessageLine(string message);

        void LogMessageLine(string message);

        void LogDebugLine(string message);
    }
}
