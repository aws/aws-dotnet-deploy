// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AWS.Deploy.CLI.Commands.Settings;
using AWS.Deploy.CLI.ServerMode;
using AWS.Deploy.CLI.ServerMode.Services;
using AWS.Deploy.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace AWS.Deploy.CLI.Commands;

/// <summary>
/// Represents a Server mode command that allows communication between this CLI and the AWS Toolkit for Visual Studio.
/// </summary>
public class ServerModeCommand(
    IToolInteractiveService toolInteractiveService) : CancellableAsyncCommand<ServerModeCommandSettings>
{
    /// <summary>
    /// Runs tool in server mode
    /// </summary>
    /// <param name="context">Command context</param>
    /// <param name="settings">Command settings</param>
    /// <param name="cancellationTokenSource">Cancellation token source</param>
    /// <returns>The command exit code</returns>
    public override async Task<int> ExecuteAsync(CommandContext context, ServerModeCommandSettings settings, CancellationTokenSource cancellationTokenSource)
    {
        toolInteractiveService.Diagnostics = settings.Diagnostics;

        toolInteractiveService.WriteLine("Server mode allows communication between this CLI and the AWS Toolkit for Visual Studio.");

        IEncryptionProvider encryptionProvider = CreateEncryptionProvider(settings.UnsecureMode);

        if (IsPortInUse(settings.Port))
            throw new TcpPortInUseException(DeployToolErrorCode.TcpPortInUse, "The port you have selected is currently in use by another process.");

        var url = $"http://localhost:{settings.Port}";

        var builder = new WebHostBuilder()
            .UseKestrel()
            .UseUrls(url)
            .ConfigureServices(services =>
            {
                services.AddSingleton<IEncryptionProvider>(encryptionProvider);
            })
            .UseStartup<Startup>();

        var host = builder.Build();

        if (settings.ParentPid.GetValueOrDefault() == 0)
        {
            await host.RunAsync(cancellationTokenSource.Token);
        }
        else
        {
            try
            {
                var process = Process.GetProcessById(settings.ParentPid.GetValueOrDefault());
                process.EnableRaisingEvents = true;
                process.Exited += async (sender, args) => { await ShutDownHost(host, cancellationTokenSource.Token); };
            }
            catch (Exception exception)
            {
                toolInteractiveService.WriteDebugLine(exception.PrettyPrint());
                return CommandReturnCodes.SUCCESS;
            }

            await host.RunAsync(cancellationTokenSource.Token);
        }

        return CommandReturnCodes.SUCCESS;
    }

    /// <summary>
    /// Shuts down the web host
    /// </summary>
    /// <param name="host">Web host</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task ShutDownHost(IWebHost host, CancellationToken cancellationToken)
    {
        toolInteractiveService.WriteLine(string.Empty);
        toolInteractiveService.WriteLine("The parent process is no longer running.");
        toolInteractiveService.WriteLine("Server mode is shutting down...");
        await host.StopAsync(cancellationToken);
    }

    /// <summary>
    /// Creates encryption provider
    /// </summary>
    /// <param name="noEncryptionKeyInfo">Indicates that no encryption key info will be provided</param>
    /// <returns>Encryption provider</returns>
    private IEncryptionProvider CreateEncryptionProvider(bool noEncryptionKeyInfo)
    {
        IEncryptionProvider encryptionProvider;
        if (noEncryptionKeyInfo)
        {
            encryptionProvider = new NoEncryptionProvider();
        }
        else
        {
            toolInteractiveService.WriteLine("Waiting on symmetric key from stdin");
            var input = toolInteractiveService.ReadLine();
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

            toolInteractiveService.WriteLine("Encryption provider enabled");
        }

        return encryptionProvider;
    }

    /// <summary>
    /// Checks if a port is in use
    /// </summary>
    /// <param name="port">Tcp port</param>
    /// <returns>true, if port is in use. false if not.</returns>
    private bool IsPortInUse(int port)
    {
        var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
        var listeners = ipGlobalProperties.GetActiveTcpListeners();

        return listeners.Any(x => x.Port == port);
    }
}
