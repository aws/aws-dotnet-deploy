// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace AWS.Deploy.CLI.ServerMode.Hubs
{
    public interface IDeploymentCommunicationHub
    {
        Task JoinSession(string sessionId);
        Task OnLogSectionStart(string sectionName, string? description);
        Task OnLogDebugMessage(string? message);
        Task OnLogErrorMessage(string? message);
        Task OnLogInfoMessage(string? message);
    }

    public class DeploymentCommunicationHub : Hub<IDeploymentCommunicationHub>
    {
        public async Task JoinSession(string sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
        }
    }
}
