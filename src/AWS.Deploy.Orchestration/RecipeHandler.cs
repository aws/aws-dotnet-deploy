// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Recipes;
using Newtonsoft.Json;

namespace AWS.Deploy.Orchestration
{
    public class RecipeHandler
    {
        public static List<RecipeDefinition> GetRecipeDefinitions()
        {
            var recipeDefinitionsPath = RecipeLocator.FindRecipeDefinitionsPath();
            var recipeDefinitions = new List<RecipeDefinition>();

            try
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
            catch(IOException)
            {
                throw new NoRecipeDefinitionsFoundException("Failed to find recipe definitions");
            }

            return recipeDefinitions;
        }
    }
}
