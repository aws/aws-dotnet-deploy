// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Reflection;
using AWS.Deploy.DockerEngine;
using AWS.DeploymentCommon;
using Xunit;

namespace AWS.Deploy.CLI.UnitTests
{
    public class DockerTests
    {
        [Fact]
        public void DockerGenerateWebAppNoSolution()
        {
            var projectPath = ResolvePath("WebAppNoSolution");
            var engine = new DockerEngine.DockerEngine(new ProjectDefinition(projectPath));
            engine.GenerateDockerFile();

            var dockerfile = File.ReadAllText(Path.Combine(projectPath, "Dockerfile"));
            var referenceDockerfile = File.ReadAllText(Path.Combine(projectPath, "referenceDockerfile"));

            Assert.Equal(dockerfile, referenceDockerfile);
        }

        [Fact]
        public void DockerGenerateWebAppWithSolutionSameLevel()
        {
            var projectPath = ResolvePath("WebAppWithSolutionSameLevel");
            var engine = new DockerEngine.DockerEngine(new ProjectDefinition(projectPath));
            engine.GenerateDockerFile();

            var dockerfile = File.ReadAllText(Path.Combine(projectPath, "Dockerfile"));
            var referenceDockerfile = File.ReadAllText(Path.Combine(projectPath, "referenceDockerfile"));

            Assert.Equal(dockerfile, referenceDockerfile);
        }

        [Fact]
        public void DockerGenerateWebAppWithSolutionParentLevel()
        {
            var projectPath = ResolvePath("WebAppWithSolutionParentLevel\\WebAppWithSolutionParentLevel");
            var engine = new DockerEngine.DockerEngine(new ProjectDefinition(projectPath));
            engine.GenerateDockerFile();

            var dockerfile = File.ReadAllText(Path.Combine(projectPath, "Dockerfile"));
            var referenceDockerfile = File.ReadAllText(Path.Combine(projectPath, "referenceDockerfile"));

            Assert.Equal(dockerfile, referenceDockerfile);
        }

        [Fact]
        public void DockerGenerateWebAppDifferentAssemblyName()
        {
            var projectPath = ResolvePath("WebAppDifferentAssemblyName");
            var engine = new DockerEngine.DockerEngine(new ProjectDefinition(projectPath));
            engine.GenerateDockerFile();

            var dockerfile = File.ReadAllText(Path.Combine(projectPath, "Dockerfile"));
            var referenceDockerfile = File.ReadAllText(Path.Combine(projectPath, "referenceDockerfile"));

            Assert.Equal(dockerfile, referenceDockerfile);
        }

        [Fact(Skip = "TODO: Fix line endings ")]
        public void DockerGenerateWebAppProjectDependencies()
        {
            var projectPath = ResolvePath("WebAppProjectDependencies\\WebAppProjectDependencies");
            var engine = new DockerEngine.DockerEngine(new ProjectDefinition(projectPath));
            engine.GenerateDockerFile();

            var dockerfile = File.ReadAllText(Path.Combine(projectPath, "Dockerfile"));
            var referenceDockerfile = File.ReadAllText(Path.Combine(projectPath, "referenceDockerfile"));

            Assert.Equal(dockerfile, referenceDockerfile);
        }

        [Fact]
        public void DockerGenerateWebAppDifferentTargetFramework()
        {
            var projectPath = ResolvePath("WebAppDifferentTargetFramework");
            var engine = new DockerEngine.DockerEngine(new ProjectDefinition(projectPath));
            engine.GenerateDockerFile();

            var dockerfile = File.ReadAllText(Path.Combine(projectPath, "Dockerfile"));
            var referenceDockerfile = File.ReadAllText(Path.Combine(projectPath, "referenceDockerfile"));

            Assert.Equal(dockerfile, referenceDockerfile);
        }

        [Fact]
        public void DockerGenerateConsoleSdkType()
        {
            var projectPath = ResolvePath("ConsoleSdkType");
            var engine = new DockerEngine.DockerEngine(new ProjectDefinition(projectPath));
            engine.GenerateDockerFile();

            var dockerfile = File.ReadAllText(Path.Combine(projectPath, "Dockerfile"));
            var referenceDockerfile = File.ReadAllText(Path.Combine(projectPath, "referenceDockerfile"));

            Assert.Equal(dockerfile, referenceDockerfile);
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
    }
}
