// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;
using AWS.Deploy.CLI.Commands;
using AWS.Deploy.CLI.Utilities;
using AWS.Deploy.Common;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace AWS.Deploy.CLI;

public class App
{
    public static CommandApp<RootCommand> ConfigureServices(TypeRegistrar registrar)
    {
        var app = new CommandApp<RootCommand>(registrar);

        app.Configure(config =>
        {
            config.SetApplicationName(Constants.CLI.TOOL_NAME);
            config.AddCommand<DeployCommand>("deploy")
                .WithDescription("Inspect, build, and deploy the .NET project to AWS using the recommended AWS service.");
            config.AddCommand<ListDeploymentsCommand>("list-deployments")
                .WithDescription("List existing deployments.");
            config.AddCommand<DeleteDeploymentCommand>("delete-deployment")
                .WithDescription("Delete an existing deployment.");
            config.AddBranch("deployment-project", deploymentProject =>
            {
                deploymentProject.SetDescription("Save the deployment project inside a user provided directory path.");
                deploymentProject.AddCommand<GenerateDeploymentProjectCommand>("generate")
                    .WithDescription("Save the deployment project inside a user provided directory path without proceeding with a deployment");
            });
            config.AddCommand<ServerModeCommand>("server-mode")
                .WithDescription("Launches the tool in a server mode for IDEs like Visual Studio to integrate with.");

            config.SetExceptionHandler((exception, _) =>
            {
                var serviceProvider = registrar.GetServiceProvider();;
                var toolInteractiveService = serviceProvider.GetRequiredService<IToolInteractiveService>();

                if (exception.IsAWSDeploymentExpectedException())
                {
                    if (toolInteractiveService.Diagnostics)
                        toolInteractiveService.WriteErrorLine(exception.PrettyPrint());
                    else
                    {
                        toolInteractiveService.WriteErrorLine(string.Empty);
                        toolInteractiveService.WriteErrorLine(exception.Message);
                    }

                    toolInteractiveService.WriteErrorLine(string.Empty);
                    toolInteractiveService.WriteErrorLine("For more information, please visit our troubleshooting guide https://aws.github.io/aws-dotnet-deploy/troubleshooting-guide/.");
                    toolInteractiveService.WriteErrorLine("If you are still unable to solve this issue and believe this is an issue with the tooling, please cut a ticket https://github.com/aws/aws-dotnet-deploy/issues/new/choose.");

                    if (exception is TcpPortInUseException)
                    {
                        return CommandReturnCodes.TCP_PORT_ERROR;
                    }

                    // bail out with an non-zero return code.
                    return CommandReturnCodes.USER_ERROR;
                }
                else
                {
                    // This is a bug
                    toolInteractiveService.WriteErrorLine(
                        "Unhandled exception.  This is a bug.  Please copy the stack trace below and file a bug at https://github.com/aws/aws-dotnet-deploy. " +
                        exception.PrettyPrint());

                    return CommandReturnCodes.UNHANDLED_EXCEPTION;
                }
            });
        });

        return app;
    }

    public static async Task<int> RunAsync(string[] args, CommandApp<RootCommand> app, TypeRegistrar registrar)
    {
        var serviceProvider = registrar.GetServiceProvider();;
        var toolInteractiveService = serviceProvider.GetRequiredService<IToolInteractiveService>();

        toolInteractiveService.WriteLine("AWS .NET deployment tool for deploying .NET Core applications to AWS.");
        toolInteractiveService.WriteLine("Project Home: https://github.com/aws/aws-dotnet-deploy");
        toolInteractiveService.WriteLine(string.Empty);

        // if user didn't specify a command, default to help
        if (args.Length == 0)
        {
            args = ["-h"];
        }

        return await app.RunAsync(args);
    }
}
