// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Deploy.Common.IO;
using AWS.Deploy.Common;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.IO;

namespace AWS.Deploy.DockerImageUploader
{
    /// <summary>
    /// This serves as the dependency injection container for the console application.
    /// </summary>
    public class App
    {
        private readonly IFileManager _fileManager;
        private readonly IDirectoryManager _directoryManager;
        private readonly IProjectDefinitionParser _projectDefinitionParser;
        private readonly CLI.App _deployToolCli;

        private readonly List<string> _testApps = new() { "WebApiNET6", "ConsoleAppTask" };

        public App(IServiceProvider serviceProvider)
        {
            _projectDefinitionParser = serviceProvider.GetRequiredService<IProjectDefinitionParser>();
            _fileManager = serviceProvider.GetRequiredService<IFileManager>();
            _directoryManager = serviceProvider.GetRequiredService<IDirectoryManager>();
            _deployToolCli = serviceProvider.GetRequiredService<CLI.App>();
        }

        /// <summary>
        /// Generates Dockerfiles for test applications using
        /// the <see href="https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.DockerEngine/Templates/Dockerfile.template">Dockerfile template</see>.
        /// It will then build and push the images to Amazon ECR where they are continuously scanned for security vulnerabilities.
        /// </summary>
        public async Task Run()
        {
            foreach (var testApp in _testApps)
            {
                var projectPath = ResolvePath(testApp);
                await CreateImageAndPushToECR(projectPath);
            }
        }

        private async Task CreateImageAndPushToECR(string projectPath)
        {
            var projectDefinition = await _projectDefinitionParser.Parse(projectPath);

            var dockerEngine = new DockerEngine.DockerEngine(projectDefinition, _fileManager, _directoryManager);
            dockerEngine.GenerateDockerFile();

            var configFilePath = Path.Combine(projectPath, "DockerImageUploaderConfigFile.json");
            var deployArgs = new[] { "deploy", "--project-path", projectPath, "--diagnostics", "--apply", configFilePath, "--silent" };
            await _deployToolCli.Run(deployArgs);
        }

        private string ResolvePath(string projectName)
        {
            const string testDir = "test";
            var testDirPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            while (testDirPath != null && !string.Equals(new DirectoryInfo(testDirPath).Name, testDir, StringComparison.OrdinalIgnoreCase))
            {
                testDirPath = Directory.GetParent(testDirPath)?.FullName;
            }

            if (string.IsNullOrEmpty(testDirPath))
            {
                throw new Exception($"Failed to find path to '{testDir}' directory.");
            }

            return Path.Combine(testDirPath, "..", "testapps", projectName);
        }
    }
}
