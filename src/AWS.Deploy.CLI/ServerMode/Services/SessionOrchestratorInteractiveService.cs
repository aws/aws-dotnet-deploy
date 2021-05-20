// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.CLI.ServerMode.Hubs;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Shell;
using Microsoft.AspNetCore.SignalR;

namespace AWS.Deploy.CLI.ServerMode.Services
{
    public class SessionOrchestratorInteractiveService : IOrchestratorInteractiveService, ICommandRunnerDelegate
    {
        private readonly string _sessionId;
        private readonly IHubContext<DeploymentCommunicationHub, IDeploymentCommunicationHub> _hubContext;

        public Action<ProcessStartInfo> BeforeStart { get; set; }

        public SessionOrchestratorInteractiveService(string sessionId, IHubContext<DeploymentCommunicationHub, IDeploymentCommunicationHub> hubContext)
        {
            _sessionId = sessionId;
            _hubContext = hubContext;
            BeforeStart = _ => {};
        }

        public void LogDebugLine(string message)
        {
            _hubContext.Clients.Group(_sessionId).OnLogDebugLine(message);
        }

        public void LogErrorMessageLine(string message)
        {
            _hubContext.Clients.Group(_sessionId).OnLogErrorMessageLine(message);
        }

        public void LogMessageLine(string message)
        {
            _hubContext.Clients.Group(_sessionId).OnLogMessageLine(message);
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
