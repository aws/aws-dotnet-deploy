// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Orchestration.Utilities;
using Moq;
using Should;
using Xunit;

namespace AWS.Deploy.Orchestration.UnitTests.Utilities
{
    /// <summary>
    /// Tests for <see cref="CloudApplicationNameGenerator"/>
    /// </summary>
    public class CloudApplicationNameGeneratorTests
    {
        private readonly CloudApplicationNameGenerator _cloudApplicationNameGenerator;
        private readonly TestFileManager _fakeFileManager;
        private readonly Mock<IDirectoryManager> _mockDirectoryManager;
        private readonly IProjectDefinitionParser _projectDefinitionParser;

        public CloudApplicationNameGeneratorTests()
        {
            _fakeFileManager = new TestFileManager();
            _mockDirectoryManager = new Mock<IDirectoryManager>();

            _projectDefinitionParser = new ProjectDefinitionParser(_fakeFileManager, _mockDirectoryManager.Object);

            _cloudApplicationNameGenerator = new CloudApplicationNameGenerator(_fakeFileManager, _mockDirectoryManager.Object);
        }


        [Theory]
        [InlineData("A")]
        [InlineData("Valid")]
        [InlineData("A21")]
        [InlineData("Very-Long-With-Hyphens-And-Numbers")]
        public void ValidNamesAreValid(string name)
        {
            _cloudApplicationNameGenerator
                .IsValidName(name)
                .ShouldBeTrue();
        }

        [Theory]
        [InlineData("1-starts-with-number")]
        [InlineData("withSpecial!こんにちは世界Characters")]
        [InlineData("With.Periods")]
        [InlineData("With Spaces")]
        public void InvalidNamesAreInvalid(string name)
        {
            _cloudApplicationNameGenerator
                .IsValidName(name)
                .ShouldBeFalse();
        }

        [Theory]
        [InlineData("1-starts-with-number.csproj")]
        [InlineData("A1.fsproj")]
        [InlineData("withSpecial!こんにちは世界Characters.csproj")]
        [InlineData("With.Periods.csproj")]
        [InlineData("With Spaces.csproj")]
        [InlineData(".._.11-23-Long-Invalid_Prefix.csproj")]
        [InlineData("こんにちは世界Characters.csproj")]
        public async Task SuggestsValidName(string projectFile)
        {
            // ARRANGE
            var projectPath = _fakeFileManager.AddEmptyProjectFile($"c:\\{projectFile}");

            var projectDefinition = await _projectDefinitionParser.Parse(projectPath);

            var existingApplication = new List<CloudApplication>();

            var recommendation = _cloudApplicationNameGenerator.GenerateValidName(projectDefinition, existingApplication);

            // ACT
            var recommendationIsValid = _cloudApplicationNameGenerator.IsValidName(recommendation);

            // ASSERT
            recommendationIsValid.ShouldBeTrue();
        }

        [Fact]
        public async Task SuggestsValidNameAndRespectsExistingApplications()
        {
            // ARRANGE
            var projectFile = "SuperTest";
            var expectedRecommendation = $"{projectFile}1";

            var projectPath = _fakeFileManager.AddEmptyProjectFile($"c:\\{projectFile}.csproj");

            var projectDefinition = await _projectDefinitionParser.Parse(projectPath);

            var existingApplication = new List<CloudApplication>
            {
                new CloudApplication(projectFile, string.Empty)
            };

            // ACT
            var recommendation = _cloudApplicationNameGenerator.GenerateValidName(projectDefinition, existingApplication);

            // ASSERT
            recommendation.ShouldEqual(expectedRecommendation);
        }
    }
}
