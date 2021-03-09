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
using AWS.Deploy.Common.IO;
using System.Reflection;
using System.Linq;
using System.Text;
using AWS.Deploy.Common.Extensions;
using AWS.Deploy.Orchestrator.CDK;
using AWS.Deploy.Common.IO;

namespace AWS.Deploy.CLI
{
    internal class Program
    {
        private static readonly Option<string> _optionProfile = new Option<string>("--profile", "AWS credential profile used to make calls to AWS");
        private static readonly Option<string> _optionRegion = new Option<string>("--region", "AWS region to deploy application to. For example us-west-2.");
        private static readonly Option<string> _optionProjectPath = new Option<string>("--project-path", getDefaultValue: () => Directory.GetCurrentDirectory(), description: "Path to the project to deploy");
        private static readonly Option<bool> _optionDiagnosticLogging = new Option<bool>(new []{"-d", "--diagnostics"}, description: "Enable diagnostic output");

        private static async Task<int> Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            SetExecutionEnvironment();

            var preambleWriter = new ConsoleInteractiveServiceImpl(diagnosticLoggingEnabled: false);

            preambleWriter.WriteLine("AWS .NET deployment tool for deploying .NET Core applications to AWS");
            preambleWriter.WriteLine("Project Home: https://github.com/aws/aws-dotnet-deploy");
            preambleWriter.WriteLine(string.Empty);

            // Name is important to set here to show correctly in the CLI usage help.
            // Either dotnet-aws or dotnet aws works from the CLI. System.Commandline's help system does not like having a space with dotnet aws.
            var rootCommand = new RootCommand {Name = "dotnet-aws",  Description = "The AWS .NET deployment tool for deploying .NET applications on AWS." };

           var deployCommand = new Command(
                "deploy",
                "Inspect, build, and deploy the .NET project to AWS using the recommended AWS service.")
            {
                _optionProfile,
                _optionRegion,
                _optionProjectPath,
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

                    var fileManager = new FileManager();
                    var packageJsonGenerator = new PackageJsonGenerator(
                        typeof(PackageJsonGenerator).Assembly
                        .ReadEmbeddedFile(PackageJsonGenerator.TemplateIdentifier));
                    var npmPackageInitializer = new NPMPackageInitializer(commandLineWrapper, packageJsonGenerator, fileManager);
                    var cdkInstaller = new CDKInstaller(commandLineWrapper);
                    var cdkManager = new CDKManager(cdkInstaller, npmPackageInitializer);

                    var systemCapabilityEvaluator = new SystemCapabilityEvaluator(commandLineWrapper, cdkManager);
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
                        SystemCapabilities = systemCapabilities,
                        CdkManager = cdkManager
                    };

                    var awsResourceQueryer = new AWSResourceQueryer(new DefaultAWSClientFactory());
                    var directoryManager = new DirectoryManager();
                    var zipFileManager = new ZipFileManager();

                    var deploy = new DeployCommand(
                        toolInteractiveService,
                        orchestratorInteractiveService,
                        new CdkProjectHandler(orchestratorInteractiveService, commandLineWrapper),
                        new DeploymentBundleHandler(session, commandLineWrapper, awsResourceQueryer, orchestratorInteractiveService, directoryManager, zipFileManager),
                        awsResourceQueryer,
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

            var listCommand = new Command("list-deployments", "List existing deployments.")
            {
                _optionProfile,
                _optionRegion,
                _optionProjectPath,
                _optionDiagnosticLogging
            };
            listCommand.Handler = CommandHandler.Create<string, string, string, bool>(async (profile, region, projectPath, diagnostics) =>
            {
                var toolInteractiveService = new ConsoleInteractiveServiceImpl(diagnostics);
                var orchestratorInteractiveService = new ConsoleOrchestratorLogger(toolInteractiveService);

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

                var commandLineWrapper =
                    new CommandLineWrapper(
                        orchestratorInteractiveService,
                        awsCredentials,
                        awsRegion);

                var awsResourceQueryer = new AWSResourceQueryer(new DefaultAWSClientFactory());
                var directoryManager = new DirectoryManager();
                var zipFileManager = new ZipFileManager();

                await new ListDeploymentsCommand(toolInteractiveService,
                                                new ConsoleOrchestratorLogger(toolInteractiveService),
                                                new CdkProjectHandler(orchestratorInteractiveService, commandLineWrapper),
                                                new DeploymentBundleHandler(session, commandLineWrapper, awsResourceQueryer, orchestratorInteractiveService, directoryManager, zipFileManager),
                                                awsResourceQueryer,
                                                session).ExecuteAsync();
            });
            rootCommand.Add(listCommand);

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

                await new DeleteDeploymentCommand(new DefaultAWSClientFactory(), toolInteractiveService, session).ExecuteAsync(deploymentName);

                return CommandReturnCodes.SUCCESS;
            });
            rootCommand.Add(deleteCommand);

            return await rootCommand.InvokeAsync(args);
        }

        /// <summary>
        /// Set up the execution environment variable picked up by the AWS .NET SDK. This can be useful for identify calls
        /// made by this tool in AWS CloudTrail.
        /// </summary>
        private static void SetExecutionEnvironment()
        {
            const string envName = "AWS_EXECUTION_ENV";
            const string awsDotnetDeployCLI = "aws-dotnet-deploy-cli";

            var assemblyVersion = typeof(Program).Assembly
                .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
                .FirstOrDefault()
                as AssemblyInformationalVersionAttribute;

            var envValue = new StringBuilder();

            // If there is an existing execution environment variable add this tool as a suffix.
            if(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envName)))
            {
                envValue.Append($"{Environment.GetEnvironmentVariable(envName)}_");
            }

            envValue.Append($"{awsDotnetDeployCLI}_{assemblyVersion?.InformationalVersion}");

            Environment.SetEnvironmentVariable(envName, envValue.ToString());
        }
    }
}
