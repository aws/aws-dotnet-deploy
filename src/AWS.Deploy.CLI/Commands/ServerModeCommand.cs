// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AWS.Deploy.CLI.ServerMode;
using AWS.Deploy.CLI.ServerMode.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.Deploy.CLI.Commands
{
    public class ServerModeCommand
    {
        private readonly IToolInteractiveService _interactiveService;
        private readonly int _port;
        private readonly int? _parentPid;
        private readonly bool _encryptionKeyInfoStdIn;

        public ServerModeCommand(IToolInteractiveService interactiveService, int port, int? parentPid, bool encryptionKeyInfoStdIn)
        {
            _interactiveService = interactiveService;
            _port = port;
            _parentPid = parentPid;
            _encryptionKeyInfoStdIn = encryptionKeyInfoStdIn;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            _interactiveService.WriteLine("Server mode is an experimental feature being developed to allow communication between this CLI and the AWS Toolkit for Visual Studio. Expect behavior changes and API changes as server mode is being developed.");

            IEncryptionProvider encryptionProvider = CreateEncryptionProvider();

            var url = $"http://localhost:{_port}";

            var builder = new WebHostBuilder()
                .UseKestrel()
                .UseUrls(url)
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IEncryptionProvider>(encryptionProvider);
                })
                .UseStartup<Startup>();

            var host = builder.Build();

            if (_parentPid == null)
            {
                await host.RunAsync(cancellationToken);
            }
            else
            {
                var monitorTask = WaitOnParentPid(cancellationToken);
                var webTask = host.RunAsync(cancellationToken);

                //This call will wait until one of the tasks completes.
                //The monitor task will complete if a parent pid is not found.
                await Task.WhenAny(monitorTask, webTask);

                //The monitor task is expected to complete only when a parent pid is not found.
                if (monitorTask.IsCompleted && monitorTask.Result)
                {
                    _interactiveService.WriteLine(string.Empty);
                    _interactiveService.WriteLine("The parent process is no longer running.");
                    _interactiveService.WriteLine("Server mode is shutting down...");
                    await host.StopAsync(cancellationToken);
                }

                //If the web task completes with a fault because an exception was thrown,
                //We need to capture the inner exception and rethrow it so it can bubble up to the end user.
                if (webTask.IsCompleted && webTask.IsFaulted)
                {
                    var innerException = webTask.Exception?.InnerException;
                    if (innerException != null)
                        throw innerException;
                }
            }
        }

        private IEncryptionProvider CreateEncryptionProvider()
        {
            IEncryptionProvider encryptionProvider;
            if (_encryptionKeyInfoStdIn)
            {
                _interactiveService.WriteLine("Waiting on encryption key info from stdin");
                var input = _interactiveService.ReadLine();
                var keyInfo = EncryptionKeyInfo.ParseStdInKeyInfo(input);

                switch(keyInfo.Version)
                {
                    case EncryptionKeyInfo.VERSION_1_0:
                        var aes = Aes.Create();

                        if (keyInfo.Key != null)
                        {
                            aes.Key = Convert.FromBase64String(keyInfo.Key);
                        }
                        if (keyInfo.IV != null)
                        {
                            aes.IV = Convert.FromBase64String(keyInfo.IV);
                        }

                        encryptionProvider = new AesEncryptionProvider(aes);
                        break;
                    case null:
                        throw new InvalidEncryptionKeyInfoException("Missing required \"Version\" property in encryption key info");
                    default:
                        throw new InvalidEncryptionKeyInfoException($"Unsupported encryption key info {keyInfo.Version}");
                }

                _interactiveService.WriteLine("Encryption provider enabled");
            }
            else
            {
                encryptionProvider = new NoEncryptionProvider();
            }

            return encryptionProvider;
        }

        private async Task<bool> WaitOnParentPid(CancellationToken token)
        {
            if (_parentPid == null)
                return true;

            while (true)
            {
                try
                {
                    Process.GetProcessById((int)_parentPid);
                    await Task.Delay(1000, token);
                }
                catch
                {
                    return true;
                }
            }
        }
    }
}
