// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using Newtonsoft.Json;

namespace AWS.Deploy.Orchestration.RecommendationEngine
{
    public class RecommendationEngine
    {
        private readonly IList<RecipeDefinition> _availableRecommendations = new List<RecipeDefinition>();
        private readonly OrchestratorSession _orchestratorSession;

        public RecommendationEngine(IEnumerable<string> recipeDefinitionPaths, OrchestratorSession orchestratorSession)
        {
            _orchestratorSession = orchestratorSession;

            recipeDefinitionPaths ??= new List<string>();

            foreach (var recommendationPath in recipeDefinitionPaths)
            {
                foreach (var recipeFile in Directory.GetFiles(recommendationPath, "*.recipe", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        var content = File.ReadAllText(recipeFile);
                        var definition = JsonConvert.DeserializeObject<RecipeDefinition>(content);
                        definition.RecipePath = recipeFile;

                        _availableRecommendations.Add(definition);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Failed to Deserialize Recipe [{recipeFile}]: {e.Message}", e);
                    }
                }
            }
        }

        public async Task<List<Recommendation>> ComputeRecommendations(string projectPath, Dictionary<string, string> additionalReplacements)
        {
            var projectDefinition = new ProjectDefinition(projectPath);
            var recommendations = new List<Recommendation>();

            foreach (var potentialRecipe in _availableRecommendations)
            {
                var results = await EvaluateRules(projectDefinition, potentialRecipe.RecommendationRules);
                if(!results.Include)
                {
                    continue;
                }

                var priority = potentialRecipe.RecipePriority + results.PriorityAdjustment;
                // Recipes with a negative priority are ignored.
                if (priority < 0)
                {
                    continue;
                }

                recommendations.Add(new Recommendation(potentialRecipe, projectDefinition.ProjectPath, priority, additionalReplacements));
            }

            recommendations = recommendations.OrderByDescending(recommendation => recommendation.ComputedPriority).ToList();
            return recommendations;
        }

        public async Task<RulesResult> EvaluateRules(ProjectDefinition projectDefinition, IList<RecommendationRuleItem> rules)
        {
            // If there are no rules the recipe must be invalid so don't include it.
            if (false == rules?.Any())
            {
                return new RulesResult { Include = false };
            }

            var availableTests = RecommendationTestFactory.LoadAvailableTests();
            var results = new RulesResult {Include = true };

            foreach (var rule in rules)
            {
                var allTestPass = true;
                foreach (var test in rule.Tests)
                {
                    if(!availableTests.TryGetValue(test.Type, out var testInstance))
                    {
                        throw new InvalidRecipeDefinitionException($"Invalid test type [{test.Type}] found in rule.");
                    }

                    var input = new RecommendationTestInput
                    {
                        Test = test,
                        ProjectDefinition = projectDefinition,
                        Session = _orchestratorSession
                    };
                    allTestPass &= await testInstance.Execute(input);

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

        public bool ShouldInclude(RuleEffect effect, bool testPass)
        {
            // Get either the pass or fail effect options.
            var effectOptions = GetEffectOptions(effect, testPass);
            if (effectOptions != null)
            {
                if (effectOptions.Include.HasValue)
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

        private EffectOptions GetEffectOptions(RuleEffect effect, bool testPass)
        {
            if (effect == null)
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
