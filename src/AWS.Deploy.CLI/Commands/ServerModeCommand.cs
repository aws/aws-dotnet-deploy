// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
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
        private readonly bool _noEncryptionKeyInfo;

        public ServerModeCommand(IToolInteractiveService interactiveService, int port, int? parentPid, bool noEncryptionKeyInfo)
        {
            _interactiveService = interactiveService;
            _port = port;
            _parentPid = parentPid;
            _noEncryptionKeyInfo = noEncryptionKeyInfo;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            _interactiveService.WriteLine("Server mode is an experimental feature being developed to allow communication between this CLI and the AWS Toolkit for Visual Studio. Expect behavior changes and API changes as server mode is being developed.");

            IEncryptionProvider encryptionProvider = CreateEncryptionProvider();

            if (IsPortInUse(_port))
                throw new TcpPortInUseException("The port you have selected is currently in use by another process.");

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
                try
                {
                    var process = Process.GetProcessById((int)_parentPid);
                    process.EnableRaisingEvents = true;
                    process.Exited += async (sender, args) => { await ShutDownHost(host, cancellationToken); };
                }
                catch (Exception)
                {
                    return;
                }

                await host.RunAsync(cancellationToken);
            }
        }

        private async Task ShutDownHost(IWebHost host, CancellationToken cancellationToken)
        {
            _interactiveService.WriteLine(string.Empty);
            _interactiveService.WriteLine("The parent process is no longer running.");
            _interactiveService.WriteLine("Server mode is shutting down...");
            await host.StopAsync(cancellationToken);
        }

        private IEncryptionProvider CreateEncryptionProvider()
        {
            IEncryptionProvider encryptionProvider;
            if (_noEncryptionKeyInfo)
            {
                encryptionProvider = new NoEncryptionProvider();
            }
            else
            {
                _interactiveService.WriteLine("Waiting on symmetric key from stdin");
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

                        encryptionProvider = new AesEncryptionProvider(aes);
                        break;
                    case null:
                        throw new InvalidEncryptionKeyInfoException("Missing required \"Version\" property in the symmetric key");
                    default:
                        throw new InvalidEncryptionKeyInfoException($"Unsupported symmetric key {keyInfo.Version}");
                }

                _interactiveService.WriteLine("Encryption provider enabled");
            }

            return encryptionProvider;
        }

        private bool IsPortInUse(int port)
        {
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            var listeners = ipGlobalProperties.GetActiveTcpListeners();

            return listeners.Any(x => x.Port == port);
        }
    }
}
