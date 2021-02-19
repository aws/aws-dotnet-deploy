// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.IO;
using System.Linq;
using AWS.Deploy.Common.Recipes;

namespace AWS.Deploy.Common
{
    public class Recommendation : IUserInputOption
    {
        private const string REPLACE_TOKEN_PROJECT_NAME = "{ProjectName}";

        public string ProjectPath { get; }

        public ProjectDefinition ProjectDefinition { get; }

        public RecipeDefinition Recipe { get; }
        public int ComputedPriority { get; }

        public string Name => Recipe.Name;

        public string Description => Recipe.Description;

        private readonly Dictionary<string, string> _replacementTokens = new();

        public Recommendation(RecipeDefinition recipe, string projectPath, int computedPriority, Dictionary<string, string> additionalReplacements)
        {
            Recipe = recipe;
            ProjectPath = projectPath;
            ComputedPriority = computedPriority;

            ProjectDefinition = new ProjectDefinition(projectPath);

            if (File.Exists(projectPath))
            {
                _replacementTokens[REPLACE_TOKEN_PROJECT_NAME] = Path.GetFileNameWithoutExtension(ProjectPath);
            }

            foreach (var replacement in additionalReplacements)
            {
                if (!_replacementTokens.ContainsKey(replacement.Key))
                    _replacementTokens[replacement.Key] = replacement.Value;
            }
        }

        public void ApplyPreviousSettings(IDictionary<string, object> previousSettings)
        {
            if (previousSettings == null)
                return;

            ApplyPreviousSettings(Recipe.OptionSettings, previousSettings);
        }

        private void ApplyPreviousSettings(IEnumerable<OptionSettingItem> optionSettings, IDictionary<string, object> previousSettings)
        {
            foreach (var optionSetting in optionSettings)
            {
                if (previousSettings.TryGetValue(optionSetting.Id, out var value))
                {
                    optionSetting.SetValueOverride(value);
                }
            }
        }

        /// <summary>
        /// Interactively traverses given json path and returns target option setting.
        /// Returns null if there is no <see cref="OptionSettingItem" /> that matches <paramref name="jsonPath"/> />
        /// </summary>
        /// <param name="jsonPath">
        /// Dot (.) separated key values string pointing to an option setting.
        /// Read more <see href="https://tools.ietf.org/id/draft-goessner-dispatch-jsonpath-00.html"/>
        /// </param>
        /// <returns>Option setting at the json path. Returns null if, there doesn't exist an option setting.</returns>
        public OptionSettingItem GetOptionSetting(string jsonPath)
        {
            var ids = jsonPath.Split('.');
            OptionSettingItem optionSetting = null;

            foreach (var id in ids)
            {
                var optionSettings = optionSetting?.ChildOptionSettings ?? Recipe.OptionSettings;
                optionSetting = optionSettings.FirstOrDefault(os => os.Id.Equals(id));
                if (optionSetting == null)
                {
                    return null;
                }
            }

            return optionSetting;
        }

        public T GetOptionSettingValue<T>(OptionSettingItem optionSetting, bool ignoreDefaultValue = false)
        {
            return optionSetting.GetValue<T>(_replacementTokens, ignoreDefaultValue);
        }

        public object GetOptionSettingValue(OptionSettingItem optionSetting, bool ignoreDefaultValue = false)
        {
            return optionSetting.GetValue(_replacementTokens, ignoreDefaultValue);
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
                if (dependsOnOptionSetting != null && !GetOptionSettingValue(dependsOnOptionSetting).Equals(dependency.Value))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
