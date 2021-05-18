// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
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

            await host.RunAsync(cancellationToken); 
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
    }
}
