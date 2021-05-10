// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace AWS.Deploy.ServerMode.Client
{
    public class DeploymentCommunicationClient : IDisposable
    {
        private bool _disposedValue;

        private bool _initialized = false;
        private readonly HubConnection _connection;

        public Action<string>? ReceiveLogDebugLine { get; set; }
        public Action<string>? ReceiveLogErrorMessageLine { get; set; }
        public Action<string>? ReceiveLogMessageLineAction { get; set; }

        public Action<string>? ReceiveLogAllLogAction { get; set; }

        public DeploymentCommunicationClient(string baseUrl)
        {

            _connection = new HubConnectionBuilder()
                .WithUrl(new Uri(new Uri(baseUrl), "DeploymentCommunicationHub"))
                .WithAutomaticReconnect()
                .Build();

            _connection.On<string>("OnLogDebugLine", (message) =>
            {
                ReceiveLogDebugLine?.Invoke(message);
                ReceiveLogAllLogAction?.Invoke(message);
            });

            _connection.On<string>("OnLogErrorMessageLine", (message) =>
            {
                ReceiveLogErrorMessageLine?.Invoke(message);
                ReceiveLogAllLogAction?.Invoke(message);
            });

            _connection.On<string>("OnLogMessageLine", (message) =>
            {
                ReceiveLogMessageLineAction?.Invoke(message);
                ReceiveLogAllLogAction?.Invoke(message);
            });
        }

        public async Task JoinSession(string sessionId)
        {
            if(!_initialized)
            {
                _initialized = true;
                await _connection.StartAsync();
            }
            await _connection.SendAsync("JoinSession", sessionId);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _connection.DisposeAsync().GetAwaiter().GetResult();
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
