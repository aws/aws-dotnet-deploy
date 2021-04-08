// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.CommandLine;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AWS.Deploy.CLI.Commands;

namespace AWS.Deploy.CLI
{
    public class App
    {
        private readonly ICommandFactory _commandFactory;
        private readonly IToolInteractiveService _toolInteractiveService;

        public App(ICommandFactory commandFactory, IToolInteractiveService toolInteractiveService)
        {
            _commandFactory = commandFactory;
            _toolInteractiveService = toolInteractiveService;
        }

        public async Task<int> Run(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            SetExecutionEnvironment();

            _toolInteractiveService.WriteLine("AWS .NET deployment tool for deploying .NET Core applications to AWS.");
            _toolInteractiveService.WriteLine("Project Home: https://github.com/aws/aws-dotnet-deploy");
            _toolInteractiveService.WriteLine(string.Empty);

            // if user didn't specify a command, default to help
            if (args.Length == 0)
            {
                args = new[] { "-h" };
            }

            return await _commandFactory.BuildRootCommand().InvokeAsync(args);
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
