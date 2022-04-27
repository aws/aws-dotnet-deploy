// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.Orchestration
{
    public interface IOrchestratorInteractiveService
    {
        void LogSectionStart(string sectionName, string? description);

        void LogErrorMessage(string? message);

        void LogInfoMessage(string? message);

        void LogDebugMessage(string? message);
    }
}
