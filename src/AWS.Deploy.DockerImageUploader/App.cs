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
    public class App
    {
        private readonly IFileManager _fileManager;
        private readonly IDirectoryManager _directoryManager;
        private readonly IProjectDefinitionParser _projectDefinitionParser;
        private readonly CLI.App _deployToolCli;

        private readonly List<string> _testApps = new List<string> { "WebApiNET6", "ConsoleAppTask" };

        public App(IServiceProvider serviceProvider)
        {
            _projectDefinitionParser = serviceProvider.GetRequiredService<IProjectDefinitionParser>();
            _fileManager = serviceProvider.GetRequiredService<IFileManager>();
            _directoryManager = serviceProvider.GetRequiredService<IDirectoryManager>();
            _deployToolCli = serviceProvider.GetRequiredService<CLI.App>();
        }

        public async Task Run(string[] args)
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
            const string srcDir = "src";
            var srcDirPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            while (srcDirPath != null && !string.Equals(new DirectoryInfo(srcDirPath).Name, srcDir, StringComparison.OrdinalIgnoreCase))
            {
                srcDirPath = Directory.GetParent(srcDirPath)?.FullName;
            }

            if (string.IsNullOrEmpty(srcDirPath))
            {
                throw new Exception($"Failed to find path to '{srcDir}' directory.");
            }

            return Path.Combine(srcDirPath, "..", "testapps", projectName);
        }
    }
}
