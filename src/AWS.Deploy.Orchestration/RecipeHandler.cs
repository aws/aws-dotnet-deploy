// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Recipes;
using Newtonsoft.Json;

namespace AWS.Deploy.Orchestration
{
    public class RecipeHandler
    {
        public static async Task<List<RecipeDefinition>> GetRecipeDefinitions(ICustomRecipeLocator customRecipeLocator, ProjectDefinition? projectDefinition)
        {
            IEnumerable<string> recipeDefinitionsPaths = new List<string> { RecipeLocator.FindRecipeDefinitionsPath() };
            if(projectDefinition != null)
            {
                var targetApplicationFullPath = new DirectoryInfo(projectDefinition.ProjectPath).FullName;
                var solutionDirectoryPath = !string.IsNullOrEmpty(projectDefinition.ProjectSolutionPath) ?
                    new DirectoryInfo(projectDefinition.ProjectSolutionPath).Parent.FullName : string.Empty;

                var customPaths = await customRecipeLocator.LocateCustomRecipePaths(targetApplicationFullPath, solutionDirectoryPath);
                recipeDefinitionsPaths = recipeDefinitionsPaths.Union(customPaths);
            }

            var recipeDefinitions = new List<RecipeDefinition>();

            try
            {
                foreach(var recipeDefinitionsPath in recipeDefinitionsPaths)
                {
                    foreach (var recipeDefinitionFile in Directory.GetFiles(recipeDefinitionsPath, "*.recipe", SearchOption.TopDirectoryOnly))
                    {
                        try
                        {
                            var content = File.ReadAllText(recipeDefinitionFile);
                            var definition = JsonConvert.DeserializeObject<RecipeDefinition>(content);
                            recipeDefinitions.Add(definition);
                        }
                        catch (Exception e)
                        {
                            throw new Exception($"Failed to Deserialize Recipe Definition [{recipeDefinitionFile}]: {e.Message}", e);
                        }
                    }
                }
            }
            catch(IOException)
            {
                throw new NoRecipeDefinitionsFoundException(DeployToolErrorCode.FailedToFindRecipeDefinitions, "Failed to find recipe definitions");
            }

            return recipeDefinitions;
        }
    }
}
