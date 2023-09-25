// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.ECR.Model;
using Amazon.ECR;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.IO;

namespace AWS.Deploy.DockerImageUploader
{
    public class App
    {
        private readonly IFileManager _fileManager;
        private readonly IDirectoryManager _directoryManager;
        private readonly IProjectDefinitionParser _projectDefinitionParser;
        private readonly IAWSClientFactory _awsClientFactory;
        private readonly CLI.App _deployToolCli;

        private readonly List<string> _testApps = new List<string> { "webappnodockerfile" };

        public App(IServiceProvider serviceProvider)
        {
            _projectDefinitionParser = serviceProvider.GetRequiredService<IProjectDefinitionParser>();
            _fileManager = serviceProvider.GetRequiredService<IFileManager>();
            _directoryManager = serviceProvider.GetRequiredService<IDirectoryManager>();
            _awsClientFactory = serviceProvider.GetRequiredService<IAWSClientFactory>();
            _deployToolCli = serviceProvider.GetRequiredService<CLI.App>();
        }

        public async Task Run(string[] args)
        {
            await SetupContinousRegistryScanning();

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

        private async Task SetupContinousRegistryScanning()
        {
            var ecrClient = _awsClientFactory.GetAWSClient<IAmazonECR>();
            await ecrClient.PutRegistryScanningConfigurationAsync(new PutRegistryScanningConfigurationRequest
            {
                ScanType = ScanType.ENHANCED,
                Rules = new List<RegistryScanningRule>
                {
                    new RegistryScanningRule
                    {
                        ScanFrequency = ScanFrequency.CONTINUOUS_SCAN,
                        RepositoryFilters = new List<ScanningRepositoryFilter>
                        {
                            new ScanningRepositoryFilter
                            {
                                FilterType = ScanningRepositoryFilterType.WILDCARD,
                                Filter = "*",
                            }
                        }
                    }
                }
            });
        }

        private string ResolvePath(string projectName)
        {
            const string srcDir = "src";
            var srcDirPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            while (srcDirPath != null && !string.Equals(new DirectoryInfo(srcDirPath).Name, srcDir, StringComparison.OrdinalIgnoreCase))
            {
                srcDirPath = Directory.GetParent(srcDirPath)!.FullName;
            }

            return Path.Combine(srcDirPath!, "..", "testapps", projectName);
        }
    }
}
