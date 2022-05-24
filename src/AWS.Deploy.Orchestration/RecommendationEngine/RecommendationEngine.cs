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

        public async Task<List<Recommendation>> ComputeRecommendations(List<string>? recipeDefinitionPaths = null, Dictionary<string, string>? additionalReplacements = null)
        {
            additionalReplacements ??= new Dictionary<string, string>();

            var recommendations = new List<Recommendation>();
            var availableRecommendations = await _recipeHandler.GetRecipeDefinitions(_orchestratorSession.ProjectDefinition, recipeDefinitionPaths);
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

                var deploymentBundleSettings = GetDeploymentBundleSettings(potentialRecipe.DeploymentBundle);
                recommendations.Add(new Recommendation(potentialRecipe, _orchestratorSession.ProjectDefinition, deploymentBundleSettings, priority, additionalReplacements));
            }

            recommendations = recommendations.OrderByDescending(recommendation => recommendation.ComputedPriority).ThenBy(recommendation => recommendation.Name).ToList();
            return recommendations;
        }

        public List<OptionSettingItem> GetDeploymentBundleSettings(DeploymentBundleTypes deploymentBundleTypes)
        {
            var deploymentBundleDefinitionsPath = DeploymentBundleDefinitionLocator.FindDeploymentBundleDefinitionPath();

            try
            {
                foreach (var deploymentBundleFile in Directory.GetFiles(deploymentBundleDefinitionsPath, "*.deploymentbundle", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        var content = File.ReadAllText(deploymentBundleFile);
                        var definition = JsonConvert.DeserializeObject<DeploymentBundleDefinition>(content);
                        if (definition == null)
                            throw new FailedToDeserializeException(DeployToolErrorCode.FailedToDeserializeDeploymentBundle, $"Failed to Deserialize Deployment Bundle [{deploymentBundleFile}]");
                        if (definition.Type.Equals(deploymentBundleTypes))
                        {
                            // Assign Build category to all of the deployment bundle settings.
                            foreach(var setting in definition.Parameters)
                            {
                                setting.Category = Category.DeploymentBundle.Id;
                            }

                            return definition.Parameters;
                        }
                    }
                    catch (Exception e)
                    {
                        throw new FailedToDeserializeException(DeployToolErrorCode.FailedToDeserializeDeploymentBundle, $"Failed to Deserialize Deployment Bundle [{deploymentBundleFile}]: {e.Message}", e);
                    }
                }
            }
            catch(IOException)
            {
                throw new NoDeploymentBundleDefinitionsFoundException(DeployToolErrorCode.DeploymentBundleDefinitionNotFound, "Failed to find a deployment bundle definition");
            }

            throw new NoDeploymentBundleDefinitionsFoundException(DeployToolErrorCode.DeploymentBundleDefinitionNotFound, "Failed to find a deployment bundle definition");
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
