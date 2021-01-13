// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using AWS.Deploy.Common;

namespace AWS.Deploy.RecommendationEngine
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
                var results = EvaluateRules(projectDefinition, potentialRecipe.RecommendationRules);
                if(!results.Include)
                {
                    continue;
                }

                var priority = potentialRecipe.RecipePriority + results.PriorityAdjustment;
                recommendations.Add(new Recommendation(potentialRecipe, projectDefinition.ProjectPath, priority));
            }

            recommendations = recommendations.OrderByDescending(recommendation => recommendation.ComputedPriority).ToList();
            return recommendations;
        }

        public RulesResult EvaluateRules(ProjectDefinition projectDefinition, IList<RecipeDefinition.RecommendationRuleItem> rules)
        {
            // If there are no rules the recipe must be invalid so don't include it.
            if(false == rules?.Any())
            {
                return new RulesResult { Include = false };
            }

            var results = new RulesResult {Include = true };

            foreach (var rule in rules)
            {
                var allTestPass = true;
                foreach (var test in rule.Tests)
                {
                    switch (test.Type)
                    {
                        case "MSProjectSdkAttribute":
                            allTestPass &= string.Equals(projectDefinition.SdkType, test.Condition.Value, StringComparison.InvariantCultureIgnoreCase);
                            break;

                        case "MSProperty":
                            var propertyValue = projectDefinition.GetMSPropertyValue(test.Condition.PropertyName);
                            allTestPass &= (propertyValue != null && test.Condition.AllowedValues.Contains(propertyValue));
                            break;

                        case "MSPropertyExists":
                            allTestPass &= !string.IsNullOrEmpty(projectDefinition.GetMSPropertyValue(test.Condition.PropertyName));
                            break;

                        case "FileExists":
                            var directory = Path.GetDirectoryName(projectDefinition.ProjectPath);
                            allTestPass &= (Directory.GetFiles(directory, test.Condition.FileName).Length == 1);
                            break;
                        default:
                            throw new InvalidRecipeDefinitionException($"Invalid test type for rule: {test.Type}");
                    }

                    if (!allTestPass)
                        break;
                }

                results.Include &= ShouldInclude(rule.Effect, allTestPass);

                var effectOptions = GetEffectOptions(rule.Effect, allTestPass);

                if(effectOptions != null)
                {
                    if(effectOptions.PriorityAdjustment.HasValue)
                    {
                        results.PriorityAdjustment += effectOptions.PriorityAdjustment.Value;
                    }
                }
            }

            return results;
        }

        public bool ShouldInclude(RecipeDefinition.RuleEffect effect, bool testPass)
        {
            // Get either the pass or fail effect options.
            var effectOptions = GetEffectOptions(effect, testPass);
            if (effectOptions != null)
            {
                if(effectOptions.Include.HasValue)
                {
                    return effectOptions.Include.Value;
                }
                else
                {
                    return true;
                }
                
            }

            return testPass;
        }

        private RecipeDefinition.EffectOptions GetEffectOptions(RecipeDefinition.RuleEffect effect, bool testPass)
        {
            if(effect == null)
            {
                return null;
            }

            return testPass ? effect.Pass : effect.Fail;
        }

        public class RulesResult
        {
            public bool Include { get; set; }

            public int PriorityAdjustment { get; set; }
        }
    }
}
