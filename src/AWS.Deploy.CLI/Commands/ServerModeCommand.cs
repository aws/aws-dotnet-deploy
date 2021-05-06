// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AWS.Deploy.CLI.ServerMode;
using Microsoft.AspNetCore.Hosting;

namespace AWS.Deploy.CLI.Commands
{
    public class ServerModeCommand
    {
        private readonly IToolInteractiveService _interactiveService;
        private readonly int _port;
        private readonly int? _parentPid;

        public ServerModeCommand(IToolInteractiveService interactiveService, int port, int? parentPid)
        {
            _interactiveService = interactiveService;
            _port = port;
            _parentPid = parentPid;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var url = $"http://localhost:{_port}";

            var builder = new WebHostBuilder()
                .UseKestrel()
                .UseUrls(url)
                .UseStartup<Startup>();

            var host = builder.Build();

            await host.RunAsync(cancellationToken); 
        }
    }
}
