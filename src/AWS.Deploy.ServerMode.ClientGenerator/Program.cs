using AWS.Deploy.CLI;
using AWS.Deploy.CLI.Commands;
using NSwag;
using NSwag.CodeGeneration.CSharp;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AWS.Deploy.CLI.Commands.Settings;

// Start up the server mode to make the swagger.json file available.
var portNumber = 5678;
var serverCommandSettings = new ServerModeCommandSettings
{
    Port = portNumber,
    ParentPid = null,
    UnsecureMode = true
};
var serverCommand = new ServerModeCommand(new ConsoleInteractiveServiceImpl());
var cancelSource = new CancellationTokenSource();
_ = serverCommand.ExecuteAsync(null!, serverCommandSettings, cancelSource);
try
{
    // Wait till server mode is started.
    await Task.Delay(3000);

    // Grab the swagger.json from the running instances of server mode
    var document = await OpenApiDocument.FromUrlAsync($"http://localhost:{portNumber}/swagger/v1/swagger.json");

    var settings = new CSharpClientGeneratorSettings
    {
        ClassName = "RestAPIClient",
        GenerateClientInterfaces = true,
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

static string DetermineFullFilePath(string codeFile)
{
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());

    while (!string.Equals(dir?.Name, "src"))
    {
        if (dir == null)
            break;

        dir = dir.Parent;
    }

    if (dir == null)
        throw new Exception("Could not determine file path of current directory.");

    return Path.Combine(dir.FullName, "AWS.Deploy.ServerMode.Client", codeFile);
}
