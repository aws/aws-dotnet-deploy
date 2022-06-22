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
        /// Save the deployment settings at the specified file path. Throws a <see cref="FailedToSaveDeploymentSettingsException"/> if this operation fails.
        /// </summary>
        Task SaveSettings(SaveSettingsConfiguration saveSettingsConfig, Recommendation recommendation, CloudApplication cloudApplication, OrchestratorSession orchestratorSession);

        /// <summary>
        /// Validates the file path where deployment settings will be saved. Ensures that the path is structurally valid and all its parent directory exist on disk.
        /// Throws a <see cref="FailedToSaveDeploymentSettingsException"/> if the validation fails
        /// </summary>
        void ValidateSaveSettingsFile(string filePath);
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
            try
            {
                var contents = await _fileManager.ReadAllTextAsync(filePath);
                var userDeploymentSettings = JsonConvert.DeserializeObject<DeploymentSettings>(contents);
                return userDeploymentSettings;
            }
            catch (Exception ex)
            {
                throw new InvalidDeploymentSettingsException(DeployToolErrorCode.FailedToDeserializeUserDeploymentFile, $"An error occured while trying to deserialize the deployment settings file located at {filePath}", ex);
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

            ValidateSaveSettingsFile(saveSettingsConfig.FilePath);

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
                Settings = new Dictionary<string, object>()
            };

            var optionSettings = recommendation.GetConfigurableOptionSettingItems();
            foreach (var optionSetting in optionSettings)
            {
                if (saveSettingsConfig.SettingsType == SaveSettingsType.Modified && !_optionSettingHandler.IsOptionSettingModified(recommendation, optionSetting))
                {
                    continue;
                }

                var id = optionSetting.FullyQualifiedId;
                var value = _optionSettingHandler.GetOptionSettingValue(recommendation, optionSetting);
                if (optionSetting.TypeHint.HasValue && (optionSetting.TypeHint == OptionSettingTypeHint.FilePath || optionSetting.TypeHint == OptionSettingTypeHint.DockerExecutionDirectory))
                {
                    var path = value?.ToString();
                    if (string.IsNullOrEmpty(path))
                    {
                        continue;
                    }

                    var absolutePath = _directoryManager.GetAbsolutePath(projectDirectory, path);
                    value = _directoryManager.GetRelativePath(projectDirectory, absolutePath)
                                .Replace(Path.DirectorySeparatorChar, '/');
                }
                deploymentSettings.Settings[id] = value;
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

        public void ValidateSaveSettingsFile(string filePath)
        {
            var parentDirectory = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(parentDirectory))
            {
                var message = $"Failed to save deployment settings because {filePath} is not a valid path";
                throw new FailedToSaveDeploymentSettingsException(DeployToolErrorCode.FailedToSaveDeploymentSettings, message);
            }
            if (!_directoryManager.Exists(parentDirectory))
            {
                var message = $"Failed to save deployment settings because {filePath} does not exist on disk";
                throw new FailedToSaveDeploymentSettingsException(DeployToolErrorCode.FailedToSaveDeploymentSettings, message);
            }
        }
    }
}
