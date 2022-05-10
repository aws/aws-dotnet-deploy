// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AWS.Deploy.Common.Extensions;
using AWS.Deploy.Common.Recipes;

namespace AWS.Deploy.Common
{
    public class Recommendation : IUserInputOption
    {
        public string ProjectPath => ProjectDefinition.ProjectPath;

        public ProjectDefinition ProjectDefinition { get; }

        public RecipeDefinition Recipe { get; }

        public int ComputedPriority { get; }

        public string Name => Recipe.Name;

        public bool IsExistingCloudApplication { get; set; }

        public string Description => Recipe.Description;

        public string ShortDescription => Recipe.ShortDescription;

        public DeploymentBundle DeploymentBundle { get; }

        public readonly List<OptionSettingItem> DeploymentBundleSettings = new ();

        public readonly Dictionary<string, string> ReplacementTokens = new();

        public Recommendation(RecipeDefinition recipe, ProjectDefinition projectDefinition, List<OptionSettingItem> deploymentBundleSettings, int computedPriority, Dictionary<string, string> additionalReplacements)
        {
            additionalReplacements ??= new Dictionary<string, string>();
            Recipe = recipe;

            ComputedPriority = computedPriority;

            ProjectDefinition = projectDefinition;
            DeploymentBundle = new DeploymentBundle();
            DeploymentBundleSettings = deploymentBundleSettings;

            CollectRecommendationReplacementTokens(GetConfigurableOptionSettingItems().ToList());

            foreach (var replacement in additionalReplacements)
            {
                ReplacementTokens[replacement.Key] = replacement.Value;
            }
        }

        public IEnumerable<OptionSettingItem> GetConfigurableOptionSettingItems()
        {
            if (DeploymentBundleSettings == null)
                return Recipe.OptionSettings;

            return Recipe.OptionSettings.Union(DeploymentBundleSettings);
        }

        private void CollectRecommendationReplacementTokens(List<OptionSettingItem> optionSettings)
        {
            foreach (var optionSetting in optionSettings)
            {
                string defaultValue = optionSetting.DefaultValue?.ToString() ?? "";
                Regex regex = new Regex(@"^.*\{[\w\d]+\}.*$");
                Match match = regex.Match(defaultValue);

                if (match.Success)
                {
                    var replacement = defaultValue.Substring(defaultValue.IndexOf("{"), defaultValue.IndexOf("}") + 1);
                    ReplacementTokens[replacement] = "";
                }

                if (optionSetting.ChildOptionSettings.Any())
                    CollectRecommendationReplacementTokens(optionSetting.ChildOptionSettings);
            }
        }

        public void AddReplacementToken(string key, string value)
        {
            ReplacementTokens[key] = value;
        }
    }
}
