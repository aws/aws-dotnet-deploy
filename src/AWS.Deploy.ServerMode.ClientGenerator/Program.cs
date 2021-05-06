using AWS.Deploy.CLI;
using AWS.Deploy.CLI.Commands;
using NSwag;
using NSwag.CodeGeneration.CSharp;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AWS.Deploy.ServerMode.ClientGenerator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Start up the server mode to make the swagger.json file available.
            var portNumber = 5678;
            var serverCommand = new ServerModeCommand(new ConsoleInteractiveServiceImpl(), portNumber, null);
            var cancelSource = new CancellationTokenSource();
            _ = serverCommand.ExecuteAsync(cancelSource.Token);
            try
            {
                // Wait till server mode is started.
                await Task.Delay(3000);

                // Grab the swagger.json from the running instances of server mode
                var document = await OpenApiDocument.FromUrlAsync($"http://localhost:{portNumber}/swagger/v1/swagger.json");

                var settings = new CSharpClientGeneratorSettings
                {
                    ClassName = "RestAPIClient",
                    CSharpGeneratorSettings =
                    {
                        Namespace = "AWS.Deploy.ServerMode.Client",
                    },
                    HttpClientType = "ServerModeHttpClient"
                };

                var generator = new CSharpClientGenerator(document, settings);
                var code = generator.GenerateFile();

                // Save the generated client to the AWS.Deploy.ServerMode.Client project
                var fullPath = DetermineFullFilePath("RestAPI.cs");
                File.WriteAllText(fullPath, code);
            }
            finally
            {
                // terminate running server mode.
                cancelSource.Cancel();
            }
        }

        static string DetermineFullFilePath(string codeFile)
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());

            while (!string.Equals(dir.Name, "src"))
            {
                dir = dir.Parent;
            }

            return Path.Combine(dir.FullName, "AWS.Deploy.ServerMode.Client", codeFile);
        }
    }
}
