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

namespace AWS.Deploy.CLI.IntegrationTests.SaveCdkDeploymentProject
{
    public static class Utilities
    {
        public static async Task CreateCDKDeploymentProject(string targetApplicationPath, string saveDirectoryPath = null, bool isValid = true)
        {
            var (app, interactiveService) = GetAppServiceProvider();
            Assert.NotNull(app);
            Assert.NotNull(interactiveService);

            // Arrange input for saving the CDK deployment project
            await interactiveService.StdInWriter.WriteAsync(Environment.NewLine); // Select default recommendation

            string[] deployArgs;
            // default save directory
            if (string.IsNullOrEmpty(saveDirectoryPath))
            {
                saveDirectoryPath = targetApplicationPath + "CDK";
                deployArgs = new[] { "deployment-project", "generate", "--project-path", targetApplicationPath };
            }  
            else
            {
                deployArgs = new[] { "deployment-project", "generate", "--project-path", targetApplicationPath, "--output", saveDirectoryPath };
            }
                

            var returnCode = await app.Run(deployArgs);

            // Verify project is saved
            var stdOut = interactiveService.StdOutReader.ReadAllLines();
            var successMessage = $"The CDK deployment project is saved at: {saveDirectoryPath}";
            
            if (!isValid)
            {
                returnCode.ShouldEqual(CommandReturnCodes.USER_ERROR);
                return;
            }

            returnCode.ShouldEqual(CommandReturnCodes.SUCCESS);
            stdOut.ShouldContain(successMessage);

            VerifyCreatedArtifacts(targetApplicationPath, saveDirectoryPath);
        }

        public static async Task CreateCDKDeploymentProjectWithRecipeName(string targetApplicationPath, string recipeName, string option, string saveDirectoryPath = null, bool isValid = true)
        {
            var (app, interactiveService) = GetAppServiceProvider();
            Assert.NotNull(app);
            Assert.NotNull(interactiveService);

            // Arrange input for saving the CDK deployment project
            await interactiveService.StdInWriter.WriteAsync(option); // select recipe to save the CDK deployment project
            await interactiveService.StdInWriter.FlushAsync();


            string[] deployArgs;
            // default save directory
            if (string.IsNullOrEmpty(saveDirectoryPath))
            {
                saveDirectoryPath = targetApplicationPath + "CDK";
                deployArgs = new[] { "deployment-project", "generate", "--project-path", targetApplicationPath, "--project-display-name", recipeName};
            }
            else
            {
                deployArgs = new[] { "deployment-project", "generate", "--project-path", targetApplicationPath, "--output", saveDirectoryPath, "--project-display-name", recipeName };
            }


            var returnCode = await app.Run(deployArgs);

            // Verify project is saved
            var stdOut = interactiveService.StdOutReader.ReadAllLines();
            var successMessage = $"The CDK deployment project is saved at: {saveDirectoryPath}";

            if (!isValid)
            {
                returnCode.ShouldEqual(CommandReturnCodes.USER_ERROR);
                return;
            }

            returnCode.ShouldEqual(CommandReturnCodes.SUCCESS);
            stdOut.ShouldContain(successMessage);

            VerifyCreatedArtifacts(targetApplicationPath, saveDirectoryPath);
        }

        public static string ResolvePathToTestApps()
        {
            var testsPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            while (testsPath != null && !string.Equals(new DirectoryInfo(testsPath).Name, "test", StringComparison.OrdinalIgnoreCase))
            {
                testsPath = Directory.GetParent(testsPath).FullName;
            }
            return new DirectoryInfo(Path.Combine(testsPath, "..", "testapps")).FullName;
        }

        private static (App app, InMemoryInteractiveService interactiveService) GetAppServiceProvider()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddCustomServices();
            serviceCollection.AddTestServices();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var app = serviceProvider.GetService<App>();
            var interactiveService = serviceProvider.GetService<InMemoryInteractiveService>();
            return (app, interactiveService);
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
