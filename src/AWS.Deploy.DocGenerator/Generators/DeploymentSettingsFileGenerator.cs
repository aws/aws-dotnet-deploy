// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.Deploy.Common.IO;
using AWS.Deploy.DocGenerator.Utilities;
using AWS.Deploy.ServerMode.Client;

namespace AWS.Deploy.DocGenerator.Generators
{
    /// <summary>
    /// Creates documentation for the deployment settings file that can be used as part of a CI/CD pipeline.
    /// </summary>
    public class DeploymentSettingsFileGenerator : IDocGenerator
    {
        private readonly IRestAPIClient _restAPIClient;
        private readonly IFileManager _fileManager;

        public DeploymentSettingsFileGenerator(IRestAPIClient restAPIClient, IFileManager fileManager)
        {
            _restAPIClient = restAPIClient;
            _fileManager = fileManager;
        }

        /// <summary>
        /// Creates markdown files per recipe that lists all the option settings that can be used in the deployment settings file.
        /// </summary>
        public async Task Generate()
        {
            var recipes = await _restAPIClient.ListAllRecipesAsync(null);

            foreach (var recipeSummary in recipes.Recipes)
            {
                var stringBuilder = new StringBuilder();

                stringBuilder.AppendLine($"**Recipe ID:** {recipeSummary.Id}");
                stringBuilder.AppendLine();
                stringBuilder.AppendLine($"**Recipe Description:** {recipeSummary.Description}");
                stringBuilder.AppendLine();
                stringBuilder.AppendLine("**Settings:**");
                stringBuilder.AppendLine();

                var optionSettings = await _restAPIClient.GetRecipeOptionSettingsAsync(recipeSummary.Id, null);

                GenerateChildSettings(stringBuilder, 0, optionSettings);

                var fullPath = DocGeneratorExtensions.DetermineDocsPath($"content/docs/cicd/recipes/{recipeSummary.Name}.md");
                await _fileManager.WriteAllTextAsync(fullPath, stringBuilder.ToString());
            }
        }

        private void GenerateChildSettings(StringBuilder stringBuilder, int level, ICollection<RecipeOptionSettingSummary> settings)
        {
            var titlePadding = new string(' ', level * 4);
            var detailsPadding = new string(' ', (level + 1) * 4);
            foreach (var setting in settings)
            {
                stringBuilder.AppendLine($"{titlePadding}* **{setting.Name}**");
                stringBuilder.AppendLine($"{detailsPadding}* ID: {setting.Id}");
                stringBuilder.AppendLine($"{detailsPadding}* Description: {setting.Description}");
                stringBuilder.AppendLine($"{detailsPadding}* Type: {setting.Type}");

                if (setting.Settings.Any())
                {
                    stringBuilder.AppendLine($"{detailsPadding}* Settings:");
                    GenerateChildSettings(stringBuilder, level + 2, setting.Settings);
                }
            }
        }
    }
}
