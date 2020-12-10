// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using AWS.DeploymentCommon;

namespace AWS.Deploy.Common
{
    public class RecommendationEngine
    {
        private readonly IList<RecipeDefinition> _availableRecommendations = new List<RecipeDefinition>();
        
        public RecommendationEngine(IEnumerable<string> recipeDefinitionPaths)
        {
            recipeDefinitionPaths ??= new List<string>();

            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

            foreach (var recommendationPath in recipeDefinitionPaths)
            {
                foreach (var recipeFile in Directory.GetFiles(recommendationPath, "*.recipe", SearchOption.TopDirectoryOnly))
                {
                    var content = File.ReadAllText(recipeFile);
                    var definition = JsonSerializer.Deserialize<RecipeDefinition>(content, options);
                    definition.RecipePath = recipeFile;

                    _availableRecommendations.Add(definition);
                }
            }
        }

        public IList<Recommendation> ComputeRecommendations(string projectPath)
        {
            var projectDefinition = new ProjectDefinition(projectPath);
            var recommendations = new List<Recommendation>();

            foreach (var potentialRecipe in _availableRecommendations)
            {
                // If the recipe doesn't match on the required rules then skip
                if (!projectDefinition.EvaluateRules(potentialRecipe.RecommendationRules.RequiredRules))
                {
                    continue;
                }

                // If the recipe matches on the negative rules, meaning it has things in it that make it incompatible with the recipe, then skip.
                if (potentialRecipe.RecommendationRules.NegativeRules?.Count > 0 && projectDefinition.EvaluateRules(potentialRecipe.RecommendationRules.NegativeRules))
                {
                    continue;
                }

                var priority = potentialRecipe.RecommendationRules.Priority;

                // If it doesn't match on the optional rules it means we can most likely get it to work but it is not 
                // the preferred approach so divide the priority in half.
                if (!projectDefinition.EvaluateRules(potentialRecipe.RecommendationRules.OptionalRules))
                {
                    priority /= 2;
                }

                recommendations.Add(new Recommendation(potentialRecipe, projectDefinition.ProjectPath, priority));
            }

            recommendations = recommendations.OrderByDescending(recommendation => recommendation.ComputedPriority).ToList();
            return recommendations;
        }
    }
}
