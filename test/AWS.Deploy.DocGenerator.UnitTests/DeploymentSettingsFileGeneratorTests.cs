// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Deploy.DocGenerator.Generators;
using AWS.Deploy.DocGenerator.UnitTests.Utilities;
using AWS.Deploy.ServerMode.Client;
using Moq;
using Xunit;

namespace AWS.Deploy.DocGenerator.UnitTests
{
    public class DeploymentSettingsFileGeneratorTests
    {
        private readonly Mock<IRestAPIClient> _restClient;
        private readonly TestFileManager _fileManager;

        public DeploymentSettingsFileGeneratorTests()
        {
            _restClient = new Mock<IRestAPIClient>();
            _fileManager = new TestFileManager();
        }

        /// <summary>
        /// Checks if the generated documentation file is producing the same output as a local copy.
        /// </summary>
        [Fact]
        public async Task GenerateTest()
        {
            var recipeSummary = new RecipeSummary {
                Id = "AspNetAppAppRunner",
                Name = "ASP.NET Core App to AWS App Runner",
                Description = "This ASP.NET Core application will be built as a container image on Linux and deployed to AWS App Runner," +
                " a fully managed service for web applications and APIs." +
                " If your project does not contain a Dockerfile, it will be automatically generated," +
                " otherwise an existing Dockerfile will be used. " +
                "Recommended if you want to deploy your web application as a Linux container image on a fully managed environment."
            };
            var recipeOptionSettingSummary1 = new RecipeOptionSettingSummary
            {
                Id = "ServiceName",
                Name = "Service Name",
                Description = "The name of the AWS App Runner service.",
                Type = "String",
                Settings = new List<RecipeOptionSettingSummary>()
            };
            var recipeOptionSettingSummary2 = new RecipeOptionSettingSummary
            {
                Id = "ApplicationIAMRole",
                Name = "Application IAM Role",
                Description = "The Identity and Access Management (IAM) role that provides AWS credentials to the application to access AWS services.",
                Type = "Object",
                Settings = new List<RecipeOptionSettingSummary>
                {
                    new RecipeOptionSettingSummary
                    {
                        Id = "CreateNew",
                        Name = "Create New Role",
                        Description = "Do you want to create a new role?",
                        Type = "Bool",
                        Settings = new List<RecipeOptionSettingSummary>()
                    }
                }
            };

            _restClient.Setup(x => x.ListAllRecipesAsync(It.IsAny<string?>())).ReturnsAsync(new ListAllRecipesOutput { Recipes = new List<RecipeSummary> { recipeSummary } });
            _restClient.Setup(x => x.GetRecipeOptionSettingsAsync(It.IsAny<string>(), It.IsAny<string?>())).ReturnsAsync(new List<RecipeOptionSettingSummary> { recipeOptionSettingSummary1, recipeOptionSettingSummary2 });

            var deploymentSettingsFileGenerator = new DeploymentSettingsFileGenerator(_restClient.Object, _fileManager);
            await deploymentSettingsFileGenerator.Generate();

            var filePath = _fileManager.InMemoryStore.Keys.First();
            var actualResult = _fileManager.InMemoryStore[filePath];

            Assert.Equal(File.ReadAllText("./DeploymentSettingsFiles/AspNetAppAppRunner.md"), actualResult);
        }
    }
}
