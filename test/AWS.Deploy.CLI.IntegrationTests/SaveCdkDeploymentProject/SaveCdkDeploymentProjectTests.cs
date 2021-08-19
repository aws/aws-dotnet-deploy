// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Linq;
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.CLI.Utilities;
using Xunit;
using Task = System.Threading.Tasks.Task;
using Newtonsoft.Json;
using AWS.Deploy.Common.Recipes;

namespace AWS.Deploy.CLI.IntegrationTests.SaveCdkDeploymentProject
{
    public class SaveCdkDeploymentProjectTests 
    {
        private readonly CommandLineWrapper _commandLineWrapper;

        public SaveCdkDeploymentProjectTests()
        {
            _commandLineWrapper = new CommandLineWrapper(new ConsoleOrchestratorLogger(new ConsoleInteractiveServiceImpl()));
        }

        [Fact]
        public async Task DefaultSaveDirectory()
        {
            var tempDirectoryPath = new TestAppManager().GetProjectPath(string.Empty);
            await _commandLineWrapper.Run("git init", tempDirectoryPath);
            var targetApplicationProjectPath = Path.Combine(tempDirectoryPath, "testapps", "WebAppWithDockerFile");

            await Utilities.CreateCDKDeploymentProject(targetApplicationProjectPath);

            // Verify a bug fix that the IDictionary for TypeHintData was not getting serialized.
            var recipeFilePath = Directory.GetFiles(targetApplicationProjectPath + "CDK", "*.recipe", SearchOption.TopDirectoryOnly).FirstOrDefault();
            Assert.True(File.Exists(recipeFilePath));
            var recipeRoot = JsonConvert.DeserializeObject<RecipeDefinition>(File.ReadAllText(recipeFilePath));
            var applicationIAMRoleSetting = recipeRoot.OptionSettings.FirstOrDefault(x => string.Equals(x.Id, "ApplicationIAMRole"));
            Assert.Equal("ecs-tasks.amazonaws.com", applicationIAMRoleSetting.TypeHintData["ServicePrincipal"]);
        }

        [Fact]
        public async Task CustomSaveDirectory()
        {
            var tempDirectoryPath = new TestAppManager().GetProjectPath(string.Empty);
            await _commandLineWrapper.Run("git init", tempDirectoryPath);
            var targetApplicationProjectPath = Path.Combine(tempDirectoryPath, "testapps", "WebAppWithDockerFile");

            var saveDirectoryPath = Path.Combine(tempDirectoryPath, "DeploymentProjects", "MyCdkApp");
            await Utilities.CreateCDKDeploymentProject(targetApplicationProjectPath, saveDirectoryPath);
        }

        [Fact]
        public async Task InvalidSaveCdkDirectoryInsideProjectDirectory()
        {
            var tempDirectoryPath = new TestAppManager().GetProjectPath(string.Empty);
            await _commandLineWrapper.Run("git init", tempDirectoryPath);
            var targetApplicationProjectPath = Path.Combine(tempDirectoryPath, "testapps", "WebAppWithDockerFile");

            var saveDirectoryPath = Path.Combine(tempDirectoryPath, "testapps", "WebAppWithDockerFile", "MyCdkApp");
            await Utilities.CreateCDKDeploymentProject(targetApplicationProjectPath, saveDirectoryPath, false);
        }

        [Fact]
        public async Task InvalidNonEmptySaveCdkDirectory()
        {
            var tempDirectoryPath = new TestAppManager().GetProjectPath(string.Empty);
            await _commandLineWrapper.Run("git init", tempDirectoryPath);
            var targetApplicationProjectPath = Path.Combine(tempDirectoryPath, "testapps", "WebAppWithDockerFile");

            Directory.CreateDirectory(Path.Combine(tempDirectoryPath, "MyCdkApp", "MyFolder"));
            var saveDirectoryPath = Path.Combine(tempDirectoryPath, "MyCdkApp");
            await Utilities.CreateCDKDeploymentProject(targetApplicationProjectPath, saveDirectoryPath, false);
        }
    }
}
