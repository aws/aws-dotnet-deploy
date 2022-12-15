// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AWS.Deploy.CLI.UnitTests.Utilities;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using Xunit;
using Should;
using AWS.Deploy.CLI.Utilities;

namespace AWS.Deploy.CLI.UnitTests
{
    public class ProjectParserUtilityTests
    {
        [Theory]
        [InlineData("WebAppWithDockerFile", "WebAppWithDockerFile.csproj")]
        [InlineData("WebAppNoDockerFile", "WebAppNoDockerFile.csproj")]
        [InlineData("ConsoleAppTask", "ConsoleAppTask.csproj")]
        [InlineData("ConsoleAppService", "ConsoleAppService.csproj")]
        [InlineData("MessageProcessingApp", "MessageProcessingApp.csproj")]
        [InlineData("ContosoUniversityBackendService", "ContosoUniversityBackendService.csproj")]
        [InlineData("ContosoUniversityWeb", "ContosoUniversity.csproj")]
        [InlineData("BlazorWasm60", "BlazorWasm60.csproj")]
        public async Task ParseProjectDefinitionWithRelativeProjectPath(string projectName, string csprojName)
        {
            //Arrange
            var currrentWorkingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); 
            var projectDirectoryPath = SystemIOUtilities.ResolvePath(projectName);
            var absoluteProjectDirectoryPath = new DirectoryInfo(projectDirectoryPath).FullName;
            var absoluteProjectPath = Path.Combine(absoluteProjectDirectoryPath, csprojName);
            var relativeProjectDirectoryPath = Path.GetRelativePath(currrentWorkingDirectory, absoluteProjectDirectoryPath);
            var projectSolutionPath = SystemIOUtilities.ResolvePathToSolution();
            var projectDefinitionParser = new ProjectDefinitionParser(new FileManager(), new DirectoryManager());
            var projectParserUtility = new ProjectParserUtility(projectDefinitionParser, new DirectoryManager());

            // Act
            var projectDefinition = await projectParserUtility.Parse(relativeProjectDirectoryPath);

            // Assert
            projectDefinition.ShouldNotBeNull();
            Assert.Equal(absoluteProjectPath, projectDefinition.ProjectPath);
            Assert.Equal(projectSolutionPath, projectDefinition.ProjectSolutionPath);
        }

        [Theory]
        [InlineData("WebAppWithDockerFile", "WebAppWithDockerFile.csproj")]
        [InlineData("WebAppNoDockerFile", "WebAppNoDockerFile.csproj")]
        [InlineData("ConsoleAppTask", "ConsoleAppTask.csproj")]
        [InlineData("ConsoleAppService", "ConsoleAppService.csproj")]
        [InlineData("MessageProcessingApp", "MessageProcessingApp.csproj")]
        [InlineData("ContosoUniversityBackendService", "ContosoUniversityBackendService.csproj")]
        [InlineData("ContosoUniversityWeb", "ContosoUniversity.csproj")]
        [InlineData("BlazorWasm60", "BlazorWasm60.csproj")]
        public async Task ParseProjectDefinitionWithAbsoluteProjectPath(string projectName, string csprojName)
        {
            //Arrange
            var projectDirectoryPath = SystemIOUtilities.ResolvePath(projectName);
            var absoluteProjectDirectoryPath = new DirectoryInfo(projectDirectoryPath).FullName;
            var absoluteProjectPath = Path.Combine(absoluteProjectDirectoryPath, csprojName);
            var projectSolutionPath = SystemIOUtilities.ResolvePathToSolution();
            var projectDefinitionParser = new ProjectDefinitionParser(new FileManager(), new DirectoryManager());
            var projectParserUtility = new ProjectParserUtility(projectDefinitionParser, new DirectoryManager());

            // Act
            var projectDefinition = await projectParserUtility.Parse(absoluteProjectPath);

            // Assert
            projectDefinition.ShouldNotBeNull();
            Assert.Equal(absoluteProjectPath, projectDefinition.ProjectPath);
            Assert.Equal(projectSolutionPath, projectDefinition.ProjectSolutionPath);
        }

        [Theory]
        [InlineData("C:\\MyProject\\doesNotExistSrc")]
        [InlineData("C:\\MyProject\\src\\doesNotExist.csproj")]
        public async Task Throws_FailedToFindDeployableTargetException_WithInvalidProjectPaths(string projectPath)
        {
            // Arrange
            var projectDefinitionParser = new ProjectDefinitionParser(new FileManager(), new DirectoryManager());
            var projectParserUtility = new ProjectParserUtility(projectDefinitionParser, new DirectoryManager());

            // Act and Assert
            var ex = await Assert.ThrowsAsync<FailedToFindDeployableTargetException>(async () => await projectParserUtility.Parse(projectPath));
            Assert.Equal($"Failed to find a valid .csproj or .fsproj file at path {projectPath}", ex.Message);
        }
    }
}

