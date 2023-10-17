// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

extern alias DockerEngine;

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using DockerEngine.AWS.Deploy.DockerEngine;
using Should;
using Xunit;
using AWS.Deploy.CLI.UnitTests.Utilities;
using System.Linq;

namespace AWS.Deploy.CLI.UnitTests
{
    public class DockerTests
    {
        [Theory]
        [InlineData("WebAppNoSolution", "")]
        [InlineData("WebAppWithSolutionSameLevel", "")]
        [InlineData("WebAppWithSolutionParentLevel", "WebAppWithSolutionParentLevel")]
        [InlineData("WebAppDifferentAssemblyName", "")]
        [InlineData("WebAppProjectDependencies", "WebAppProjectDependencies")]
        [InlineData("WebAppDifferentTargetFramework", "")]
        [InlineData("ConsoleSdkType", "")]
        [InlineData("WorkerServiceExample", "")]
        [InlineData("WebAppNet7", "")]
        [InlineData("WebAppNet8", "")]
        public async Task DockerGenerate(string topLevelFolder, string projectName)
        {
            await DockerGenerateTestHelper(topLevelFolder, projectName);
        }

        /// <summary>
        /// Tests that we throw the intended exception when attempting to generate
        /// a Dockerfile that would reference projects located above the solution
        /// </summary>
        [Fact]
        public async Task DockerGenerate_ParentDependency_Fails()
        {
            try
            {
                await DockerGenerateTestHelper("WebAppProjectDependenciesAboveSolution", "WebAppProjectDependencies");

                Assert.True(false, $"Expected to be unable to generate a Dockerfile");
            }
            catch (Exception ex)
            {
                Assert.NotNull(ex);
                Assert.IsType<DockerEngineException>(ex);
                Assert.Equal(DeployToolErrorCode.FailedToGenerateDockerFile, (ex as DeployToolException).ErrorCode);
            }
        }

        [Fact]
        public void DockerFileConfigExists()
        {
            var dockerFileConfig = ProjectUtilities.ReadDockerFileConfig();
            Assert.False(string.IsNullOrWhiteSpace(dockerFileConfig));
        }

        [Fact]
        public void DockerfileTemplateExists()
        {
            var dockerFileTemplate = ProjectUtilities.ReadTemplate();
            Assert.False(string.IsNullOrWhiteSpace(dockerFileTemplate));
        }

        /// <summary>
        /// Generates the Dockerfile for a specified project from the testapps\Docker\ folder
        /// and compares it to the hardcoded ReferenceDockerfile
        /// </summary>
        private async Task DockerGenerateTestHelper(string topLevelFolder, string projectName)
        {
            var fileManager = new FileManager();
            var directoryManager = new DirectoryManager();

            var projectPath = ResolvePath(Path.Combine(topLevelFolder, projectName));

            // ARRANGE - select recommendation
            var recommendationEngine = await HelperFunctions.BuildRecommendationEngine(
                () => projectPath,
                fileManager,
                directoryManager,
                "us-west-2",
                "123456789012",
                "default"
            );

            var recommendations = await recommendationEngine.ComputeRecommendations();
            var selectedRecommendation = recommendations.First();

            var projectDefinition = await new ProjectDefinitionParser(fileManager, new DirectoryManager()).Parse(projectPath);

            var engine = new DockerEngine.AWS.Deploy.DockerEngine.DockerEngine(projectDefinition, fileManager, new TestDirectoryManager());

            selectedRecommendation.DeploymentBundle.DockerfileHttpPort = engine.DetermineDefaultDockerPort(selectedRecommendation);

            engine.GenerateDockerFile(selectedRecommendation);

            AssertDockerFilesAreEqual(projectPath);
        }

        private string ResolvePath(string projectName)
        {
            var testsPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            while (testsPath != null && !string.Equals(new DirectoryInfo(testsPath).Name, "test", StringComparison.OrdinalIgnoreCase))
            {
                testsPath = Directory.GetParent(testsPath).FullName;
            }

            return Path.Combine(testsPath, "..", "testapps", "docker", projectName);
        }

        private void AssertDockerFilesAreEqual(string path, string generatedFile = "Dockerfile", string referenceFile = "ReferenceDockerfile")
        {
            var generated = File.ReadAllText(Path.Combine(path, generatedFile));
            var reference = File.ReadAllText(Path.Combine(path, referenceFile));

            // normalize line endings
            generated = generated.Replace("\r\n", "\n");
            reference = reference.Replace("\r\n", "\n");

            generated.ShouldEqual(reference);
        }
    }
}
