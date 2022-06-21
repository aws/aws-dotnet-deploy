// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
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
    }

    public class DeploymentSettingsHandler : IDeploymentSettingsHandler
    {
        private readonly IFileManager _filemanager;
        private readonly IOptionSettingHandler _optionSettingHandler;
        private readonly IRecipeHandler _recipeHandler;

        public DeploymentSettingsHandler(IFileManager fileManager, IOptionSettingHandler optionSettingHandler, IRecipeHandler recipeHandler)
        {
            _filemanager = fileManager;
            _optionSettingHandler = optionSettingHandler;
            _recipeHandler = recipeHandler;
        }

        public async Task<DeploymentSettings?> ReadSettings(string filePath)
        {
            try
            {
                var contents = await _filemanager.ReadAllTextAsync(filePath);
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
    }
}
