// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.CLI.ServerMode.Hubs;
using AWS.Deploy.Orchestration;
using Microsoft.AspNetCore.SignalR;

namespace AWS.Deploy.CLI.ServerMode.Services
{
    public class SessionOrchestratorInteractiveService : IOrchestratorInteractiveService
    {
        private readonly string _sessionId;
        private readonly IHubContext<DeploymentCommunicationHub, IDeploymentCommunicationHub> _hubContext;


        public SessionOrchestratorInteractiveService(string sessionId, IHubContext<DeploymentCommunicationHub, IDeploymentCommunicationHub> hubContext)
        {
            _sessionId = sessionId;
            _hubContext = hubContext;
        }

        public void LogDebugLine(string? message)
        {
            _hubContext.Clients.Group(_sessionId).OnLogDebugLine(message);
        }

        public void LogErrorMessageLine(string? message)
        {
            _hubContext.Clients.Group(_sessionId).OnLogErrorMessageLine(message);
        }

        public void LogMessageLine(string? message)
        {
            _hubContext.Clients.Group(_sessionId).OnLogMessageLine(message);
        }
    }
}
