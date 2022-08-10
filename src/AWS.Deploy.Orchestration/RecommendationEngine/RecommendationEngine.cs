// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
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

namespace AWS.Deploy.Orchestration.RecommendationEngine
{
    public class RecommendationEngine
    {
        private readonly OrchestratorSession _orchestratorSession;
        private readonly IRecipeHandler _recipeHandler;

        public RecommendationEngine(OrchestratorSession orchestratorSession, IRecipeHandler recipeHandler)
        {
            _orchestratorSession = orchestratorSession;
            _recipeHandler = recipeHandler;
        }

        public async Task<List<Recommendation>> ComputeRecommendations(List<string>? recipeDefinitionPaths = null, Dictionary<string, object>? additionalReplacements = null)
        {
            additionalReplacements ??= new Dictionary<string, object>();

            var recommendations = new List<Recommendation>();
            var availableRecommendations = await _recipeHandler.GetRecipeDefinitions(recipeDefinitionPaths);
            foreach (var potentialRecipe in availableRecommendations)
            {
                var results = await EvaluateRules(potentialRecipe.RecommendationRules);
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

                recommendations.Add(new Recommendation(potentialRecipe, _orchestratorSession.ProjectDefinition, priority, additionalReplacements));
            }

            recommendations = recommendations.OrderByDescending(recommendation => recommendation.ComputedPriority).ThenBy(recommendation => recommendation.Name).ToList();
            return recommendations;
        }

        public async Task<RulesResult> EvaluateRules(IList<RecommendationRuleItem> rules)
        {
            // If there are no rules the recipe must be invalid so don't include it.
            if (false == rules?.Any())
            {
                return new RulesResult { Include = false };
            }

            var availableTests = RecommendationTestFactory.LoadAvailableTests();
            var results = new RulesResult {Include = true };

            foreach (var rule in rules!)
            {
                var allTestPass = true;
                foreach (var test in rule.Tests)
                {
                    if(!availableTests.TryGetValue(test.Type, out var testInstance))
                    {
                        throw new InvalidRecipeDefinitionException(DeployToolErrorCode.RuleHasInvalidTestType, $"Invalid test type [{test.Type}] found in rule.");
                    }

                    var input = new RecommendationTestInput(
                        test,
                        _orchestratorSession.ProjectDefinition,
                        _orchestratorSession);

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

        public bool ShouldInclude(RuleEffect? effect, bool testPass)
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

        private EffectOptions? GetEffectOptions(RuleEffect? effect, bool testPass)
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
