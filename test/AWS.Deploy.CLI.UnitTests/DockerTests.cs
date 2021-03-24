// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using AWS.Deploy.DockerEngine;
using Should;
using Xunit;

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
        public async Task DockerGenerate(string topLevelFolder, string projectName)
        {
            var projectPath = ResolvePath(Path.Combine(topLevelFolder, projectName));

            var project = await new ProjectDefinitionParser(new FileManager(), new DirectoryManager()).Parse(projectPath);

            var engine = new DockerEngine.DockerEngine(project);

            engine.GenerateDockerFile();

            AssertDockerFilesAreEqual(projectPath);
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

        private string ResolvePath(string projectName)
        {
            var testsPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            while (testsPath != null && !string.Equals(new DirectoryInfo(testsPath).Name, "test", StringComparison.OrdinalIgnoreCase))
            {
                testsPath = Directory.GetParent(testsPath).FullName;
            }

            return Path.Combine(testsPath, "..", "testapps", "docker", projectName);
        }

        private void AssertDockerFilesAreEqual(string path, string generatedFile = "Dockerfile", string referenceFile = "Dockerfile")
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
