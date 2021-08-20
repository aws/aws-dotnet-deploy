// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.CLI.Commands;
using AWS.Deploy.CLI.ServerMode.Controllers;
using AWS.Deploy.CLI.ServerMode.Models;
using AWS.Deploy.Orchestration;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace AWS.Deploy.CLI.UnitTests
{
    public class ServerModeTests
    {
        [Fact]
        public async Task TcpPortIsInUseTest()
        {
            var serverModeCommand1 = new ServerModeCommand(new TestToolInteractiveServiceImpl(), 1234, null, true);
            var serverModeCommand2 = new ServerModeCommand(new TestToolInteractiveServiceImpl(), 1234, null, true);

            var serverModeTask1 = serverModeCommand1.ExecuteAsync();
            var serverModeTask2 = serverModeCommand2.ExecuteAsync();

            await Task.WhenAny(serverModeTask1, serverModeTask2);

            Assert.False(serverModeTask1.IsCompleted);

            Assert.True(serverModeTask2.IsCompleted);
            Assert.True(serverModeTask2.IsFaulted);

            Assert.NotNull(serverModeTask2.Exception);
            Assert.Single(serverModeTask2.Exception.InnerExceptions);

            Assert.IsType<TcpPortInUseException>(serverModeTask2.Exception.InnerException);
        }

        [Theory]
        [InlineData("")]
        [InlineData("InvalidId")]
        public void RecipeController_GetRecipe_EmptyId(string recipeId)
        {
            var recipeController = new RecipeController();
            var response = recipeController.GetRecipe(recipeId);

            Assert.IsType<BadRequestObjectResult>(response);
        }

        [Fact]
        public void RecipeController_GetRecipe_HappyPath()
        {
            var recipeController = new RecipeController();
            var recipeDefinitions = RecipeHandler.GetRecipeDefinitions();
            var recipe = recipeDefinitions.First();

            var response = recipeController.GetRecipe(recipe.Id);

            var result = Assert.IsType<OkObjectResult>(response);
            var resultRecipe = Assert.IsType<RecipeSummary>(result.Value);
            Assert.Equal(recipe.Id, resultRecipe.Id);
        }
    }
}
