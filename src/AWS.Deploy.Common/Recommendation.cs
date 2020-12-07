// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AWS.DeploymentCommon
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

        public object GetOptionSettingValue(string settingId, bool ignoreDefaultValue = false)
        {
            if (_overrideOptionSettingValues.TryGetValue(settingId, out var value))
            {
                return value;
            }

            if (ignoreDefaultValue)
                return null;

            var defaultValue = Recipe.OptionSettings.FirstOrDefault((x) => string.Equals(x.Id, settingId, StringComparison.InvariantCultureIgnoreCase))?.DefaultValue;
            if (defaultValue == null)
                return string.Empty;

            defaultValue = ApplyReplacementTokens(defaultValue);
            return defaultValue;
        }

        public void SetOverrideOptionSettingValue(string settingId, object value)
        {
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
