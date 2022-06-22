// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.RecommendationEngine;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AWS.Deploy.CLI.IntegrationTests.Helpers
{
    public class ConfigFileHelper
    {
        /// <summary>
        /// Applies replacement tokens to a file
        /// </summary>
        public static void ApplyReplacementTokens(Dictionary<string, string> replacements, string filePath)
        {
            var content = File.ReadAllText(filePath);
            foreach (var replacement in replacements)
            {
                content = content.Replace(replacement.Key, replacement.Value);
            }
            File.WriteAllText(filePath, content);
        }

        /// <summary>
        /// This method create a JSON config file from the specified recipeId and option setting values
        /// </summary>
        /// <param name="serviceProvider">The dependency injection container</param>
        /// <param name="applicationName">The cloud application name used to uniquely identify the app within AWS. Ex - CloudFormation stack name</param>
        /// <param name="recipeId">The recipeId for the deployment recommendation</param>
        /// <param name="optionSettings">This is a dictionary with FullyQualifiedId as key and their corresponsing option setting values</param>
        /// <param name="projectPath">The path to the .NET application that will be deployed</param>
        /// <param name="configFilePath">The absolute JSON file path where the deployment settings are persisted</param>
        public static async Task CreateConfigFile(IServiceProvider serviceProvider, string applicationName, string recipeId, Dictionary<string, object> optionSettings, string projectPath, string configFilePath, SaveSettingsType saveSettingsType)
        {
            var parser = serviceProvider.GetService<IProjectDefinitionParser>();
            var optionSettingHandler = serviceProvider.GetRequiredService<IOptionSettingHandler>();
            var deploymentSettingHandler = serviceProvider.GetRequiredService<IDeploymentSettingsHandler>();

            var orchestratorSession = new OrchestratorSession(parser.Parse(projectPath).Result);
            var cloudApplication = new CloudApplication(applicationName, "", CloudApplicationResourceType.CloudFormationStack, recipeId);

            var recommendationEngine = new RecommendationEngine(orchestratorSession, serviceProvider.GetService<IRecipeHandler>());
            var recommendations = await recommendationEngine.ComputeRecommendations();
            var selectedRecommendation = recommendations.FirstOrDefault(x => string.Equals(x.Recipe.Id, recipeId));

            foreach (var item in optionSettings)
            {
                await optionSettingHandler.SetOptionSettingValue(selectedRecommendation, item.Key, item.Value, skipValidation: true);
            }

            await deploymentSettingHandler.SaveSettings(new SaveSettingsConfiguration(saveSettingsType, configFilePath), selectedRecommendation, cloudApplication, orchestratorSession);
        }

        /// <summary>
        /// Verifies that the file contents are the same by accounting for os-specific new line delimiters.
        /// </summary>
        /// <returns>true if the contents match, false otherwise</returns>
        public static async Task<bool> VerifyConfigFileContents(string expectedContentPath, string actualContentPath)
        {
            var expectContent = await File.ReadAllTextAsync(expectedContentPath);
            var actualContent = await File.ReadAllTextAsync(actualContentPath);
            actualContent = SanitizeFileContents(actualContent);
            expectContent = SanitizeFileContents(expectContent);
            return string.Equals(expectContent, actualContent);
        }

        private static string SanitizeFileContents(string content)
        {
            return content.Replace("\r\n", Environment.NewLine)
                .Replace("\n", Environment.NewLine)
                .Replace("\r\r\n", Environment.NewLine)
                .Trim();
        }
    }
}
