// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.CLI.Commands;
using AWS.Deploy.CLI.Utilities;
using AWS.Deploy.Orchestrator;
using AWS.Deploy.Orchestrator.Data;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;

namespace AWS.Deploy.CLI
{
    internal class Program
    {
        private static readonly Option<string> _optionProfile = new Option<string>("--profile", "AWS credential profile used to make calls to AWS");
        private static readonly Option<string> _optionRegion = new Option<string>("--region", "AWS region to deploy application to. For example us-west-2.");
        private static readonly Option<string> _optionProjectPath = new Option<string>("--project-path", getDefaultValue: () => Directory.GetCurrentDirectory(), description: "Path to the project to deploy");
        private static readonly Option<bool> _optionSaveCdkProject = new Option<bool>("--save-cdk-project", getDefaultValue: () => false, description: "Save generated CDK project in solution to customize");
        private static readonly Option<bool> _optionDiagnosticLogging = new Option<bool>(new []{"-d", "--diagnostics"}, description: "Enables diagnostic output");

        private static async Task<int> Main(string[] args)
        {
            var preambleWriter = new ConsoleInteractiveServiceImpl(diagnosticLoggingEnabled: false);

            preambleWriter.WriteLine("AWS .NET Suite for deploying .NET Core applications to AWS");
            preambleWriter.WriteLine("Project Home: https://github.com/aws/aws-dotnet-deploy");
            preambleWriter.WriteLine(string.Empty);

            var rootCommand = new RootCommand { Description = "The AWS .NET Suite for getting .NET applications running on AWS." };

           var deployCommand = new Command(
                "deploy",
                "Inspect the .NET project and deploy the application to AWS to the appropriate AWS service.")
            {
                _optionProfile,
                _optionRegion,
                _optionProjectPath,
                _optionSaveCdkProject,
                _optionDiagnosticLogging
            };

            deployCommand.Handler = CommandHandler.Create<string, string, string, bool, bool>(async (profile, region, projectPath, saveCdkProject, diagnostics) =>
            {
                var toolInteractiveService = new ConsoleInteractiveServiceImpl(diagnostics);

                try
                {
                    var orchestratorInteractiveService = new ConsoleOrchestratorLogger(toolInteractiveService);

                    var previousSettings = PreviousDeploymentSettings.ReadSettings(projectPath, null);

                    var awsUtilities = new AWSUtilities(toolInteractiveService);
                    var awsCredentials = await awsUtilities.ResolveAWSCredentials(profile, previousSettings.Profile);
                    var awsRegion = awsUtilities.ResolveAWSRegion(region, previousSettings.Region);

                    var commandLineWrapper =
                        new CommandLineWrapper(
                            orchestratorInteractiveService,
                            awsCredentials,
                            awsRegion);

                    var systemCapabilityEvaluator = new SystemCapabilityEvaluator(commandLineWrapper);
                    var systemCapabilities = systemCapabilityEvaluator.Evaluate();

                    var stsClient = new AmazonSecurityTokenServiceClient(awsCredentials);
                    var callerIdentity = await stsClient.GetCallerIdentityAsync(new GetCallerIdentityRequest());

                    var session = new OrchestratorSession
                    {
                        AWSProfileName = profile,
                        AWSCredentials = awsCredentials,
                        AWSRegion = awsRegion,
                        AWSAccountId = callerIdentity.Account,
                        ProjectPath = projectPath,
                        ProjectDirectory = projectPath,
                        SystemCapabilities = systemCapabilities
                    };

                    var deploy = new DeployCommand(
                        toolInteractiveService,
                        orchestratorInteractiveService,
                        new CdkProjectHandler(orchestratorInteractiveService, commandLineWrapper),
                        new AWSResourceQueryer(new DefaultAWSClientFactory()),
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
                    toolInteractiveService.WriteErrorLine(
                        "Unhandled exception.  This is a bug.  Please copy the stack trace below and file a bug at https://github.com/aws/aws-dotnet-deploy. " + 
                        e.PrettyPrint());

                    return CommandReturnCodes.UNHANDLED_EXCEPTION;
                }
            });
            rootCommand.Add(deployCommand);

            var listCommand = new Command("list-stacks", "List CloudFormation stacks.")
            {
                _optionProfile,
                _optionRegion,
                _optionProjectPath,
                _optionDiagnosticLogging
            };
            listCommand.Handler = CommandHandler.Create<string, string, string, bool>(async (profile, region, projectPath, diagnostics) =>
            {
                var toolInteractiveService = new ConsoleInteractiveServiceImpl(diagnostics);

                var awsUtilities = new AWSUtilities(toolInteractiveService);

                var previousSettings = PreviousDeploymentSettings.ReadSettings(projectPath, null);

                var awsCredentials = await awsUtilities.ResolveAWSCredentials(profile, previousSettings.Profile);
                var awsRegion = awsUtilities.ResolveAWSRegion(region, previousSettings.Region);

                var stsClient = new AmazonSecurityTokenServiceClient(awsCredentials);
                var callerIdentity = await stsClient.GetCallerIdentityAsync(new GetCallerIdentityRequest());

                var session = new OrchestratorSession
                {
                    AWSProfileName = profile,
                    AWSCredentials = awsCredentials,
                    AWSRegion = awsRegion,
                    AWSAccountId = callerIdentity.Account,
                    ProjectPath = projectPath,
                    ProjectDirectory = projectPath
                };

                await new ListStacksCommand(new DefaultAWSClientFactory(), toolInteractiveService, session).ExecuteAsync();
            });
            rootCommand.Add(listCommand);

            var deleteCommand = new Command("delete-stack", "Deletes a CloudFormation stack.")
            {
                _optionProfile,
                _optionRegion,
                _optionProjectPath,
                new Option<string>("--stack-name", "The name or the unique stack ID that is associated with the stack.")
                {
                    IsRequired = true,
                },
                _optionDiagnosticLogging
            };
            deleteCommand.Handler = CommandHandler.Create<string, string, string, string, bool>(async (profile, region, projectPath, stackName, diagnostics) =>
            {
                var toolInteractiveService = new ConsoleInteractiveServiceImpl(diagnostics);
                var awsUtilities = new AWSUtilities(toolInteractiveService);

                var previousSettings = PreviousDeploymentSettings.ReadSettings(projectPath, null);

                var awsCredentials = await awsUtilities.ResolveAWSCredentials(profile, previousSettings.Profile);
                var awsRegion = awsUtilities.ResolveAWSRegion(region, previousSettings.Region);

                var session = new OrchestratorSession
                {
                    AWSProfileName = profile,
                    AWSCredentials = awsCredentials,
                    AWSRegion = awsRegion,
                };

                await new DeleteStackCommand(new DefaultAWSClientFactory(), toolInteractiveService, session).ExecuteAsync(stackName);

                return CommandReturnCodes.SUCCESS;
            });
            rootCommand.Add(deleteCommand);

            return await rootCommand.InvokeAsync(args);
        }
    }
}
