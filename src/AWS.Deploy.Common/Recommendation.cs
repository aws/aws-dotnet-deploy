// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AWS.Deploy.Common
{
    public class Recommendation : IUserInputOption
    {
        private const string REPLACE_TOKEN_PROJECTNAME = "{ProjectName}";
        
        public string ProjectPath { get; }

        public ProjectDefinition ProjectDefinition { get; }

        public RecipeDefinition Recipe { get; }
        public int ComputedPriority { get; }

        public string Name => Recipe.Name;

        public string Description => Recipe.Description;

        private readonly IDictionary<string, object> _overrideOptionSettingValues = new Dictionary<string, object>();

        public Recommendation(RecipeDefinition recipe, string projectPath, int computedPriority)
        {
            Recipe = recipe;
            ProjectPath = projectPath;
            ComputedPriority = computedPriority;

            ProjectDefinition = new ProjectDefinition(projectPath);
        }

        public void ApplyPreviousSettings(IDictionary<string, object> previousSettings)
        {
            if (previousSettings == null)
                return;

            foreach (var option in Recipe.OptionSettings)
            {
                if (previousSettings.TryGetValue(option.Id, out var value))
                {
                    SetOverrideOptionSettingValue(option.Id, value);
                }
            }
        }

        public ICollection<string> ListOptionSettings()
        {
            return _overrideOptionSettingValues.Keys;
        }

        public object GetOptionSettingValue(string settingId, bool ignoreDefaultValue = false)
        {
            if (_overrideOptionSettingValues.TryGetValue(settingId, out var value))
            {
                return value;
            }

            if (ignoreDefaultValue)
                return null;

            var setting = Recipe.OptionSettings.FirstOrDefault((x) => string.Equals(x.Id, settingId, StringComparison.InvariantCultureIgnoreCase));
            var defaultValue = setting?.DefaultValue;
            if (defaultValue == null)
                return string.Empty;

            if(setting.ValueMapping != null && setting.ValueMapping.ContainsKey(defaultValue))
            {
                defaultValue = setting.ValueMapping[defaultValue];
            }

            defaultValue = ApplyReplacementTokens(defaultValue);
            return defaultValue;
        }

        public void SetOverrideOptionSettingValue(string settingId, object value)
        {
            var setting = Recipe.OptionSettings.FirstOrDefault((x) => string.Equals(x.Id, settingId, StringComparison.InvariantCultureIgnoreCase));
            if (setting != null && value != null && setting.ValueMapping != null && setting.ValueMapping.ContainsKey(value.ToString()))
            {
                value = setting.ValueMapping[value.ToString()];
            }

            _overrideOptionSettingValues[settingId] = value;
        }

        public string ApplyReplacementTokens(string defaultValue)
        {
            var projectName = Path.GetFileNameWithoutExtension(ProjectPath);
            defaultValue = defaultValue.Replace(REPLACE_TOKEN_PROJECTNAME, projectName);
            return defaultValue;
        }
    }
}
