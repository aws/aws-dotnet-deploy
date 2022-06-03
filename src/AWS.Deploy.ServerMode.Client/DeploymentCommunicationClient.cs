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
    public interface IDeploymentCommunicationClient : IDisposable
    {
        Action<string>? ReceiveLogDebugMessage { get; set; }

        Action<string>? ReceiveLogErrorMessage { get; set; }

        Action<string>? ReceiveLogInfoMessage { get; set; }

        Action<string, string>? ReceiveLogSectionStart { get; set; }

        Task JoinSession(string sessionId);
    }

    public class DeploymentCommunicationClient : IDeploymentCommunicationClient
    {
        private bool _disposedValue;

        private bool _initialized = false;
        private readonly HubConnection _connection;

        public Action<string>? ReceiveLogDebugMessage { get; set; }
        public Action<string>? ReceiveLogErrorMessage { get; set; }
        public Action<string>? ReceiveLogInfoMessage { get; set; }
        public Action<string, string>? ReceiveLogSectionStart { get; set; }


        public DeploymentCommunicationClient(string baseUrl)
        {

            _connection = new HubConnectionBuilder()
                .WithUrl(new Uri(new Uri(baseUrl), "DeploymentCommunicationHub"))
                .WithAutomaticReconnect()
                .Build();

            _connection.On<string>("OnLogDebugMessage", (message) =>
            {
                ReceiveLogDebugMessage?.Invoke(message);
            });

            _connection.On<string>("OnLogErrorMessage", (message) =>
            {
                ReceiveLogErrorMessage?.Invoke(message);
            });

            _connection.On<string>("OnLogInfoMessage", (message) =>
            {
                ReceiveLogInfoMessage?.Invoke(message);
            });

            _connection.On<string, string>("OnLogSectionStart", (message, description) =>
            {
                ReceiveLogSectionStart?.Invoke(message, description);
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
