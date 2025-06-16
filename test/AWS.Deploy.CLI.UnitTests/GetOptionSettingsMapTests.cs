// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.CLI.UnitTests.Utilities;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Recipes.Validation;
using AWS.Deploy.Orchestration;
using Moq;
using Xunit;

namespace AWS.Deploy.CLI.UnitTests
{
    public class GetOptionSettingsMapTests
    {
        private readonly IOptionSettingHandler _optionSettingHandler;
        private readonly Mock<IServiceProvider> _serviceProvider;
        private readonly IDirectoryManager _directoryManager;
        private readonly IFileManager _fileManager;
        private readonly IProjectDefinitionParser _projectDefinitionParser;

        public GetOptionSettingsMapTests()
        {
            _serviceProvider = new Mock<IServiceProvider>();
            _optionSettingHandler = new OptionSettingHandler(new ValidatorFactory(_serviceProvider.Object));
            _directoryManager = new DirectoryManager();
            _fileManager = new FileManager();
            _projectDefinitionParser = new ProjectDefinitionParser(_fileManager, _directoryManager);
        }

        [Fact]
        public async Task GetOptionSettingsMap()
        {
            // ARRANGE - select recommendation
            var engine = await HelperFunctions.BuildRecommendationEngine(
                "WebAppWithDockerFile",
                _fileManager,
                _directoryManager,
                "us-west-2",
                "123456789012",
                "default"
            );
            var recommendations = await engine.ComputeRecommendations();
            var selectedRecommendation = recommendations.First(x => string.Equals(x.Recipe.Id, "AspNetAppAppRunner"));

            // ARRANGE - get project definition
            var projectPath = SystemIOUtilities.ResolvePath("WebAppWithDockerFile");
            var projectDefinition = await _projectDefinitionParser.Parse(projectPath);

            // ARRANGE - Modify option setting items
            await _optionSettingHandler.SetOptionSettingValue(selectedRecommendation, "ServiceName", "MyAppRunnerService", true);
            await _optionSettingHandler.SetOptionSettingValue(selectedRecommendation, "Port", "100", true);
            await _optionSettingHandler.SetOptionSettingValue(selectedRecommendation, "ECRRepositoryName", "my-ecr-repository", true);
            await _optionSettingHandler.SetOptionSettingValue(selectedRecommendation, "DockerfilePath", Path.Combine(projectPath, "Dockerfile"), true);
            await _optionSettingHandler.SetOptionSettingValue(selectedRecommendation, "DockerExecutionDirectory", projectPath, true);

            // ACT and ASSERT - OptionSettingType.All
            var container = _optionSettingHandler.GetOptionSettingsMap(selectedRecommendation, projectDefinition, _directoryManager);
            Assert.Equal("MyAppRunnerService", container["ServiceName"]);
            Assert.Equal(100, container["Port"]);
            Assert.Equal("my-ecr-repository", container["ECRRepositoryName"]);
            Assert.Equal("Dockerfile", container["DockerfilePath"]); // path relative to projectPath
            Assert.Equal(".", container["DockerExecutionDirectory"]); // path relative to projectPath

            // ACT and ASSERT - OptionSettingType.Recipe
            container = _optionSettingHandler.GetOptionSettingsMap(selectedRecommendation, projectDefinition, _directoryManager, OptionSettingsType.Recipe);
            Assert.Equal("MyAppRunnerService", container["ServiceName"]);
            Assert.Equal(100, container["Port"]);
            Assert.False(container.ContainsKey("Dockerfile"));
            Assert.False(container.ContainsKey("DockerExecutionDirectory"));
            Assert.False(container.ContainsKey("ECRRepositoryName"));

            // ACT and ASSERT - OptionSettingType.DeploymentBundle
            container = _optionSettingHandler.GetOptionSettingsMap(selectedRecommendation, projectDefinition, _directoryManager, OptionSettingsType.DeploymentBundle);
            Assert.Equal("my-ecr-repository", container["ECRRepositoryName"]);
            Assert.Equal("Dockerfile", container["DockerfilePath"]); // path relative to projectPath
            Assert.Equal(".", container["DockerExecutionDirectory"]); // path relative to projectPath
            Assert.False(container.ContainsKey("ServiceName"));
            Assert.False(container.ContainsKey("Port"));
        }
    }
}
