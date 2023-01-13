// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.CLI.ServerMode.Models;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Recipes;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AWS.Deploy.CLI.ServerMode.Controllers
{
    [Produces("application/json")]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class RecipeController : ControllerBase
    {
        private readonly IRecipeHandler _recipeHandler;
        private readonly IProjectDefinitionParser _projectDefinitionParser;

        public RecipeController(IRecipeHandler recipeHandler, IProjectDefinitionParser projectDefinitionParser)
        {
            _recipeHandler = recipeHandler;
            _projectDefinitionParser = projectDefinitionParser;
        }

        private async Task<List<RecipeDefinition>> GetAllAvailableRecipes(string? projectPath = null)
        {
            var recipePaths = new HashSet<string> { RecipeLocator.FindRecipeDefinitionsPath() };
            HashSet<string> customRecipePaths = new HashSet<string>();

            if (!string.IsNullOrEmpty(projectPath))
            {
                var projectDefinition = await _projectDefinitionParser.Parse(projectPath);
                customRecipePaths = await _recipeHandler.LocateCustomRecipePaths(projectDefinition);
            }

            return await _recipeHandler.GetRecipeDefinitions(recipeDefinitionPaths: recipePaths.Union(customRecipePaths).ToList());
        }

        /// <summary>
        /// Gets a list of available recipe IDs.
        /// </summary>
        [HttpGet("recipes")]
        [SwaggerOperation(OperationId = "ListAllRecipes")]
        [SwaggerResponse(200, type: typeof(ListAllRecipesOutput))]
        [ProducesResponseType(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ListAllRecipes([FromQuery] string? projectPath = null)
        {
            var recipeDefinitions = await GetAllAvailableRecipes(projectPath);

            var recipes = recipeDefinitions
                .Select(x =>
                    new RecipeSummary(
                        x.Id,
                        x.Version,
                        x.Name,
                        x.Description,
                        x.ShortDescription,
                        x.TargetService,
                        x.DeploymentType.ToString(),
                        x.DeploymentBundle.ToString())
                ).ToList();

            var output = new ListAllRecipesOutput
            {
                Recipes = recipes
            };

            return Ok(output);
        }

        /// <summary>
        /// Gets a summary of the specified Recipe.
        /// </summary>
        [HttpGet("{recipeId}")]
        [SwaggerOperation(OperationId = "GetRecipe")]
        [SwaggerResponse(200, type: typeof(RecipeSummary))]
        [ProducesResponseType(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetRecipe(string recipeId, [FromQuery] string? projectPath = null)
        {
            if (string.IsNullOrEmpty(recipeId))
            {
                return BadRequest($"A Recipe ID was not provided.");
            }

            var recipeDefinitions = await GetAllAvailableRecipes(projectPath);

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

        /// <summary>
        /// Gets a summary of the specified recipe option settings.
        /// </summary>
        [HttpGet("{recipeId}/settings")]
        [SwaggerOperation(OperationId = "GetRecipeOptionSettings")]
        [SwaggerResponse(200, type: typeof(List<RecipeOptionSettingSummary>))]
        [ProducesResponseType(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetRecipeOptionSettings(string recipeId, [FromQuery] string? projectPath = null)
        {
            if (string.IsNullOrEmpty(recipeId))
            {
                return BadRequest($"A Recipe ID was not provided.");
            }

            var recipeDefinitions = await GetAllAvailableRecipes(projectPath);

            var selectedRecipeDefinition = recipeDefinitions.FirstOrDefault(x => x.Id.Equals(recipeId));

            if (selectedRecipeDefinition == null)
            {
                return BadRequest($"Recipe ID {recipeId} not found.");
            }

            var settings = GetOptionSettingSummary(selectedRecipeDefinition.OptionSettings);

            return Ok(settings);
        }

        private List<RecipeOptionSettingSummary> GetOptionSettingSummary(List<OptionSettingItem> optionSettingItems)
        {
            var settings = new List<RecipeOptionSettingSummary>();
            foreach (var optionSetting in optionSettingItems)
            {
                settings.Add(new RecipeOptionSettingSummary(
                    optionSetting.Id,
                    optionSetting.Name,
                    optionSetting.Description,
                    optionSetting.Type.ToString()
                    )
                {
                    Settings = GetOptionSettingSummary(optionSetting.ChildOptionSettings)
                });
            }
            return settings;
        }
    }
}
