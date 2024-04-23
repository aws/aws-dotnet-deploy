// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
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

            SetExecutionEnvironment(args);

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
        private static void SetExecutionEnvironment(string[] args)
        {
            const string envName = "AWS_EXECUTION_ENV";

            var toolVersion = GetToolVersion();

            // The leading and trailing whitespaces are intentional
            var userAgent = $" lib/aws-dotnet-deploy-cli#{toolVersion} ";
            if (args?.Length > 0)
            {
                // The trailing whitespace is intentional
                userAgent = $"{userAgent}md/cli-args#{args[0]} ";
            }


            var envValue = new StringBuilder();
            var existingValue = Environment.GetEnvironmentVariable(envName);

            // If there is an existing execution environment variable add this tool as a suffix.
            if (!string.IsNullOrEmpty(existingValue))
            {
                envValue.Append(existingValue);
            }

            envValue.Append(userAgent);

            Environment.SetEnvironmentVariable(envName, envValue.ToString());
        }

        private static string GetToolVersion()
        {
            var assembly = typeof(App).GetTypeInfo().Assembly;
            var version = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
            if (version is null)
            {
                return string.Empty;
            }

            var versionParts = version.Split('.');
            if (versionParts.Length == 4)
            {
                versionParts[3] = "0";
            }

            return string.Join(".", versionParts);
        }
    }
}
