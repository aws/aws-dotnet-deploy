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
using AWS.Deploy.CLI.Commands.CommandHandlerInput;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.DeploymentManifest;

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
        private static readonly Option<string> _optionApply = new("--apply", "Path to the deployment settings file to be applied.");
        private static readonly Option<bool> _optionDisableInteractive = new(new []{"-s", "--silent" }, "Disable interactivity to deploy without any prompts for user input.");
        private static readonly Option<string> _optionOutputDirectory = new(new[]{"-o", "--output"}, "Directory path in which the CDK deployment project will be saved.");
        private static readonly Option<string> _optionProjectDisplayName = new(new[] { "--project-display-name" }, "The name of the deployment project that will be displayed in the list of available deployment options.");

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
        private readonly IDeploymentManifestEngine _deploymentManifestEngine;

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
            IConsoleUtilities consoleUtilities,
            IDeploymentManifestEngine deploymentManifestEngine)
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
            _deploymentManifestEngine = deploymentManifestEngine;
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
            rootCommand.Add(BuildDeploymentProjectCommand());
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
                _optionApply,
                _optionDiagnosticLogging,
                _optionDisableInteractive
            };

            deployCommand.Handler = CommandHandler.Create(async (DeployCommandHandlerInput input) =>
            {
                try
                {
                    _toolInteractiveService.Diagnostics = input.Diagnostics;
                    _toolInteractiveService.DisableInteractive = input.Silent;
                    
                    var userDeploymentSettings = !string.IsNullOrEmpty(input.Apply)
                        ? UserDeploymentSettings.ReadSettings(input.Apply)
                        : null;

                    var awsCredentials = await _awsUtilities.ResolveAWSCredentials(input.Profile ?? userDeploymentSettings?.AWSProfile);
                    var awsRegion = _awsUtilities.ResolveAWSRegion(input.Region ?? userDeploymentSettings?.AWSRegion);

                    _commandLineWrapper.RegisterAWSContext(awsCredentials, awsRegion);
                    _awsClientFactory.RegisterAWSContext(awsCredentials, awsRegion);

                    var systemCapabilities = _systemCapabilityEvaluator.Evaluate();

                    var projectDefinition = await _projectParserUtility.Parse(input.ProjectPath ?? "");

                    var callerIdentity = await _awsResourceQueryer.GetCallerIdentity();

                    var session = new OrchestratorSession(
                        projectDefinition,
                        awsCredentials,
                        awsRegion,
                        callerIdentity.Account)
                    {
                        SystemCapabilities = systemCapabilities,
                        AWSProfileName = input.Profile ?? userDeploymentSettings?.AWSProfile ?? null
                    };

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

                    await deploy.ExecuteAsync(input.StackName ?? "", userDeploymentSettings);

                    return CommandReturnCodes.SUCCESS;
                }
                catch (Exception e) when (e.IsAWSDeploymentExpectedException())
                {
                    if (input.Diagnostics)
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
            deleteCommand.Handler = CommandHandler.Create(async (DeleteCommandHandlerInput input) =>
            {
                try
                {
                    _toolInteractiveService.Diagnostics = input.Diagnostics;

                    var awsCredentials = await _awsUtilities.ResolveAWSCredentials(input.Profile);
                    var awsRegion = _awsUtilities.ResolveAWSRegion(input.Region);

                    _awsClientFactory.ConfigureAWSOptions(awsOption =>
                    {
                        awsOption.Credentials = awsCredentials;
                        awsOption.Region = RegionEndpoint.GetBySystemName(awsRegion);
                    });

                    if (string.IsNullOrEmpty(input.DeploymentName))
                    {
                        _toolInteractiveService.WriteErrorLine(string.Empty);
                        _toolInteractiveService.WriteErrorLine("Deployment name cannot be empty. Please provide a valid deployment name and try again.");
                        return CommandReturnCodes.USER_ERROR;
                    }

                    await new DeleteDeploymentCommand(_awsClientFactory, _toolInteractiveService, _consoleUtilities).ExecuteAsync(input.DeploymentName);

                    return CommandReturnCodes.SUCCESS;
                }
                catch (Exception e) when (e.IsAWSDeploymentExpectedException())
                {
                    if (input.Diagnostics)
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
            listCommand.Handler = CommandHandler.Create(async (ListCommandHandlerInput input) =>
            {
                try
                {
                    _toolInteractiveService.Diagnostics = input.Diagnostics;

                    var awsCredentials = await _awsUtilities.ResolveAWSCredentials(input.Profile);
                    var awsRegion = _awsUtilities.ResolveAWSRegion(input.Region);

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
                    if (input.Diagnostics)
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

        /// <summary>
        /// Builds the top level command called "deployment-project" which supports the creation and saving of the
        /// CDK deployment project.
        /// </summary>
        /// <returns>An instance of the <see cref="Command"/> class</returns>
        private Command BuildDeploymentProjectCommand()
        {
            var deploymentProjectCommand = new Command("deployment-project",
                "Save the deployment project inside a user provided directory path.");

            var generateDeploymentProjectCommand = new Command("generate",
                "Save the deployment project inside a user provided directory path without proceeding with a deployment")
            {
                _optionOutputDirectory,
                _optionDiagnosticLogging,
                _optionProjectPath,
                _optionProjectDisplayName
            };

            generateDeploymentProjectCommand.Handler = CommandHandler.Create(async (GenerateDeploymentProjectCommandHandlerInput input) =>
            {
                try
                {
                    _toolInteractiveService.Diagnostics = input.Diagnostics;
                    var projectDefinition = await _projectParserUtility.Parse(input.ProjectPath ?? "");

                    var saveDirectory = input.Output ?? "";
                    var projectDisplayName = input.ProjectDisplayName ?? "";

                    OrchestratorSession session = new OrchestratorSession(projectDefinition);

                    var targetApplicationFullPath = new DirectoryManager().GetDirectoryInfo(projectDefinition.ProjectPath).FullName;

                    var generateDeploymentProject = new GenerateDeploymentProjectCommand(
                        _toolInteractiveService,
                        _consoleUtilities,
                        _cdkProjectHandler,
                        _commandLineWrapper,
                        new DirectoryManager(),
                        new FileManager(),
                        session,
                        _deploymentManifestEngine,
                        targetApplicationFullPath);

                    await generateDeploymentProject.ExecuteAsync(saveDirectory, projectDisplayName);

                    return CommandReturnCodes.SUCCESS;
                }
                catch (Exception e) when (e.IsAWSDeploymentExpectedException())
                {
                    if (input.Diagnostics)
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

            deploymentProjectCommand.Add(generateDeploymentProjectCommand);
            return deploymentProjectCommand;
        }

        private Command BuildServerModeCommand()
        {
            var serverModeCommand = new Command(
                "server-mode",
                "Launches the tool in a server mode for IDEs like Visual Studio to integrate with.")
            {
                new Option<int>(new []{"--port"}, description: "Port the server mode will listen to."),
                new Option<int>(new []{"--parent-pid"}, description: "The ID of the process that is launching server mode. Server mode will exit when the parent pid terminates."),
                new Option<bool>(new []{"--encryption-keyinfo-stdin"}, description: "If set the cli reads encryption key info from stdin to use for decryption."),
                _optionDiagnosticLogging
            };
            serverModeCommand.Handler = CommandHandler.Create(async (ServerModeCommandHandlerInput input) =>
            {
                try
                {
                    var toolInteractiveService = new ConsoleInteractiveServiceImpl(input.Diagnostics);
                    var serverMode = new ServerModeCommand(toolInteractiveService, input.Port, input.ParentPid, input.EncryptionKeyInfoStdIn);

                    await serverMode.ExecuteAsync();

                    return CommandReturnCodes.SUCCESS;
                }
                catch (Exception e) when (e.IsAWSDeploymentExpectedException())
                {
                    if (input.Diagnostics)
                        _toolInteractiveService.WriteErrorLine(e.PrettyPrint());
                    else
                    {
                        _toolInteractiveService.WriteErrorLine(string.Empty);
                        _toolInteractiveService.WriteErrorLine(e.Message);
                    }

                    if (e is TcpPortInUseException)
                    {
                        return CommandReturnCodes.TCP_PORT_ERROR;
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

            return serverModeCommand;
        }
    }
}
