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

namespace AWS.Deploy.CLI.UnitTests
{
    public class ProjectDefinitionParserTest
    {
        [Theory]
        [InlineData("WebAppWithDockerFile", "WebAppWithDockerFile.csproj")]
        [InlineData("WebAppNoDockerFile", "WebAppNoDockerFile.csproj")]
        [InlineData("ConsoleAppTask", "ConsoleAppTask.csproj")]
        [InlineData("ConsoleAppService", "ConsoleAppService.csproj")]
        [InlineData("MessageProcessingApp", "MessageProcessingApp.csproj")]
        [InlineData("ContosoUniversityBackendService", "ContosoUniversityBackendService.csproj")]
        [InlineData("ContosoUniversityWeb", "ContosoUniversity.csproj")]
        [InlineData("BlazorWasm31", "BlazorWasm31.csproj")]
        [InlineData("BlazorWasm50", "BlazorWasm50.csproj")]
        public async Task ParseProjectDefinitionWithRelativeProjectPath(string projectName, string csprojName)
        {
            //Arrange
            var currrentWorkingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); 
            var projectDirectoryPath = SystemIOUtilities.ResolvePath(projectName);
            var absoluteProjectDirectoryPath = new DirectoryInfo(projectDirectoryPath).FullName;
            var absoluteProjectPath = Path.Combine(absoluteProjectDirectoryPath, csprojName);
            var relativeProjectDirectoryPath = Path.GetRelativePath(currrentWorkingDirectory, absoluteProjectDirectoryPath);

            // Act
            var projectDefinition = await new ProjectDefinitionParser(new FileManager(), new DirectoryManager()).Parse(relativeProjectDirectoryPath);

            // Assert
            projectDefinition.ShouldNotBeNull();
            Assert.Equal(absoluteProjectPath, projectDefinition.ProjectPath);
        }

        [Theory]
        [InlineData("WebAppWithDockerFile", "WebAppWithDockerFile.csproj")]
        [InlineData("WebAppNoDockerFile", "WebAppNoDockerFile.csproj")]
        [InlineData("ConsoleAppTask", "ConsoleAppTask.csproj")]
        [InlineData("ConsoleAppService", "ConsoleAppService.csproj")]
        [InlineData("MessageProcessingApp", "MessageProcessingApp.csproj")]
        [InlineData("ContosoUniversityBackendService", "ContosoUniversityBackendService.csproj")]
        [InlineData("ContosoUniversityWeb", "ContosoUniversity.csproj")]
        [InlineData("BlazorWasm31", "BlazorWasm31.csproj")]
        [InlineData("BlazorWasm50", "BlazorWasm50.csproj")]
        public async Task ParseProjectDefinitionWithAbsoluteProjectPath(string projectName, string csprojName)
        {
            //Arrange
            var projectDirectoryPath = SystemIOUtilities.ResolvePath(projectName);
            var absoluteProjectDirectoryPath = new DirectoryInfo(projectDirectoryPath).FullName;
            var absoluteProjectPath = Path.Combine(absoluteProjectDirectoryPath, csprojName);

            // Act
            var projectDefinition = await new ProjectDefinitionParser(new FileManager(), new DirectoryManager()).Parse(absoluteProjectDirectoryPath);

            // Assert
            projectDefinition.ShouldNotBeNull();
            Assert.Equal(absoluteProjectPath, projectDefinition.ProjectPath);
        }
    }
}

