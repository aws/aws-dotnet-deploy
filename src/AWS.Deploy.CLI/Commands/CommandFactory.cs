// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using Amazon;
using AWS.Deploy.CLI.Commands.TypeHints;
using AWS.Deploy.CLI.Utilities;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Extensions;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.CDK;
using AWS.Deploy.Orchestration.Data;
using AWS.Deploy.Orchestration.Utilities;

namespace AWS.Deploy.CLI.Commands
{
    public interface ICommandFactory
    {
        Command BuildRootCommand();
    }

    public class CommandFactory : ICommandFactory
    {
        private static readonly Option<string> _optionProfile = new("--profile", "AWS credential profile used to make calls to AWS.");
        private static readonly Option<string> _optionRegion = new("--region", "AWS region to deploy the application to. For example, us-west-2.");
        private static readonly Option<string> _optionProjectPath = new("--project-path", () => Directory.GetCurrentDirectory(), "Path to the project to deploy.");
        private static readonly Option<string> _optionStackName = new("--stack-name", "Name the AWS stack to deploy your application to.");
        private static readonly Option<bool> _optionDiagnosticLogging = new(new []{"-d", "--diagnostics"}, "Enable diagnostic output.");

        private readonly IToolInteractiveService _toolInteractiveService;
        private readonly IOrchestratorInteractiveService _orchestratorInteractiveService;
        private readonly ICDKManager _cdkManager;
        private readonly ISystemCapabilityEvaluator _systemCapabilityEvaluator;
        private readonly ICloudApplicationNameGenerator _cloudApplicationNameGenerator;
        private readonly IAWSUtilities _awsUtilities;
        private readonly IAWSClientFactory _awsClientFactory;
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IProjectParserUtility _projectParserUtility;
        private readonly ICommandLineWrapper _commandLineWrapper;
        private readonly ICdkProjectHandler _cdkProjectHandler;
        private readonly IDeploymentBundleHandler _deploymentBundleHandler;
        private readonly ITemplateMetadataReader _templateMetadataReader;
        private readonly IDeployedApplicationQueryer _deployedApplicationQueryer;
        private readonly ITypeHintCommandFactory _typeHintCommandFactory;
        private readonly IConsoleUtilities _consoleUtilities;

        public CommandFactory(
            IToolInteractiveService toolInteractiveService,
            IOrchestratorInteractiveService orchestratorInteractiveService,
            ICDKManager cdkManager,
            ISystemCapabilityEvaluator systemCapabilityEvaluator,
            ICloudApplicationNameGenerator cloudApplicationNameGenerator,
            IAWSUtilities awsUtilities,
            IAWSClientFactory awsClientFactory,
            IAWSResourceQueryer awsResourceQueryer,
            IProjectParserUtility projectParserUtility,
            ICommandLineWrapper commandLineWrapper,
            ICdkProjectHandler cdkProjectHandler,
            IDeploymentBundleHandler deploymentBundleHandler,
            ITemplateMetadataReader templateMetadataReader,
            IDeployedApplicationQueryer deployedApplicationQueryer,
            ITypeHintCommandFactory typeHintCommandFactory,
            IConsoleUtilities consoleUtilities)
        {
            _toolInteractiveService = toolInteractiveService;
            _orchestratorInteractiveService = orchestratorInteractiveService;
            _cdkManager = cdkManager;
            _systemCapabilityEvaluator = systemCapabilityEvaluator;
            _cloudApplicationNameGenerator = cloudApplicationNameGenerator;
            _awsUtilities = awsUtilities;
            _awsClientFactory = awsClientFactory;
            _awsResourceQueryer = awsResourceQueryer;
            _projectParserUtility = projectParserUtility;
            _commandLineWrapper = commandLineWrapper;
            _cdkProjectHandler = cdkProjectHandler;
            _deploymentBundleHandler = deploymentBundleHandler;
            _templateMetadataReader = templateMetadataReader;
            _deployedApplicationQueryer = deployedApplicationQueryer;
            _typeHintCommandFactory = typeHintCommandFactory;
            _consoleUtilities = consoleUtilities;
        }

        public Command BuildRootCommand()
        {
            // Name is important to set here to show correctly in the CLI usage help.
            // Either dotnet-aws or dotnet aws works from the CLI. System.Commandline's help system does not like having a space with dotnet aws.
            var rootCommand = new RootCommand {
                Name = "dotnet-aws",
                Description = "The AWS .NET deployment tool for deploying .NET applications on AWS."
            };

            rootCommand.Add(BuildDeployCommand());
            rootCommand.Add(BuildListCommand());
            rootCommand.Add(BuildDeleteCommand());
            rootCommand.Add(BuildServerModeCommand());

            return rootCommand;
        }

        private Command BuildDeployCommand()
        {
             var deployCommand = new Command(
                "deploy",
                "Inspect, build, and deploy the .NET project to AWS using the recommended AWS service.")
            {
                _optionProfile,
                _optionRegion,
                _optionProjectPath,
                _optionStackName,
                _optionDiagnosticLogging,
            };

            deployCommand.Handler = CommandHandler.Create<string, string, string, string, bool, bool>(async (profile, region, projectPath, stackName, saveCdkProject, diagnostics) =>
            {
                try
                {
                    _toolInteractiveService.Diagnostics = diagnostics;

                    var previousSettings = PreviousDeploymentSettings.ReadSettings(projectPath, null);

                    var awsCredentials = await _awsUtilities.ResolveAWSCredentials(profile, previousSettings.Profile);
                    var awsRegion = _awsUtilities.ResolveAWSRegion(region, previousSettings.Region);

                    _commandLineWrapper.RegisterAWSContext(awsCredentials, awsRegion);
                    _awsClientFactory.RegisterAWSContext(awsCredentials, awsRegion);

                    var systemCapabilities = _systemCapabilityEvaluator.Evaluate();

                    var projectDefinition = await _projectParserUtility.Parse(projectPath);

                    var callerIdentity = await _awsResourceQueryer.GetCallerIdentity();

                    var session = new OrchestratorSession(
                        projectDefinition,
                        profile,
                        awsCredentials,
                        awsRegion,
                        systemCapabilities,
                        callerIdentity.Account);

                    var dockerEngine = new DockerEngine.DockerEngine(projectDefinition);

                    var deploy = new DeployCommand(
                        _toolInteractiveService,
                        _orchestratorInteractiveService,
                        _cdkProjectHandler,
                        _cdkManager,
                        _deploymentBundleHandler,
                        dockerEngine,
                        _awsResourceQueryer,
                        _templateMetadataReader,
                        _deployedApplicationQueryer,
                        _typeHintCommandFactory,
                        _cloudApplicationNameGenerator,
                        _consoleUtilities,
                        session);

                    await deploy.ExecuteAsync(stackName, saveCdkProject);

                    return CommandReturnCodes.SUCCESS;
                }
                catch (Exception e) when (e.IsAWSDeploymentExpectedException())
                {
                    if (diagnostics)
                        _toolInteractiveService.WriteErrorLine(e.PrettyPrint());
                    else
                    {
                        _toolInteractiveService.WriteErrorLine(string.Empty);
                        _toolInteractiveService.WriteErrorLine(e.Message);
                    }
                    // bail out with an non-zero return code.
                    return CommandReturnCodes.USER_ERROR;
                }
                catch (Exception e)
                {
                    // This is a bug
                    _toolInteractiveService.WriteErrorLine(
                        "Unhandled exception.  This is a bug.  Please copy the stack trace below and file a bug at https://github.com/aws/aws-dotnet-deploy. " +
                        e.PrettyPrint());

                    return CommandReturnCodes.UNHANDLED_EXCEPTION;
                }
            });
            return deployCommand;
        }

        private Command BuildDeleteCommand()
        {
            var deleteCommand = new Command("delete-deployment", "Delete an existing deployment.")
            {
                _optionProfile,
                _optionRegion,
                _optionProjectPath,
                _optionDiagnosticLogging,
                new Argument("deployment-name")
            };
            deleteCommand.Handler = CommandHandler.Create<string, string, string, string, bool>(async (profile, region, projectPath, deploymentName, diagnostics) =>
            {
                try
                {
                    _toolInteractiveService.Diagnostics = diagnostics;

                    var previousSettings = PreviousDeploymentSettings.ReadSettings(projectPath, null);

                    var awsCredentials = await _awsUtilities.ResolveAWSCredentials(profile, previousSettings.Profile);
                    var awsRegion = _awsUtilities.ResolveAWSRegion(region, previousSettings.Region);

                    _awsClientFactory.ConfigureAWSOptions(awsOption =>
                    {
                        awsOption.Credentials = awsCredentials;
                        awsOption.Region = RegionEndpoint.GetBySystemName(awsRegion);
                    });

                    await new DeleteDeploymentCommand(_awsClientFactory, _toolInteractiveService, _consoleUtilities).ExecuteAsync(deploymentName);

                    return CommandReturnCodes.SUCCESS;
                }
                catch (Exception e) when (e.IsAWSDeploymentExpectedException())
                {
                    if (diagnostics)
                        _toolInteractiveService.WriteErrorLine(e.PrettyPrint());
                    else
                    {
                        _toolInteractiveService.WriteErrorLine(string.Empty);
                        _toolInteractiveService.WriteErrorLine(e.Message);
                    }
                    // bail out with an non-zero return code.
                    return CommandReturnCodes.USER_ERROR;
                }
                catch (Exception e)
                {
                    // This is a bug
                    _toolInteractiveService.WriteErrorLine(
                        "Unhandled exception.  This is a bug.  Please copy the stack trace below and file a bug at https://github.com/aws/aws-dotnet-deploy. " +
                        e.PrettyPrint());

                    return CommandReturnCodes.UNHANDLED_EXCEPTION;
                }
            });
            return deleteCommand;
        }

        private Command BuildListCommand()
        {
            var listCommand = new Command("list-deployments", "List existing deployments.")
            {
                _optionProfile,
                _optionRegion,
                _optionProjectPath,
                _optionDiagnosticLogging
            };
            listCommand.Handler = CommandHandler.Create<string, string, string, bool>(async (profile, region, projectPath, diagnostics) =>
            {
                try
                {
                    _toolInteractiveService.Diagnostics = diagnostics;

                    var previousSettings = PreviousDeploymentSettings.ReadSettings(projectPath, null);

                    var awsCredentials = await _awsUtilities.ResolveAWSCredentials(profile, previousSettings.Profile);
                    var awsRegion = _awsUtilities.ResolveAWSRegion(region, previousSettings.Region);

                    _awsClientFactory.ConfigureAWSOptions(awsOptions =>
                    {
                        awsOptions.Credentials = awsCredentials;
                        awsOptions.Region = RegionEndpoint.GetBySystemName(awsRegion);
                    });

                    var listDeploymentsCommand = new ListDeploymentsCommand(_toolInteractiveService, _deployedApplicationQueryer);

                    await listDeploymentsCommand.ExecuteAsync();
                }
                catch (Exception e) when (e.IsAWSDeploymentExpectedException())
                {
                    if (diagnostics)
                        _toolInteractiveService.WriteErrorLine(e.PrettyPrint());
                    else
                    {
                        _toolInteractiveService.WriteErrorLine(string.Empty);
                        _toolInteractiveService.WriteErrorLine(e.Message);
                    }
                }
                catch (Exception e)
                {
                    // This is a bug
                    _toolInteractiveService.WriteErrorLine(
                        "Unhandled exception.  This is a bug.  Please copy the stack trace below and file a bug at https://github.com/aws/aws-dotnet-deploy. " +
                        e.PrettyPrint());
                }
            });
            return listCommand;
        }

        private Command BuildServerModeCommand()
        {
            var serverModeCommand = new Command(
                "server-mode",
                "Launches the tool in a server mode for IDEs like Visual Studio to integrate with.")
            {
                new Option<int>(new []{"--port"}, description: "Port the server mode will listen to."),
                new Option<int>(new []{"--parent-pid"}, description: "The ID of the process that is launching server mode. Server mode will exit when the parent pid terminates."),
                _optionDiagnosticLogging
            };
            serverModeCommand.Handler = CommandHandler.Create<int, int?, bool>(async (port, parentPid, diagnostics) =>
            {
                try
                {
                    var toolInteractiveService = new ConsoleInteractiveServiceImpl(diagnostics);
                    var serverMode = new ServerModeCommand(toolInteractiveService, port, parentPid);

                    await serverMode.ExecuteAsync();
                }
                catch (Exception e) when (e.IsAWSDeploymentExpectedException())
                {
                    if (diagnostics)
                        _toolInteractiveService.WriteErrorLine(e.PrettyPrint());
                    else
                    {
                        _toolInteractiveService.WriteErrorLine(string.Empty);
                        _toolInteractiveService.WriteErrorLine(e.Message);
                    }
                }
                catch (Exception e)
                {
                    // This is a bug
                    _toolInteractiveService.WriteErrorLine(
                        "Unhandled exception.  This is a bug.  Please copy the stack trace below and file a bug at https://github.com/aws/aws-dotnet-deploy. " +
                        e.PrettyPrint());
                }
            });

            return serverModeCommand;
        }
    }
}
