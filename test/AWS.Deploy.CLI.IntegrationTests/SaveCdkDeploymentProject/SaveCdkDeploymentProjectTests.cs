// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Linq;
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.CLI.IntegrationTests.Services;
using AWS.Deploy.CLI.Utilities;
using Task = System.Threading.Tasks.Task;
using Newtonsoft.Json;
using AWS.Deploy.Common.Recipes;
using NUnit.Framework;

namespace AWS.Deploy.CLI.IntegrationTests.SaveCdkDeploymentProject
{
    [TestFixture]
    public class SaveCdkDeploymentProjectTests
    {
        private CommandLineWrapper _commandLineWrapper;
        private InMemoryInteractiveService _inMemoryInteractiveService;

        [SetUp]
        public void Initialize()
        {
            _inMemoryInteractiveService = new InMemoryInteractiveService();
            _commandLineWrapper = new CommandLineWrapper(_inMemoryInteractiveService);
        }

        [Test]
        public async Task DefaultSaveDirectory()
        {
            var tempDirectoryPath = new TestAppManager().GetProjectPath(string.Empty);
            await _commandLineWrapper.Run("git init", tempDirectoryPath);
            var targetApplicationProjectPath = Path.Combine(tempDirectoryPath, "testapps", "WebAppWithDockerFile");

            await Utilities.CreateCDKDeploymentProject(targetApplicationProjectPath);

            // Verify a bug fix that the IDictionary for TypeHintData was not getting serialized.
            var recipeFilePath = Directory.GetFiles(targetApplicationProjectPath + ".Deployment", "*.recipe", SearchOption.TopDirectoryOnly).FirstOrDefault();
            Assert.True(File.Exists(recipeFilePath));
            var recipeRoot = JsonConvert.DeserializeObject<RecipeDefinition>(File.ReadAllText(recipeFilePath));
            var applicationIAMRoleSetting = recipeRoot.OptionSettings.FirstOrDefault(x => string.Equals(x.Id, "ApplicationIAMRole"));
            Assert.AreEqual("ecs-tasks.amazonaws.com", applicationIAMRoleSetting.TypeHintData["ServicePrincipal"]);
        }

        [Test]
        public async Task CustomSaveDirectory()
        {
            var tempDirectoryPath = new TestAppManager().GetProjectPath(string.Empty);
            await _commandLineWrapper.Run("git init", tempDirectoryPath);
            var targetApplicationProjectPath = Path.Combine(tempDirectoryPath, "testapps", "WebAppWithDockerFile");

            var saveDirectoryPath = Path.Combine(tempDirectoryPath, "DeploymentProjects", "MyCdkApp");
            await Utilities.CreateCDKDeploymentProject(targetApplicationProjectPath, saveDirectoryPath);
        }

        [Test]
        public async Task InvalidSaveCdkDirectoryInsideProjectDirectory()
        {
            var tempDirectoryPath = new TestAppManager().GetProjectPath(string.Empty);
            await _commandLineWrapper.Run("git init", tempDirectoryPath);
            var targetApplicationProjectPath = Path.Combine(tempDirectoryPath, "testapps", "WebAppWithDockerFile");

            var saveDirectoryPath = Path.Combine(tempDirectoryPath, "testapps", "WebAppWithDockerFile", "MyCdkApp");
            await Utilities.CreateCDKDeploymentProject(targetApplicationProjectPath, saveDirectoryPath, false);
        }

        [Test]
        public async Task InvalidNonEmptySaveCdkDirectory()
        {
            var tempDirectoryPath = new TestAppManager().GetProjectPath(string.Empty);
            await _commandLineWrapper.Run("git init", tempDirectoryPath);
            var targetApplicationProjectPath = Path.Combine(tempDirectoryPath, "testapps", "WebAppWithDockerFile");

            Directory.CreateDirectory(Path.Combine(tempDirectoryPath, "MyCdkApp", "MyFolder"));
            var saveDirectoryPath = Path.Combine(tempDirectoryPath, "MyCdkApp");
            await Utilities.CreateCDKDeploymentProject(targetApplicationProjectPath, saveDirectoryPath, false);
        }

        [TearDown]
        public void Cleanup()
        {
            _inMemoryInteractiveService.ReadStdOutStartToEnd();
        }
    }
}
