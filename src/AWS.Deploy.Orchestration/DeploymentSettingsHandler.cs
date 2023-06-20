// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Recipes.Validation;
using AWS.Deploy.Orchestration.Utilities;
using Newtonsoft.Json;

namespace AWS.Deploy.Orchestration
{
    public interface IDeploymentSettingsHandler
    {
        /// <summary>
        /// Read the JSON content at the specified file and deserializes it into <see cref="DeploymentSettings"/>
        /// </summary>
        Task<DeploymentSettings?> ReadSettings(string filePath);

        /// <summary>
        /// Iterates over the option setting values found at <see cref="DeploymentSettings"/> and applies them to the selected recommendation
        /// </summary>
        Task ApplySettings(DeploymentSettings deploymentSettings, Recommendation recommendation, IDeployToolValidationContext deployToolValidationContext);

        /// <summary>
        /// Save the deployment settings at the specified file path.
        /// </summary>
        /// <exception cref="FailedToSaveDeploymentSettingsException">Thrown if this operation fails.</exception>
        Task SaveSettings(SaveSettingsConfiguration saveSettingsConfig, Recommendation recommendation, CloudApplication cloudApplication, OrchestratorSession orchestratorSession);
    }

    public class DeploymentSettingsHandler : IDeploymentSettingsHandler
    {
        private readonly IFileManager _fileManager;
        private readonly IDirectoryManager _directoryManager;
        private readonly IOptionSettingHandler _optionSettingHandler;
        private readonly IRecipeHandler _recipeHandler;

        public DeploymentSettingsHandler(IFileManager fileManager, IDirectoryManager directoryManager, IOptionSettingHandler optionSettingHandler, IRecipeHandler recipeHandler)
        {
            _fileManager = fileManager;
            _directoryManager = directoryManager;
            _optionSettingHandler = optionSettingHandler;
            _recipeHandler = recipeHandler;
        }

        public async Task<DeploymentSettings?> ReadSettings(string filePath)
        {
            if (_fileManager.Exists(filePath))
            {
                try
                {
                    var contents = await _fileManager.ReadAllTextAsync(filePath);
                    var userDeploymentSettings = JsonConvert.DeserializeObject<DeploymentSettings>(contents);
                    return userDeploymentSettings;
                }
                catch (Exception ex)
                {
                    throw new InvalidDeploymentSettingsException(DeployToolErrorCode.FailedToDeserializeUserDeploymentFile, $"An error occurred while trying to deserialize the deployment settings file located at {filePath}.\n  {ex.Message}", ex);
                }
            }
            else
            {
                throw new InvalidDeploymentSettingsException(DeployToolErrorCode.UserDeploymentFileNotFound, $"The deployment settings file located at {filePath} doesn't exist.");
            }
        }

        public async Task ApplySettings(DeploymentSettings deploymentSettings, Recommendation recommendation, IDeployToolValidationContext deployToolValidationContext)
        {
            var optionSettings = deploymentSettings.Settings ?? new Dictionary<string, object>();
            foreach (var entry  in optionSettings)
            {
                try
                {
                    var optionSettingId = entry.Key;
                    var optionSettingValue = entry.Value;
                    var optionSetting = _optionSettingHandler.GetOptionSetting(recommendation, optionSettingId);
                    await _optionSettingHandler.SetOptionSettingValue(recommendation, optionSetting, optionSettingValue, true);
                }
                catch (OptionSettingItemDoesNotExistException ex)
                {
                    throw new InvalidDeploymentSettingsException(DeployToolErrorCode.DeploymentConfigurationNeedsAdjusting, ex.Message, ex);
                }
            }

            var optionSettingValidationFailedResult = _optionSettingHandler.RunOptionSettingValidators(recommendation);
            var recipeValidationFailedResult = _recipeHandler.RunRecipeValidators(recommendation, deployToolValidationContext);

            if (!optionSettingValidationFailedResult.Any() && !recipeValidationFailedResult.Any())
            {
                // All validations are successful
                return;
            }

            var errorMessage = "The deployment configuration needs to be adjusted before it can be deployed:" + Environment.NewLine;
            var failedValidations = optionSettingValidationFailedResult.Concat(recipeValidationFailedResult);

            foreach (var validation in failedValidations)
            {
                errorMessage += validation.ValidationFailedMessage + Environment.NewLine;
            }

            throw new InvalidDeploymentSettingsException(DeployToolErrorCode.DeploymentConfigurationNeedsAdjusting, errorMessage.Trim());
        }

        public async Task SaveSettings(SaveSettingsConfiguration saveSettingsConfig, Recommendation recommendation, CloudApplication cloudApplication, OrchestratorSession orchestratorSession)
        {
            if (saveSettingsConfig.SettingsType == SaveSettingsType.None)
            {
                // We are not throwing an expected exception here as this issue is not caused by the user.
                throw new InvalidOperationException($"Cannot persist settings with {SaveSettingsType.None}");
            }

            if (!_fileManager.IsFileValidPath(saveSettingsConfig.FilePath))
            {
                var message = $"Failed to save deployment settings because {saveSettingsConfig.FilePath} is invalid or its parent directory does not exist on disk.";
                throw new FailedToSaveDeploymentSettingsException(DeployToolErrorCode.FailedToSaveDeploymentSettings, message);
            }

            var projectDirectory = Path.GetDirectoryName(orchestratorSession.ProjectDefinition.ProjectPath);
            if (string.IsNullOrEmpty(projectDirectory))
            {
                var message = "Failed to save deployment settings because the current deployment session does not have a valid project path";
                throw new FailedToSaveDeploymentSettingsException(DeployToolErrorCode.FailedToSaveDeploymentSettings, message);
            }

            var deploymentSettings = new DeploymentSettings
            {
                AWSProfile = orchestratorSession.AWSProfileName,
                AWSRegion = orchestratorSession.AWSRegion,
                ApplicationName = recommendation.Recipe.DeploymentType == DeploymentTypes.ElasticContainerRegistryImage ? null : cloudApplication.Name,
                RecipeId = cloudApplication.RecipeId,
                Settings = _optionSettingHandler.GetOptionSettingsMap(recommendation, orchestratorSession.ProjectDefinition, _directoryManager)
            };

            if (saveSettingsConfig.SettingsType == SaveSettingsType.Modified)
            {
                foreach (var optionSetting in recommendation.GetConfigurableOptionSettingItems())
                {
                    if (!_optionSettingHandler.IsOptionSettingModified(recommendation, optionSetting))
                    {
                        deploymentSettings.Settings.Remove(optionSetting.FullyQualifiedId);
                    }
                }
            }

            try
            {
                var content = JsonConvert.SerializeObject(deploymentSettings, new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    ContractResolver = new SerializeModelContractResolver()
                });

                await _fileManager.WriteAllTextAsync(saveSettingsConfig.FilePath, content);
            }
            catch (Exception ex)
            {
                var message = $"Failed to save the deployment settings at {saveSettingsConfig.FilePath} due to the following error: {Environment.NewLine}{ex.Message}";
                throw new FailedToSaveDeploymentSettingsException(DeployToolErrorCode.FailedToSaveDeploymentSettings, message, ex);
            }
        }
    }
}
