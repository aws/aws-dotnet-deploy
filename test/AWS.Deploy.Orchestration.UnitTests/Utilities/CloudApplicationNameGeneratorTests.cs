// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
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
        public void ValidNamesAreValid_WithRespectTo_Regex(string name)
        {
            var existingApplications = new List<CloudApplication>();
            var validationResult = _cloudApplicationNameGenerator.IsValidName(name, existingApplications);
            validationResult.IsValid.ShouldBeTrue();
        }

        [Theory]
        [InlineData("1-starts-with-number")]
        [InlineData("withSpecial!こんにちは世界Characters")]
        [InlineData("With.Periods")]
        [InlineData("With Spaces")]
        public void InvalidNamesAreInvalid_WithRespectTo_Regex(string name)
        {
            var existingApplications = new List<CloudApplication>();
            var validationResult = _cloudApplicationNameGenerator.IsValidName(name, existingApplications);
            validationResult.IsValid.ShouldBeFalse();
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
            var validationResult = _cloudApplicationNameGenerator.IsValidName(recommendation, existingApplication);

            // ASSERT
            validationResult.IsValid.ShouldBeTrue();
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
                new CloudApplication(projectFile, string.Empty, CloudApplicationResourceType.CloudFormationStack, string.Empty)
            };

            // ACT
            var recommendation = _cloudApplicationNameGenerator.GenerateValidName(projectDefinition, existingApplication);

            // ASSERT
            recommendation.ShouldEqual(expectedRecommendation);
        }

        [Theory]
        [InlineData("SuperTest", "SuperTest1")]
        [InlineData("SuperTest1", "SuperTest2")]
        [InlineData("SuperTest2022", "SuperTest2023")]
        public async Task SuggestsValidNameAndRespectsExistingApplications_ProjectWithNumber(string projectFile, string expectedRecommendation)
        {
            var projectPath = _fakeFileManager.AddEmptyProjectFile($"c:\\{projectFile}.csproj");

            var projectDefinition = await _projectDefinitionParser.Parse(projectPath);

            var existingApplication = new List<CloudApplication>
            {
                new CloudApplication(projectFile, string.Empty, CloudApplicationResourceType.CloudFormationStack, string.Empty)
            };

            // ACT
            var recommendation = _cloudApplicationNameGenerator.GenerateValidName(projectDefinition, existingApplication);

            // ASSERT
            recommendation.ShouldEqual(expectedRecommendation);
        }

        [Fact]
        public async Task SuggestsValidNameAndRespectsExistingApplications_NoExistingCloudApplication()
        {
            // ARRANGE
            var projectFile = "SuperTest";
            var expectedRecommendation = $"{projectFile}";

            var projectPath = _fakeFileManager.AddEmptyProjectFile($"c:\\{projectFile}.csproj");

            var projectDefinition = await _projectDefinitionParser.Parse(projectPath);

            var existingApplication = new List<CloudApplication> ();

            // ACT
            var recommendation = _cloudApplicationNameGenerator.GenerateValidName(projectDefinition, existingApplication);

            // ASSERT
            recommendation.ShouldEqual(expectedRecommendation);
        }

        [Fact]
        public async Task SuggestsValidNameAndRespectsExistingApplications_MultipleProjectWithNumber()
        {
            // ARRANGE
            var projectFile = "SuperTest1";
            var projectFile2 = "SuperTest2";
            var expectedRecommendation = $"SuperTest3";

            var projectPath = _fakeFileManager.AddEmptyProjectFile($"c:\\{projectFile}.csproj");
            var projectPath2 = _fakeFileManager.AddEmptyProjectFile($"c:\\{projectFile2}.csproj");

            var projectDefinition = await _projectDefinitionParser.Parse(projectPath);

            var existingApplication = new List<CloudApplication>
            {
                new CloudApplication(projectFile, string.Empty, CloudApplicationResourceType.CloudFormationStack, string.Empty),
                new CloudApplication(projectFile2, string.Empty, CloudApplicationResourceType.CloudFormationStack, string.Empty)
            };

            // ACT
            var recommendation = _cloudApplicationNameGenerator.GenerateValidName(projectDefinition, existingApplication);

            // ASSERT
            recommendation.ShouldEqual(expectedRecommendation);
        }

        [Theory]
        [InlineData("application1", DeploymentTypes.CdkProject)]
        [InlineData("application2", DeploymentTypes.CdkProject)]
        [InlineData("application3", DeploymentTypes.BeanstalkEnvironment)]
        public void InvalidNamesAreInvalid_WithRespectTo_ExistingApplications(string name, DeploymentTypes deploymentType)
        {
            // ARRANGE
            var existingApplications = new List<CloudApplication>()
            {
                new CloudApplication("application1", "id1", CloudApplicationResourceType.CloudFormationStack, "recipe1"),
                new CloudApplication("application2", "id2", CloudApplicationResourceType.CloudFormationStack, "recipe2"),
                new CloudApplication("application3", "id3", CloudApplicationResourceType.BeanstalkEnvironment, "recipe3"),
                new CloudApplication("application4", "id4", CloudApplicationResourceType.CloudFormationStack, "recipe1"),
            };

            // ACT
            var validationResult = _cloudApplicationNameGenerator.IsValidName(name, existingApplications, deploymentType);

            // ASSERT
            validationResult.IsValid.ShouldBeFalse();
        }

        [Theory]
        [InlineData("application", DeploymentTypes.CdkProject)]
        [InlineData("application6", DeploymentTypes.CdkProject)]
        [InlineData("application1", DeploymentTypes.BeanstalkEnvironment)]
        [InlineData("application3", DeploymentTypes.CdkProject)]
        public void ValidNamesAreValid_WithRespectTo_ExistingApplications(string name, DeploymentTypes deploymentType)
        {
            // ARRANGE
            var existingApplications = new List<CloudApplication>()
            {
                new CloudApplication("application1", "id1", CloudApplicationResourceType.CloudFormationStack, "recipe1"),
                new CloudApplication("application2", "id2", CloudApplicationResourceType.CloudFormationStack, "recipe2"),
                new CloudApplication("application3", "id3", CloudApplicationResourceType.BeanstalkEnvironment, "recipe3"),
                new CloudApplication("application4", "id4", CloudApplicationResourceType.CloudFormationStack, "recipe1"),
                new CloudApplication("application5", "id4", CloudApplicationResourceType.BeanstalkEnvironment, "recipe3"),
            };

            // ACT
            var validationResult = _cloudApplicationNameGenerator.IsValidName(name, existingApplications, deploymentType);

            // ASSERT
            validationResult.IsValid.ShouldBeTrue();
        }
    }
}
