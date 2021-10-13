// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public bool IsExistingCloudApplication { get; private set; }

        public string Description => Recipe.Description;

        public string ShortDescription => Recipe.ShortDescription;

        public DeploymentBundle DeploymentBundle { get; }

        private readonly List<OptionSettingItem> DeploymentBundleSettings = new ();

        private readonly Dictionary<string, string> _replacementTokens = new();

        public Recommendation(RecipeDefinition recipe, ProjectDefinition projectDefinition, List<OptionSettingItem> deploymentBundleSettings, int computedPriority, Dictionary<string, string> additionalReplacements)
        {
            additionalReplacements ??= new Dictionary<string, string>();
            Recipe = recipe;

            ComputedPriority = computedPriority;

            ProjectDefinition = projectDefinition;
            DeploymentBundle = new DeploymentBundle();
            DeploymentBundleSettings = deploymentBundleSettings;

            foreach (var replacement in additionalReplacements)
            {
                if (!_replacementTokens.ContainsKey(replacement.Key))
                    _replacementTokens[replacement.Key] = replacement.Value;
            }
        }

        public Recommendation ApplyPreviousSettings(IDictionary<string, object> previousSettings)
        {
            var recommendation = this.DeepCopy();

            ApplyPreviousSettings(recommendation, previousSettings);

            return recommendation;
        }

        public void AddReplacementToken(string key, string value)
        {
            _replacementTokens[key] = value;
        }

        private void ApplyPreviousSettings(Recommendation recommendation, IDictionary<string, object> previousSettings)
        {
            recommendation.IsExistingCloudApplication = true;

            foreach (var optionSetting in recommendation.Recipe.OptionSettings)
            {
                if (previousSettings.TryGetValue(optionSetting.Id, out var value))
                {
                    optionSetting.SetValueOverride(value);
                }
            }
        }

        public IEnumerable<OptionSettingItem> GetConfigurableOptionSettingItems()
        {
            if (DeploymentBundleSettings == null)
                return Recipe.OptionSettings;

            return Recipe.OptionSettings.Union(DeploymentBundleSettings);
        }

        /// <summary>
        /// Interactively traverses given json path and returns target option setting.
        /// Throws exception if there is no <see cref="OptionSettingItem" /> that matches <paramref name="jsonPath"/> />
        /// </summary>
        /// <param name="jsonPath">
        /// Dot (.) separated key values string pointing to an option setting.
        /// Read more <see href="https://tools.ietf.org/id/draft-goessner-dispatch-jsonpath-00.html"/>
        /// </param>
        /// <returns>Option setting at the json path. Throws <see cref="OptionSettingItemDoesNotExistException"/> if there doesn't exist an option setting.</returns>
        public OptionSettingItem GetOptionSetting(string? jsonPath)
        {
            if (string.IsNullOrEmpty(jsonPath))
                throw new OptionSettingItemDoesNotExistException($"The Option Setting Item {jsonPath} does not exist as part of the" +
                    $" {Recipe.Name} recipe");

            var ids = jsonPath.Split('.');
            OptionSettingItem? optionSetting = null;

            foreach (var id in ids)
            {
                var optionSettings = optionSetting?.ChildOptionSettings ?? GetConfigurableOptionSettingItems();
                optionSetting = optionSettings.FirstOrDefault(os => os.Id.Equals(id));
                if (optionSetting == null)
                {
                    throw new OptionSettingItemDoesNotExistException($"The Option Setting Item {jsonPath} does not exist as part of the" +
                    $" {Recipe.Name} recipe");
                }
            }

            return optionSetting!;
        }

        public T GetOptionSettingValue<T>(OptionSettingItem optionSetting)
        {
            var displayableOptionSettings = new Dictionary<string, bool>();
            if (optionSetting.Type == OptionSettingValueType.Object)
            {
                foreach (var childOptionSetting in optionSetting.ChildOptionSettings)
                {
                    displayableOptionSettings.Add(childOptionSetting.Id, IsOptionSettingDisplayable(childOptionSetting));
                }
            }
            return optionSetting.GetValue<T>(_replacementTokens, displayableOptionSettings);
        }

        public object GetOptionSettingValue(OptionSettingItem optionSetting)
        {
            var displayableOptionSettings = new Dictionary<string, bool>();
            if (optionSetting.Type == OptionSettingValueType.Object)
            {
                foreach (var childOptionSetting in optionSetting.ChildOptionSettings)
                {
                    displayableOptionSettings.Add(childOptionSetting.Id, IsOptionSettingDisplayable(childOptionSetting));
                }
            }
            return optionSetting.GetValue(_replacementTokens, displayableOptionSettings);
        }

        public T? GetOptionSettingDefaultValue<T>(OptionSettingItem optionSetting)
        {
            return optionSetting.GetDefaultValue<T>(_replacementTokens);
        }

        public object? GetOptionSettingDefaultValue(OptionSettingItem optionSetting)
        {
            return optionSetting.GetDefaultValue(_replacementTokens);
        }

        /// <summary>
        /// Checks whether all the dependencies are satisfied or not, if there exists an unsatisfied dependency then returns false.
        /// It allows caller to decide whether we want to display an <see cref="OptionSettingItem"/> to configure or not.
        /// </summary>
        /// <param name="optionSetting">Option setting to check whether it can be displayed for configuration or not.</param>
        /// <returns>Returns true, if all the dependencies are satisfied, else false.</returns>
        public bool IsOptionSettingDisplayable(OptionSettingItem optionSetting)
        {
            if (!optionSetting.DependsOn.Any())
            {
                return true;
            }

            foreach (var dependency in optionSetting.DependsOn)
            {
                var dependsOnOptionSetting = GetOptionSetting(dependency.Id);
                var dependsOnOptionSettingValue = GetOptionSettingValue(dependsOnOptionSetting);
                if (
                    dependsOnOptionSetting != null &&
                    dependsOnOptionSettingValue != null &&
                    !dependsOnOptionSettingValue.Equals(dependency.Value))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks whether the Option Setting Item can be displayed as part of the settings summary of the previous deployment.
        /// </summary>
        public bool IsSummaryDisplayable(OptionSettingItem optionSettingItem)
        {
            if (!IsOptionSettingDisplayable(optionSettingItem))
                return false;

            var value = GetOptionSettingValue(optionSettingItem);
            if (string.IsNullOrEmpty(value?.ToString()))
                return false;

            return true;
        }
    }
}
