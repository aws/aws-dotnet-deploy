// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Recipes.Validation;
using AWS.Deploy.Orchestration;
using Moq;
using Xunit;

namespace AWS.Deploy.CLI.Common.UnitTests.Recipes.Validation
{
    /// <summary>
    /// Tests for the recipe-level validation between the Dockerfile path and
    /// docker execution recipe options, <see cref="DockerfilePathValidator"/>
    /// </summary>
    public class DockerfilePathValidationTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly RecipeDefinition _recipeDefinition;
        private readonly IDirectoryManager _directoryManager;
        private readonly IFileManager _fileManager;

        public DockerfilePathValidationTests()
        {
            _serviceProvider = new Mock<IServiceProvider>().Object;
            _directoryManager = new TestDirectoryManager();
            _fileManager = new TestFileManager();

            _recipeDefinition = new Mock<RecipeDefinition>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DeploymentTypes>(),
                It.IsAny<DeploymentBundleTypes>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()).Object;
        }

        public static IEnumerable<object[]> DockerfilePathTestData => new List<object[]>()
        {
            // Dockerfile path | Docker execution directory | expected to be valid?

            // We generate a Dockerfile later if one isn't specified, so not invalid at this point
            new object[] { "", Path.Combine("C:", "project"), true },

            // We compute the execution directory later if one isn't specified, so not invalid at this point
            new object[] { Path.Combine("C:", "project", "Dockerfile"), "", true },

            // Dockerfile is in the execution directory, with absolute paths
            new object[] { Path.Combine("C:", "project", "Dockerfile"), Path.Combine("C:", "project"), true },

            // Dockerfile is in the execution directory, with relative paths
            new object[] { Path.Combine(".", "Dockerfile"), Path.Combine("."), true },

            // Dockerfile is further down in execution directory, with absolute paths
            new object[] { Path.Combine("C:", "project", "child", "Dockerfile"), Path.Combine("C:", "project"), true },

            // Dockerfile is further down in execution directory, with relative paths
            new object[] { Path.Combine(".", "child", "Dockerfile"), Path.Combine("."), true },

            // Dockerfile is outside of the execution directory, which is invalid
            new object[] { Path.Combine("C:", "project", "Dockerfile"), Path.Combine("C:", "foo"), false }
        };

        /// <summary>
        /// Tests for <see cref="DockerfilePathValidator"/>, which validates the relationship
        /// between a Dockerfile path and the Docker execution directory
        /// </summary>
        [Theory]
        [MemberData(nameof(DockerfilePathTestData))]
        public async Task DockerfilePathValidationHelperAsync(string dockerfilePath, string dockerExecutionDirectory, bool expectedToBeValid)
        {
            var projectPath = Path.Combine("C:", "project", "test.csproj");
            var options = new List<OptionSettingItem>()
            {
                new OptionSettingItem("DockerfilePath", "", "", "")
            };
            var projectDefintion = new ProjectDefinition(null!, projectPath, "", "");
            var recommendation = new Recommendation(_recipeDefinition, projectDefintion, 100, new Dictionary<string, object>());
            var validator = new DockerfilePathValidator(_directoryManager, _fileManager);

            recommendation.DeploymentBundle.DockerExecutionDirectory = dockerExecutionDirectory;
            recommendation.DeploymentBundle.DockerfilePath = dockerfilePath;

            // "Write" to the TestFileManager so that "Exists" returns true
            if (Path.IsPathRooted(dockerfilePath))
                await _fileManager.WriteAllTextAsync(dockerfilePath, "");
            else
                await _fileManager.WriteAllTextAsync(Path.Combine(recommendation.GetProjectDirectory(), dockerfilePath), "");

            var validationResult = await validator.Validate(recommendation, null!);

            Assert.Equal(expectedToBeValid, validationResult.IsValid);
        }
    }
}
