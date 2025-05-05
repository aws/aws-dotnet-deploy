// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AWS.Deploy.CLI.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Should;
using System.Reflection;
using AWS.Deploy.CLI.Commands;
using AWS.Deploy.CLI.Utilities;
using Spectre.Console.Cli;

namespace AWS.Deploy.CLI.IntegrationTests.SaveCdkDeploymentProject
{
    public static class Utilities
    {
        public static async Task CreateCDKDeploymentProject(string targetApplicationPath, string? saveDirectoryPath = null, bool isValid = true)
        {
            var serviceCollection = GetAppServiceCollection();

            string[] deployArgs;
            // default save directory
            if (string.IsNullOrEmpty(saveDirectoryPath))
            {
                saveDirectoryPath = targetApplicationPath + ".Deployment";
                deployArgs = new[] { "deployment-project", "generate", "--project-path", targetApplicationPath, "--diagnostics" };
            }
            else
            {
                deployArgs = new[] { "deployment-project", "generate", "--project-path", targetApplicationPath, "--output", saveDirectoryPath, "--diagnostics" };
            }

            InMemoryInteractiveService interactiveService = null!;
            var returnCode = await serviceCollection.RunDeployToolAsync(deployArgs,
                provider =>
                {
                    interactiveService = provider.GetRequiredService<InMemoryInteractiveService>();

                    // Arrange input for saving the CDK deployment project
                    interactiveService.StdInWriter.WriteLine(Environment.NewLine); // Select default recommendation
                    interactiveService.StdInWriter.Flush();
                });

            // Verify project is saved
            var stdOut = interactiveService.StdOutReader.ReadAllLines();
            var successMessage = $"Saving AWS CDK deployment project to: {saveDirectoryPath}";

            if (!isValid)
            {
                returnCode.ShouldEqual(CommandReturnCodes.USER_ERROR);
                return;
            }

            returnCode.ShouldEqual(CommandReturnCodes.SUCCESS);
            stdOut.ShouldContain(successMessage);

            VerifyCreatedArtifacts(targetApplicationPath, saveDirectoryPath);
        }

        public static async Task CreateCDKDeploymentProjectWithRecipeName(string targetApplicationPath, string recipeName, string option, string? saveDirectoryPath = null, bool isValid = true, bool underSourceControl = true)
        {
            var serviceCollection = GetAppServiceCollection();

            string[] deployArgs;
            // default save directory
            if (string.IsNullOrEmpty(saveDirectoryPath))
            {
                saveDirectoryPath = targetApplicationPath + ".Deployment";
                deployArgs = new[] { "deployment-project", "generate", "--project-path", targetApplicationPath, "--project-display-name", recipeName, "--diagnostics"};
            }
            else
            {
                deployArgs = new[] { "deployment-project", "generate", "--project-path", targetApplicationPath, "--output", saveDirectoryPath, "--project-display-name", recipeName, "--diagnostics" };
            }

            InMemoryInteractiveService interactiveService = null!;
            var returnCode = await serviceCollection.RunDeployToolAsync(deployArgs,
                provider =>
                {
                    interactiveService = provider.GetRequiredService<InMemoryInteractiveService>();

                    // Arrange input for saving the CDK deployment project
                    interactiveService.StdInWriter.WriteLine(option); // select recipe to save the CDK deployment project
                    if (!underSourceControl)
                    {
                        interactiveService.StdInWriter.Write("y"); // proceed to save without source control.
                    }
                    interactiveService.StdInWriter.Flush();
                });

            // Verify project is saved
            var stdOut = interactiveService.StdOutReader.ReadAllLines();
            var successMessage = $"Saving AWS CDK deployment project to: {saveDirectoryPath}";

            if (!isValid)
            {
                returnCode.ShouldEqual(CommandReturnCodes.USER_ERROR);
                return;
            }

            returnCode.ShouldEqual(CommandReturnCodes.SUCCESS);
            stdOut.ShouldContain(successMessage);

            VerifyCreatedArtifacts(targetApplicationPath, saveDirectoryPath);
        }

        private static IServiceCollection GetAppServiceCollection()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddCustomServices();
            serviceCollection.AddTestServices();

            return serviceCollection;
        }

        private static void VerifyCreatedArtifacts(string targetApplicationPath, string saveDirectoryPath)
        {
            var saveDirectoryName = new DirectoryInfo(saveDirectoryPath).Name;

            Assert.True(Directory.Exists(saveDirectoryPath));
            Assert.True(File.Exists(Path.Combine(saveDirectoryPath, "AppStack.cs")));
            Assert.True(File.Exists(Path.Combine(saveDirectoryPath, "Program.cs")));
            Assert.True(File.Exists(Path.Combine(saveDirectoryPath, "cdk.json")));
            Assert.True(File.Exists(Path.Combine(saveDirectoryPath, $"{saveDirectoryName}.recipe")));
            Assert.True(File.Exists(Path.Combine(targetApplicationPath, "aws-deployments.json")));
        }
    }
}
