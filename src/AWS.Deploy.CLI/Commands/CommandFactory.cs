// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
using AWS.Deploy.Orchestration.DisplayedResources;
using AWS.Deploy.Orchestration.LocalUserSettings;

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
        private static readonly Option<bool> _optionDiagnosticLogging = new(new[] { "-d", "--diagnostics" }, "Enable diagnostic output.");
        private static readonly Option<string> _optionApply = new("--apply", "Path to the deployment settings file to be applied.");
        private static readonly Option<bool> _optionDisableInteractive = new(new[] { "-s", "--silent" }, "Disable interactivity to deploy without any prompts for user input.");
        private static readonly Option<string> _optionOutputDirectory = new(new[] { "-o", "--output" }, "Directory path in which the CDK deployment project will be saved.");
        private static readonly Option<string> _optionProjectDisplayName = new(new[] { "--project-display-name" }, "The name of the deployment project that will be displayed in the list of available deployment options.");
        private static readonly Option<string> _optionDeploymentProject = new(new[] { "--deployment-project" }, "The absolute or relative path of the CDK project that will be used for deployment");
        private static readonly object s_root_command_lock = new();
        private static readonly object s_child_command_lock = new();

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
        private readonly IDisplayedResourcesHandler _displayedResourceHandler;
        private readonly IConsoleUtilities _consoleUtilities;
        private readonly IDirectoryManager _directoryManager;
        private readonly IFileManager _fileManager;
        private readonly IDeploymentManifestEngine _deploymentManifestEngine;
        private readonly ICustomRecipeLocator _customRecipeLocator;
        private readonly ILocalUserSettingsEngine _localUserSettingsEngine;
        private readonly ICDKVersionDetector _cdkVersionDetector;

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
            IDisplayedResourcesHandler displayedResourceHandler,
            IConsoleUtilities consoleUtilities,
            IDirectoryManager directoryManager,
            IFileManager fileManager,
            IDeploymentManifestEngine deploymentManifestEngine,
            ICustomRecipeLocator customRecipeLocator,
            ILocalUserSettingsEngine localUserSettingsEngine,
            ICDKVersionDetector cdkVersionDetector)
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
            _displayedResourceHandler = displayedResourceHandler;
            _consoleUtilities = consoleUtilities;
            _directoryManager = directoryManager;
            _fileManager = fileManager;
            _deploymentManifestEngine = deploymentManifestEngine;
            _customRecipeLocator = customRecipeLocator;
            _localUserSettingsEngine = localUserSettingsEngine;
            _cdkVersionDetector = cdkVersionDetector;
        }

        public Command BuildRootCommand()
        {
            // Name is important to set here to show correctly in the CLI usage help.
            // Either dotnet-aws or dotnet aws works from the CLI. System.Commandline's help system does not like having a space with dotnet aws.
            var rootCommand = new RootCommand
            {
                Name = "dotnet-aws",
                Description = "The AWS .NET deployment tool for deploying .NET applications on AWS."
            };

            lock(s_root_command_lock)
            {
                rootCommand.Add(BuildDeployCommand());
                rootCommand.Add(BuildListCommand());
                rootCommand.Add(BuildDeleteCommand());
                rootCommand.Add(BuildDeploymentProjectCommand());
                rootCommand.Add(BuildServerModeCommand());
            }

            return rootCommand;
        }

        private Command BuildDeployCommand()
        {
            var deployCommand = new Command(
                "deploy",
                "Inspect, build, and deploy the .NET project to AWS using the recommended AWS service.");

            lock (s_child_command_lock)
            {
                deployCommand.Add(_optionProfile);
                deployCommand.Add(_optionRegion);
                deployCommand.Add(_optionProjectPath);
                deployCommand.Add(_optionStackName);
                deployCommand.Add(_optionApply);
                deployCommand.Add(_optionDiagnosticLogging);
                deployCommand.Add(_optionDisableInteractive);
                deployCommand.Add(_optionDeploymentProject);
            }

            deployCommand.Handler = CommandHandler.Create(async (DeployCommandHandlerInput input) =>
            {
                try
                {
                    _toolInteractiveService.Diagnostics = input.Diagnostics;
                    _toolInteractiveService.DisableInteractive = input.Silent;

                    var projectDefinition = await _projectParserUtility.Parse(input.ProjectPath ?? "");
                    var targetApplicationDirectoryPath = new DirectoryInfo(projectDefinition.ProjectPath).Parent!.FullName;

                    UserDeploymentSettings? userDeploymentSettings = null;
                    if (!string.IsNullOrEmpty(input.Apply))
                    {
                        var applyPath = Path.GetFullPath(input.Apply, targetApplicationDirectoryPath);
                        userDeploymentSettings = UserDeploymentSettings.ReadSettings(applyPath);
                    }

                    var awsCredentials = await _awsUtilities.ResolveAWSCredentials(input.Profile ?? userDeploymentSettings?.AWSProfile);
                    var awsRegion = _awsUtilities.ResolveAWSRegion(input.Region ?? userDeploymentSettings?.AWSRegion);

                    _commandLineWrapper.RegisterAWSContext(awsCredentials, awsRegion);
                    _awsClientFactory.RegisterAWSContext(awsCredentials, awsRegion);

                    var callerIdentity = await _awsResourceQueryer.GetCallerIdentity();

                    var session = new OrchestratorSession(
                        projectDefinition,
                        awsCredentials,
                        awsRegion,
                        callerIdentity.Account)
                    {
                        AWSProfileName = input.Profile ?? userDeploymentSettings?.AWSProfile ?? null
                    };

                    var dockerEngine = new DockerEngine.DockerEngine(projectDefinition, _fileManager);

                    var deploy = new DeployCommand(
                        _toolInteractiveService,
                        _orchestratorInteractiveService,
                        _cdkProjectHandler,
                        _cdkManager,
                        _cdkVersionDetector,
                        _deploymentBundleHandler,
                        dockerEngine,
                        _awsResourceQueryer,
                        _templateMetadataReader,
                        _deployedApplicationQueryer,
                        _typeHintCommandFactory,
                        _displayedResourceHandler,
                        _cloudApplicationNameGenerator,
                        _localUserSettingsEngine,
                        _consoleUtilities,
                        _customRecipeLocator,
                        _systemCapabilityEvaluator,
                        session,
                        _directoryManager,
                        _fileManager);

                    var deploymentProjectPath = input.DeploymentProject ?? string.Empty;
                    if (!string.IsNullOrEmpty(deploymentProjectPath))
                    {
                        deploymentProjectPath = Path.GetFullPath(deploymentProjectPath, targetApplicationDirectoryPath);
                    }

                    await deploy.ExecuteAsync(input.StackName ?? "", deploymentProjectPath, userDeploymentSettings);

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
            var deleteCommand = new Command("delete-deployment", "Delete an existing deployment.");
            lock (s_child_command_lock)
            {
                deleteCommand.Add(_optionProfile);
                deleteCommand.Add(_optionRegion);
                deleteCommand.Add(_optionProjectPath);
                deleteCommand.Add(_optionDiagnosticLogging);
                deleteCommand.AddArgument(new Argument("deployment-name"));
            }

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

                    OrchestratorSession? session = null;

                    try
                    {
                        var projectDefinition = await _projectParserUtility.Parse(input.ProjectPath ?? string.Empty);

                        var callerIdentity = await _awsResourceQueryer.GetCallerIdentity();

                        session = new OrchestratorSession(
                            projectDefinition,
                            awsCredentials,
                            awsRegion,
                            callerIdentity.Account);
                    }
                    catch (FailedToFindDeployableTargetException) { }

                    await new DeleteDeploymentCommand(
                        _awsClientFactory,
                        _toolInteractiveService,
                        _consoleUtilities,
                        _localUserSettingsEngine,
                        session).ExecuteAsync(input.DeploymentName);

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
            var listCommand = new Command("list-deployments", "List existing deployments.");
            lock (s_child_command_lock)
            {
                listCommand.Add(_optionProfile);
                listCommand.Add(_optionRegion);
                listCommand.Add(_optionProjectPath);
                listCommand.Add(_optionDiagnosticLogging);
            }

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
                "Save the deployment project inside a user provided directory path without proceeding with a deployment");

            lock (s_child_command_lock)
            {
                generateDeploymentProjectCommand.Add(_optionOutputDirectory);
                generateDeploymentProjectCommand.Add(_optionDiagnosticLogging);
                generateDeploymentProjectCommand.Add(_optionProjectPath);
                generateDeploymentProjectCommand.Add(_optionProjectDisplayName);
            }

            generateDeploymentProjectCommand.Handler = CommandHandler.Create(async (GenerateDeploymentProjectCommandHandlerInput input) =>
            {
                try
                {
                    _toolInteractiveService.Diagnostics = input.Diagnostics;
                    var projectDefinition = await _projectParserUtility.Parse(input.ProjectPath ?? "");

                    var saveDirectory = input.Output;
                    var projectDisplayName = input.ProjectDisplayName;

                    OrchestratorSession session = new OrchestratorSession(projectDefinition);

                    var targetApplicationFullPath = new DirectoryInfo(projectDefinition.ProjectPath).FullName;

                    if (!string.IsNullOrEmpty(saveDirectory))
                    {
                        var targetApplicationDirectoryFullPath = new DirectoryInfo(targetApplicationFullPath).Parent!.FullName;
                        saveDirectory = Path.GetFullPath(saveDirectory, targetApplicationDirectoryFullPath);
                    }

                    var generateDeploymentProject = new GenerateDeploymentProjectCommand(
                        _toolInteractiveService,
                        _consoleUtilities,
                        _cdkProjectHandler,
                        _commandLineWrapper,
                        _directoryManager,
                        _fileManager,
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

            lock (s_child_command_lock)
            {
                deploymentProjectCommand.Add(generateDeploymentProjectCommand);
            }

            return deploymentProjectCommand;
        }

        private Command BuildServerModeCommand()
        {
            var serverModeCommand = new Command(
                "server-mode",
                "Launches the tool in a server mode for IDEs like Visual Studio to integrate with.");

            lock (s_child_command_lock)
            {
                serverModeCommand.Add(new Option<int>(new[] { "--port" }, description: "Port the server mode will listen to."));
                serverModeCommand.Add(new Option<int>(new[] { "--parent-pid" }, description: "The ID of the process that is launching server mode. Server mode will exit when the parent pid terminates."));
                serverModeCommand.Add(new Option<bool>(new[] { "--unsecure-mode" }, description: "If set the cli uses an unsecure mode without encryption."));
                serverModeCommand.Add(_optionDiagnosticLogging);
            }

            serverModeCommand.Handler = CommandHandler.Create(async (ServerModeCommandHandlerInput input) =>
            {
                try
                {
                    _toolInteractiveService.Diagnostics = input.Diagnostics;
                    var serverMode = new ServerModeCommand(_toolInteractiveService, input.Port, input.ParentPid, input.UnsecureMode);

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
