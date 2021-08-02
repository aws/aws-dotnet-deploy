// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using AWS.Deploy.CLI.ServerMode.Models;
using AWS.Deploy.Orchestration;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AWS.Deploy.CLI.ServerMode.Controllers
{
    [Produces("application/json")]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class RecipeController : ControllerBase
    {
        /// <summary>
        /// Gets a summary of the specified Recipe.
        /// </summary>
        [HttpGet("{recipeId}")]
        [SwaggerOperation(OperationId = "GetRecipe")]
        [SwaggerResponse(200, type: typeof(RecipeSummary))]
        public IActionResult GetRecipe(string recipeId, [FromQuery] string? projectPath = null)
        {
            if (string.IsNullOrEmpty(recipeId))
            {
                return BadRequest($"A Recipe ID was not provided.");
            }

            var recipeDefinitions = RecipeHandler.GetRecipeDefinitions();
            var selectedRecipeDefinition = recipeDefinitions.FirstOrDefault(x => x.Id.Equals(recipeId));

            if (selectedRecipeDefinition == null)
            {
                return BadRequest($"Recipe ID {recipeId} not found.");
            }

            var output = new RecipeSummary(
                selectedRecipeDefinition.Id,
                selectedRecipeDefinition.Version,
                selectedRecipeDefinition.Name,
                selectedRecipeDefinition.Description,
                selectedRecipeDefinition.ShortDescription,
                selectedRecipeDefinition.TargetService,
                selectedRecipeDefinition.DeploymentType.ToString(),
                selectedRecipeDefinition.DeploymentBundle.ToString()
            );

            return Ok(output);
        }
    }
}
