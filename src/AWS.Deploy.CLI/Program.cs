// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using AWS.Deploy.CLI.Commands;
using AWS.Deploy.CLI.Utilities;
using AWS.Deploy.Orchestrator;
using AWS.Deploy.Orchestrator.Utilities;
using AWS.DeploymentCommon;

namespace AWS.Deploy.CLI
{
    internal class Program
    {
        private static readonly IToolInteractiveService _toolInteractiveService = new ConsoleInteractiveServiceImpl();

        private static readonly Option<string> _optionProfile = new Option<string>("--profile", "AWS credential profile used to make calls to AWS");
        private static readonly Option<string> _optionRegion = new Option<string>("--region", "AWS region to deploy application to. For example us-west-2.");
        private static readonly Option<string> _optionProjectPath = new Option<string>("--project-path", getDefaultValue: () => Directory.GetCurrentDirectory(), description: "Path to the project to deploy");
        private static readonly Option<bool> _optionSaveCdkProject = new Option<bool>("--save-cdk-project", getDefaultValue: () => false, description: "Save generated CDK project in solution to customize");

        private static async Task<int> Main(string[] args)
        {
            _toolInteractiveService.WriteLine("AWS .NET Suite for deploying .NET Core applications to AWS");
            _toolInteractiveService.WriteLine("Project Home: https://github.com/aws/aws-dotnet-suite-tooling");
            _toolInteractiveService.WriteLine(string.Empty);

            var rootCommand = new RootCommand { Description = "The AWS .NET Suite for getting .NET applications running on AWS." };

           var deployCommand = new Command(
                "deploy",
                "Inspect the .NET project and deploy the application to AWS to the appropriate AWS service.")
            {
                _optionProfile,
                _optionRegion,
                _optionProjectPath,
                _optionSaveCdkProject
            };

            deployCommand.Handler = CommandHandler.Create<string, string, string, bool>(async (profile, region, projectPath, saveCdkProject) =>
            {
                try
                {
                    var orchestratorInteractiveService = new ConsoleOrchestratorLogger(_toolInteractiveService);

                    var previousSettings = PreviousDeploymentSettings.ReadSettings(projectPath, null);

                    var awsUtilities = new AWSUtilities(_toolInteractiveService);
                    var awsCredentials = awsUtilities.ResolveAWSCredentials(profile, previousSettings.Profile);
                    var awsRegion = awsUtilities.ResolveAWSRegion(region, previousSettings.Region);

                    var commandLineWrapper =
                        new CommandLineWrapper(
                            orchestratorInteractiveService,
                            awsCredentials,
                            awsRegion);

                    var systemCapabilityEvaluator = new SystemCapabilityEvaluator(commandLineWrapper);
                    var systemCapabilities = await systemCapabilityEvaluator.Evaluate();

                    var session = new OrchestratorSession
                    {
                        AWSProfileName = profile,
                        AWSCredentials = awsCredentials,
                        AWSRegion = awsRegion,
                        ProjectPath = projectPath,
                        ProjectDirectory = projectPath,
                        SystemCapabilities = systemCapabilities
                    };

                    var deploy = new DeployCommand(
                        new DefaultAWSClientFactory(),
                        _toolInteractiveService,
                        orchestratorInteractiveService,
                        new CdkProjectHandler(orchestratorInteractiveService, commandLineWrapper),
                        session);

                    await deploy.ExecuteAsync(saveCdkProject);

                    return CommandReturnCodes.SUCCESS;
                }
                catch (Exception e) when (e.IsAWSDeploymentExpectedException())
                {
                    // helpful error message should have already been presented to the user,
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
            rootCommand.Add(deployCommand);

            var setupCICDCommand = new Command("setup-cicd", "Configure the project to be deployed to AWS using the AWS Code services") { _optionProfile, _optionRegion, _optionProjectPath, };
            setupCICDCommand.Handler = CommandHandler.Create<string>(SetupCICD);
            rootCommand.Add(setupCICDCommand);

            var inspectIAMPermissionsCommand = new Command("inspect-permissions", "Inspect the project to see what AWS permissions the application needs to access AWS services the application is using.") { _optionProjectPath };
            inspectIAMPermissionsCommand.Handler = CommandHandler.Create<string>(InspectIAMPermissions);
            rootCommand.Add(inspectIAMPermissionsCommand);

            var listCommand = new Command("list-stacks", "List CloudFormation stacks.")
            {
                _optionProfile,
                _optionRegion,
                _optionProjectPath,
            };
            listCommand.Handler = CommandHandler.Create<string, string, string>(async (profile, region, projectPath) =>
            {
                var awsUtilities = new AWSUtilities(_toolInteractiveService);

                var previousSettings = PreviousDeploymentSettings.ReadSettings(projectPath, null);

                var awsCredentials = awsUtilities.ResolveAWSCredentials(profile, previousSettings.Profile);
                var awsRegion = awsUtilities.ResolveAWSRegion(region, previousSettings.Region);

                var session = new OrchestratorSession
                {
                    AWSProfileName = profile,
                    AWSCredentials = awsCredentials,
                    AWSRegion = awsRegion,
                    ProjectPath = projectPath,
                    ProjectDirectory = projectPath
                };

                await new ListStacksCommand(new DefaultAWSClientFactory(), _toolInteractiveService, session).ExecuteAsync();
            });
            rootCommand.Add(listCommand);

            return await rootCommand.InvokeAsync(args);
        }

        private static void SetupCICD(string projectPath)
        {
            _toolInteractiveService.WriteLine("TODO: Make this work");
        }

        private static void InspectIAMPermissions(string projectPath)
        {
            _toolInteractiveService.WriteLine("TODO: Make this work");
        }
    }
}
