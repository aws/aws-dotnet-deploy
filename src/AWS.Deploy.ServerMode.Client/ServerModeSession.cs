// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using AWS.Deploy.ServerMode.Client.Utilities;
using Newtonsoft.Json;

namespace AWS.Deploy.ServerMode.Client
{
    /// <summary>
    /// Helper class that allows launching deployment tool in server mode.
    /// It abstracts the server mode setup, CLI command execution and retries when desired port is unavailable.
    /// </summary>
    public interface IServerModeSession
    {
        /// <summary>
        /// Starts deployment tool in server mode.
        /// It creates symmetric key and tries to setup the server mode.
        /// It also handles the retries when a desired port is unavailable in the provided range.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <exception cref="InternalServerModeException">Throws when deployment tool server failed to start for un unknown reason.</exception>
        /// <exception cref="PortUnavailableException">Throws when deployment tool server failed to start due to unavailability of free ports.</exception>
        Task Start(CancellationToken cancellationToken);

        /// <summary>
        /// Builds <see cref="IRestAPIClient"/> client using the cached base URL.
        /// If succeeded, <param name="restApiClient"></param> is initialized with current session client.
        /// </summary>
        /// <param name="credentialsGenerator">Func to that provides AWS credentials</param>
        /// <param name="restApiClient"><see cref="IRestAPIClient"/> client to initialize.</param>
        /// <returns>True, if <param name="restApiClient"/> is initialized successfully.</returns>
        bool TryGetRestAPIClient(Func<Task<AWSCredentials>> credentialsGenerator, out IRestAPIClient? restApiClient);

        /// <summary>
        /// Builds <see cref="IDeploymentCommunicationClient"/> client using the cached base URL.
        /// If succeeded, <param name="deploymentCommunicationClient"></param> is initialized with current session client.
        /// </summary>
        /// <param name="deploymentCommunicationClient"><see cref="DeploymentCommunicationClient"/> client to initialize.</param>
        /// <returns>True, if <param name="deploymentCommunicationClient"/> is initialized successfully.</returns>
        bool TryGetDeploymentCommunicationClient(out IDeploymentCommunicationClient? deploymentCommunicationClient);

        /// <summary>
        /// Returns the status of the deployment server by checking /api/v1/health API.
        /// Returns true, if the deployment server returns a success HTTP code.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Returns true, if the deployment server returns a success HTTP code.</returns>
        Task<bool> IsAlive(CancellationToken cancellationToken);
    }

    public class ServerModeSession : IServerModeSession, IDisposable
    {
        private const int TCP_PORT_ERROR = -100;

        private readonly int _startPort;
        private readonly int _endPort;
        private readonly CommandLineWrapper _commandLineWrapper;
        private readonly HttpClientHandler _httpClientHandler;
        private readonly TimeSpan _serverTimeout;
        private readonly string _deployToolPath;

        private string? _baseUrl;
        private Aes? _aes;

        private string HealthUrl
        {
            get
            {
                if (_baseUrl == null)
                {
                    throw new InvalidOperationException($"{nameof(_baseUrl)} must not be null.");
                }

                return $"{_baseUrl}/api/v1/health";
            }
        }

        public ServerModeSession(int startPort = 10000, int endPort = 10100, string deployToolPath = "", bool diagnosticLoggingEnabled = false)
            : this(new CommandLineWrapper(diagnosticLoggingEnabled),
                new HttpClientHandler(),
                TimeSpan.FromSeconds(60),
                startPort,
                endPort,
                deployToolPath)
        {
        }

        public ServerModeSession(CommandLineWrapper commandLineWrapper,
            HttpClientHandler httpClientHandler,
            TimeSpan serverTimeout,
            int startPort = 10000,
            int endPort = 10100,
            string deployToolPath = "")
        {
            _startPort = startPort;
            _endPort = endPort;
            _commandLineWrapper = commandLineWrapper;
            _httpClientHandler = httpClientHandler;
            _serverTimeout = serverTimeout;
            _deployToolPath = deployToolPath;
        }

        public async Task Start(CancellationToken cancellationToken)
        {
            var deployToolRoot = "dotnet aws";
            if (!string.IsNullOrEmpty(_deployToolPath))
            {
                if (!PathUtilities.IsDeployToolPathValid(_deployToolPath))
                    throw new InvalidAssemblyReferenceException("The specified assembly location is invalid.");

                deployToolRoot = _deployToolPath;
            }

            var currentProcessId = Process.GetCurrentProcess().Id;

            for (var port = _startPort; port <= _endPort; port++)
            {
                _aes = Aes.Create();
                _aes.GenerateKey();
                _aes.GenerateIV();

                var keyInfo = new EncryptionKeyInfo(
                    EncryptionKeyInfo.VERSION_1_0,
                    Convert.ToBase64String(_aes.Key),
                    Convert.ToBase64String(_aes.IV));

                var keyInfoStdin = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(keyInfo)));

                var command = $"{deployToolRoot} server-mode --port {port} --parent-pid {currentProcessId}";
                var startServerTask = _commandLineWrapper.Run(command, keyInfoStdin);

                _baseUrl = $"http://localhost:{port}";
                var isServerAvailableTask = IsServerAvailable(cancellationToken);

                if (isServerAvailableTask == await Task.WhenAny(startServerTask, isServerAvailableTask).ConfigureAwait(false))
                {
                    // The server timed out, this isn't a transient error, therefore, we throw
                    if (!isServerAvailableTask.Result)
                    {
                        throw new InternalServerModeException($"\"{command}\" failed for unknown reason.");
                    }

                    // Server has started, it is safe to return
                    return;
                }

                // For -100 errors, we want to check all the ports in the configured port range
                // If the error code other than -100, this is an unexpected exit code.
                if (startServerTask.Result != TCP_PORT_ERROR)
                {
                    throw new InternalServerModeException($"\"{command}\" failed for unknown reason.");
                }
            }

            throw new PortUnavailableException($"Free port unavailable in {_startPort}-{_endPort} range.");
        }

        public bool TryGetRestAPIClient(Func<Task<AWSCredentials>> credentialsGenerator, out IRestAPIClient? restApiClient)
        {
            if (_baseUrl == null || _aes == null)
            {
                restApiClient = null;
                return false;
            }

            var httpClient = ServerModeHttpClientFactory.ConstructHttpClient(credentialsGenerator, _aes);
            restApiClient = new RestAPIClient(_baseUrl, httpClient);
            return true;
        }

        public bool TryGetDeploymentCommunicationClient(out IDeploymentCommunicationClient? deploymentCommunicationClient)
        {
            if (_baseUrl == null || _aes == null)
            {
                deploymentCommunicationClient = null;
                return false;
            }

            deploymentCommunicationClient = new DeploymentCommunicationClient(_baseUrl);
            return true;
        }

        public async Task<bool> IsAlive(CancellationToken cancellationToken)
        {
            var client = new HttpClient(_httpClientHandler);

            try
            {
                var response = await client.GetAsync(HealthUrl, cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #region Private methods

        private Task<bool> IsServerAvailable(CancellationToken cancellationToken)
        {
            return WaitUntilHelper.WaitUntilSuccessStatusCode(
                HealthUrl,
                _httpClientHandler,
                TimeSpan.FromMilliseconds(100),
                _serverTimeout,
                cancellationToken);
        }

        #endregion

        #region Disposable

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _aes?.Dispose();
                _aes = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        private class EncryptionKeyInfo
        {
            public const string VERSION_1_0 = "1.0";

            public string Version { get; set; }
            public string Key { get; set; }
            public string IV { get; set; }

            public EncryptionKeyInfo(string version, string key, string iv)
            {
                Version = version;
                Key = key;
                IV = iv;
            }
        }
    }
}
