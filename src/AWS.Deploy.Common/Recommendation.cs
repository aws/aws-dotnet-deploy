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
        /// <summary>
        /// Returns the full path to the project file
        /// </summary>
        public string ProjectPath => ProjectDefinition.ProjectPath;

        public ProjectDefinition ProjectDefinition { get; }

        public RecipeDefinition Recipe { get; }

        public int ComputedPriority { get; }

        public string Name => Recipe.Name;

        public bool IsExistingCloudApplication { get; set; }

        public string Description => Recipe.Description;

        public string ShortDescription => Recipe.ShortDescription;

        public DeploymentBundle DeploymentBundle { get; }

        public readonly Dictionary<string, string> ReplacementTokens = new();

        public Recommendation(RecipeDefinition recipe, ProjectDefinition projectDefinition, int computedPriority, Dictionary<string, string> additionalReplacements)
        {
            additionalReplacements ??= new Dictionary<string, string>();
            Recipe = recipe;

            ComputedPriority = computedPriority;

            ProjectDefinition = projectDefinition;
            DeploymentBundle = new DeploymentBundle();

            CollectRecommendationReplacementTokens(GetConfigurableOptionSettingItems().ToList());

            foreach (var replacement in additionalReplacements)
            {
                ReplacementTokens[replacement.Key] = replacement.Value;
            }
        }

        public List<Category> GetConfigurableOptionSettingCategories()
        {
            var categories = Recipe.Categories;

            // If any top level settings has a category of General make sure the General category is added to the list.
            if(!categories.Any(x => string.Equals(x.Id, Category.General.Id)) &&
                Recipe.OptionSettings.Any(x => string.IsNullOrEmpty(x.Category) || string.Equals(x.Category, Category.General.Id)))
            {
                categories.Insert(0, Category.General);
            }

            // Add the build settings category if it is not already in the list of categories.
            if(!categories.Any(x => string.Equals(x.Id, Category.DeploymentBundle.Id)))
            {
                categories.Add(Category.DeploymentBundle);
            }

            return categories;
        }

        public IEnumerable<OptionSettingItem> GetConfigurableOptionSettingItems()
        {
            // For any top level settings that don't have a category assigned to them assign the General category.
            foreach(var setting in Recipe.OptionSettings)
            {
                if(string.IsNullOrEmpty(setting.Category))
                {
                    setting.Category = Category.General.Id;
                }
            }

            return Recipe.OptionSettings;
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

        /// <summary>
        /// Helper to get the project's directory
        /// </summary>
        /// <returns>Full name of directory containing this recommendation's project file</returns>
        public string GetProjectDirectory()
        {
            var projectDirectory = new FileInfo(ProjectPath).Directory?.FullName;

            if (string.IsNullOrEmpty(projectDirectory))
                throw new InvalidProjectPathException(DeployToolErrorCode.ProjectPathNotFound, "The project path provided is invalid.");

            return projectDirectory;
        }
    }
}
