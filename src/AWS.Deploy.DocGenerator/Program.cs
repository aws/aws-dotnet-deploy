using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using AWS.Deploy.CLI.Extensions;
using AWS.Deploy.DocGenerator.Utilities;
using AWS.Deploy.CLI;
using System.Threading;
using AWS.Deploy.ServerMode.Client;
using AWS.Deploy.CLI.Commands;
using AWS.Deploy.CLI.Commands.Settings;
using AWS.Deploy.ServerMode.Client.Utilities;

namespace AWS.Deploy.DocGenerator
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddCustomServices();
            serviceCollection.AddGeneratorServices();

            serviceCollection.AddSingleton<IRestAPIClient, RestAPIClient>(serviceProvider =>
            {
                var interactiveService = serviceProvider.GetRequiredService<IToolInteractiveService>();
                var httpClient = ServerModeHttpClientFactory.ConstructHttpClient(ServerModeUtilities.ResolveDefaultCredentials);
                var serverCommandSettings = new ServerModeCommandSettings
                {
                    Port = 4152,
                    ParentPid = null,
                    UnsecureMode = true
                };
                var serverCommand = new ServerModeCommand(interactiveService);
                _ = serverCommand.ExecuteAsync(null!, serverCommandSettings, new CancellationTokenSource());

                var baseUrl = $"http://localhost:{4152}/";

                var client = new RestAPIClient(baseUrl, httpClient);

                client.WaitUntilServerModeReady().GetAwaiter().GetResult();

                return client;
            });

            var serviceProvider = serviceCollection.BuildServiceProvider();

            // calls the Run method in App, which is replacing Main
            var app = serviceProvider.GetService<App>();
            if (app == null)
            {
                throw new Exception("App dependencies aren't injected correctly." +
                                    " Verify DocGeneratorExtensions has all the required dependencies to instantiate App.");
            }

            await app.Run(args);
        }
    }
}
