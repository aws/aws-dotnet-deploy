// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Extensions;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Recipes.Validation;

namespace AWS.Deploy.Orchestration
{
    public class OptionSettingHandler : IOptionSettingHandler
    {
        private readonly IValidatorFactory _validatorFactory;

        public OptionSettingHandler(IValidatorFactory validatorFactory)
        {
            _validatorFactory = validatorFactory;
        }

        /// <summary>
        /// This method runs all the option setting validators for the configurable settings.
        /// In case of a first time deployment, all settings and validators are run.
        /// In case of a redeployment, only the updatable settings are considered.
        /// </summary>
        public List<ValidationResult> RunOptionSettingValidators(Recommendation recommendation, IEnumerable<OptionSettingItem>? optionSettings = null)
        {
            if (optionSettings == null)
                optionSettings = recommendation.GetConfigurableOptionSettingItems().Where(x => !recommendation.IsExistingCloudApplication || x.Updatable);

            List<ValidationResult> settingValidatorFailedResults = new List<ValidationResult>();
            foreach (var optionSetting in optionSettings)
            {
                if (!IsOptionSettingDisplayable(recommendation, optionSetting))
                {
                    optionSetting.Validation.ValidationStatus = ValidationStatus.Valid;
                    optionSetting.Validation.ValidationMessage = string.Empty;
                    optionSetting.Validation.InvalidValue = null;
                    continue;
                }

                var optionSettingValue = GetOptionSettingValue(recommendation, optionSetting);
                var failedValidators = _validatorFactory.BuildValidators(optionSetting)
                    .Select(async validator => await validator.Validate(optionSettingValue, recommendation, optionSetting))
                    .Select(x => x.Result)
                    .Where(x => !x.IsValid)
                    .ToList();

                if (failedValidators.Any())
                {
                    optionSetting.Validation.ValidationStatus = ValidationStatus.Invalid;
                    optionSetting.Validation.ValidationMessage = string.Join(Environment.NewLine, failedValidators.Select(x => x.ValidationFailedMessage)).Trim();
                    optionSetting.Validation.InvalidValue = optionSettingValue;
                }
                else
                {
                    optionSetting.Validation.ValidationStatus = ValidationStatus.Valid;
                    optionSetting.Validation.ValidationMessage = string.Empty;
                    optionSetting.Validation.InvalidValue = null;
                }
                settingValidatorFailedResults.AddRange(failedValidators);
                settingValidatorFailedResults.AddRange(RunOptionSettingValidators(recommendation, optionSetting.ChildOptionSettings));
            }

            return settingValidatorFailedResults;
        }

        /// <summary>
        /// Assigns a value to the OptionSettingItem.
        /// </summary>
        /// <exception cref="ValidationFailedException">
        /// Thrown if one or more <see cref="Validators"/> determine
        /// <paramref name="value"/> is not valid.
        /// </exception>
        public async Task SetOptionSettingValue(Recommendation recommendation, OptionSettingItem optionSettingItem, object value, bool skipValidation = false)
        {
            IOptionSettingItemValidator[] validators = new IOptionSettingItemValidator[0];
            if (!skipValidation && IsOptionSettingDisplayable(recommendation, optionSettingItem))
                validators = _validatorFactory.BuildValidators(optionSettingItem);
            
            await optionSettingItem.SetValue(this, value, validators, recommendation, skipValidation);

            if (!skipValidation)
                RunOptionSettingValidators(recommendation, optionSettingItem.Dependents.Select(x => GetOptionSetting(recommendation, x)));
            
            // If the optionSettingItem came from the selected recommendation's deployment bundle,
            // set the corresponding property on recommendation.DeploymentBundle
            SetDeploymentBundleProperty(recommendation, optionSettingItem, value);
        }

        /// <summary>
        /// Assigns a value to the OptionSettingItem based on the fullyQualifiedId
        /// </summary>
        /// <exception cref="ValidationFailedException">
        /// Thrown if one or more <see cref="Validators"/> determine
        /// <paramref name="value"/> is not valid.
        /// </exception>
        /// <exception cref="OptionSettingItemDoesNotExistException">
        /// Thrown if there doesn't exist an option setting with the given fullyQualifiedId
        /// </exception>
        public async Task SetOptionSettingValue(Recommendation recommendation, string fullyQualifiedId, object value, bool skipValidation = false)
        {
            var optionSetting = GetOptionSetting(recommendation, fullyQualifiedId);
            await SetOptionSettingValue(recommendation, optionSetting, value, skipValidation);
        }

        /// <summary>
        /// Sets the corresponding value in <see cref="DeploymentBundle"/> when the
        /// corresponding <see cref="OptionSettingItem"> was just set
        /// </summary>
        /// <param name="recommendation">Selected recommendation</param>
        /// <param name="optionSettingItem">Option setting that was just set</param>
        /// <param name="value">Value that was just set, assumed to be valid</param>
        private void SetDeploymentBundleProperty(Recommendation recommendation, OptionSettingItem optionSettingItem, object value)
        {
            switch (optionSettingItem.Id)
            {
                case Constants.Docker.DockerExecutionDirectoryOptionId:
                    recommendation.DeploymentBundle.DockerExecutionDirectory = value.ToString() ?? string.Empty;
                    break;
                case Constants.Docker.DockerfileOptionId:
                    recommendation.DeploymentBundle.DockerfilePath = value.ToString() ?? string.Empty;
                    break;
                case Constants.Docker.DockerBuildArgsOptionId:
                    recommendation.DeploymentBundle.DockerBuildArgs = value.ToString() ?? string.Empty;
                    break;
                case Constants.Docker.ECRRepositoryNameOptionId:
                    recommendation.DeploymentBundle.ECRRepositoryName = value.ToString() ?? string.Empty;
                    break;
                case Constants.RecipeIdentifier.DotnetPublishConfigurationOptionId:
                    recommendation.DeploymentBundle.DotnetPublishBuildConfiguration = value.ToString() ?? string.Empty;
                    break;
                case Constants.RecipeIdentifier.DotnetPublishArgsOptionId:
                    recommendation.DeploymentBundle.DotnetPublishAdditionalBuildArguments = value.ToString() ?? string.Empty;
                    break;
                case Constants.RecipeIdentifier.DotnetPublishSelfContainedBuildOptionId:
                    recommendation.DeploymentBundle.DotnetPublishSelfContainedBuild = Convert.ToBoolean(value);
                    break;
                default:
                    return;
            }
        }

        /// <summary>
        /// Interactively traverses given json path and returns target option setting.
        /// Throws exception if there is no <see cref="OptionSettingItem" /> that matches <paramref name="jsonPath"/> />
        /// In case an option setting of type <see cref="OptionSettingValueType.KeyValue"/> is encountered,
        /// that <paramref name="jsonPath"/> can have the key value pair name as the leaf node with the option setting Id as the node before that.
        /// </summary>
        /// <param name="jsonPath">
        /// Dot (.) separated key values string pointing to an option setting.
        /// Read more <see href="https://tools.ietf.org/id/draft-goessner-dispatch-jsonpath-00.html"/>
        /// </param>
        /// <returns>Option setting at the json path. Throws <see cref="OptionSettingItemDoesNotExistException"/> if there doesn't exist an option setting.</returns>
        public OptionSettingItem GetOptionSetting(Recommendation recommendation, string? jsonPath)
        {
            if (string.IsNullOrEmpty(jsonPath))
                throw new OptionSettingItemDoesNotExistException(DeployToolErrorCode.OptionSettingItemDoesNotExistInRecipe, $"The Option Setting Item {jsonPath} does not exist as part of the" +
                    $" {recommendation.Recipe.Name} recipe");

            var ids = jsonPath.Split('.');
            OptionSettingItem? optionSetting = null;

            for (int i = 0; i < ids.Length; i++)
            {
                var optionSettings = optionSetting?.ChildOptionSettings ?? recommendation.GetConfigurableOptionSettingItems();
                optionSetting = optionSettings.FirstOrDefault(os => os.Id.Equals(ids[i]));
                if (optionSetting == null)
                {
                    throw new OptionSettingItemDoesNotExistException(DeployToolErrorCode.OptionSettingItemDoesNotExistInRecipe, $"The Option Setting Item {jsonPath} does not exist as part of the" +
                    $" {recommendation.Recipe.Name} recipe");
                }
                if (optionSetting.Type.Equals(OptionSettingValueType.KeyValue))
                {
                    return optionSetting;
                }
            }

            return optionSetting!;
        }

        /// <summary>
        /// Interactively traverses given json path and returns target option setting.
        /// Throws exception if there is no <see cref="OptionSettingItem" /> that matches <paramref name="jsonPath"/> />
        /// In case an option setting of type <see cref="OptionSettingValueType.KeyValue"/> is encountered,
        /// that <paramref name="jsonPath"/> can have the key value pair name as the leaf node with the option setting Id as the node before that.
        /// </summary>
        /// <param name="jsonPath">
        /// Dot (.) separated key values string pointing to an option setting.
        /// Read more <see href="https://tools.ietf.org/id/draft-goessner-dispatch-jsonpath-00.html"/>
        /// </param>
        /// <returns>Option setting at the json path. Throws <see cref="OptionSettingItemDoesNotExistException"/> if there doesn't exist an option setting.</returns>
        public OptionSettingItem GetOptionSetting(RecipeDefinition recipe, string? jsonPath)
        {
            if (string.IsNullOrEmpty(jsonPath))
                throw new OptionSettingItemDoesNotExistException(DeployToolErrorCode.OptionSettingItemDoesNotExistInRecipe, $"An option setting item with the specified fully qualified Id '{jsonPath}' cannot be found in the" +
                    $" '{recipe.Name}' recipe.");

            var ids = jsonPath.Split('.');
            OptionSettingItem? optionSetting = null;

            for (int i = 0; i < ids.Length; i++)
            {
                var optionSettings = optionSetting?.ChildOptionSettings ?? recipe.OptionSettings;
                optionSetting = optionSettings.FirstOrDefault(os => os.Id.Equals(ids[i]));
                if (optionSetting == null)
                {
                    throw new OptionSettingItemDoesNotExistException(DeployToolErrorCode.OptionSettingItemDoesNotExistInRecipe, $"An option setting item with the Id '{ids[i]}' " +
                        $"cannot be found in the '{recipe.Name}' recipe while processing fully qualified Id {jsonPath}.");
                }
                if (optionSetting.Type.Equals(OptionSettingValueType.KeyValue))
                {
                    return optionSetting;
                }
            }

            return optionSetting!;
        }

        /// <summary>
        /// Retrieves the value of the Option Setting Item in a given recommendation.
        /// </summary>
        public T? GetOptionSettingValue<T>(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var displayableOptionSettings = new Dictionary<string, bool>();
            if (optionSetting.Type == OptionSettingValueType.Object)
            {
                foreach (var childOptionSetting in optionSetting.ChildOptionSettings)
                {
                    displayableOptionSettings.Add(childOptionSetting.Id, IsOptionSettingDisplayable(recommendation, childOptionSetting));
                }
            }
            return optionSetting.GetValue<T>(recommendation.ReplacementTokens, displayableOptionSettings);
        }


        /// <summary>
        /// Retrieves the value of the Option Setting Item in a given recommendation.
        /// </summary>
        public object GetOptionSettingValue(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var displayableOptionSettings = new Dictionary<string, bool>();
            if (optionSetting.Type == OptionSettingValueType.Object)
            {
                foreach (var childOptionSetting in optionSetting.ChildOptionSettings)
                {
                    displayableOptionSettings.Add(childOptionSetting.Id, IsOptionSettingDisplayable(recommendation, childOptionSetting));
                }
            }
            return optionSetting.GetValue(recommendation.ReplacementTokens, displayableOptionSettings);
        }

        /// <summary>
        /// Retrieves the default value of the Option Setting Item in a given recommendation.
        /// </summary>
        public T? GetOptionSettingDefaultValue<T>(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            return optionSetting.GetDefaultValue<T>(recommendation.ReplacementTokens);
        }

        /// <summary>
        /// Retrieves the default value of the Option Setting Item in a given recommendation.
        /// </summary>
        public object? GetOptionSettingDefaultValue(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            return optionSetting.GetDefaultValue(recommendation.ReplacementTokens);
        }

        /// <summary>
        /// Checks whether all the dependencies are satisfied or not, if there exists an unsatisfied dependency then returns false.
        /// It allows caller to decide whether we want to display an <see cref="OptionSettingItem"/> to configure or not.
        /// </summary>
        /// <returns>Returns true, if all the dependencies are satisfied, else false.</returns>
        public bool IsOptionSettingDisplayable(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            if (!optionSetting.DependsOn.Any())
            {
                return true;
            }

            foreach (var dependency in optionSetting.DependsOn)
            {
                var dependsOnOptionSetting = GetOptionSetting(recommendation, dependency.Id);
                var dependsOnOptionSettingValue = GetOptionSettingValue(recommendation, dependsOnOptionSetting);
                if (
                    dependsOnOptionSetting != null)
                {
                    if (dependsOnOptionSettingValue == null)
                    {
                        if (dependency.Operation == null ||
                               dependency.Operation == PropertyDependencyOperationType.Equals)
                        {
                            if (dependency.Value != null)
                                return false;
                        }
                        else if (dependency.Operation == PropertyDependencyOperationType.NotEmpty)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (dependency.Operation == null ||
                               dependency.Operation == PropertyDependencyOperationType.Equals)
                        {
                            if (!dependsOnOptionSettingValue.Equals(dependency.Value))
                                return false;
                        }
                        else if (dependency.Operation == PropertyDependencyOperationType.NotEmpty)
                        {
                            if (dependsOnOptionSetting.Type == OptionSettingValueType.List &&
                                dependsOnOptionSettingValue.TryDeserialize<SortedSet<string>>(out var listValue) &&
                                !listValue.Any())
                            {
                                return false;
                            }
                            else if (dependsOnOptionSetting.Type == OptionSettingValueType.KeyValue &&
                                dependsOnOptionSettingValue.TryDeserialize<Dictionary<string, string>>(out var keyValue) &&
                                !keyValue.Any())
                            {
                                return false;
                            }
                            else if (string.IsNullOrEmpty(dependsOnOptionSettingValue?.ToString()))
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Checks whether the Option Setting Item can be displayed as part of the settings summary of the previous deployment.
        /// </summary>
        public bool IsSummaryDisplayable(Recommendation recommendation, OptionSettingItem optionSettingItem)
        {
            if (!IsOptionSettingDisplayable(recommendation, optionSettingItem))
                return false;

            var value = GetOptionSettingValue(recommendation, optionSettingItem);
            if (string.IsNullOrEmpty(value?.ToString()))
                return false;

            return true;
        }

        /// <summary>
        /// Checks whether the option setting item has been modified by the user. If it has been modified, then it will hold a non-default value
        /// </summary>
        /// <returns>true if the option setting item has been modified or false otherwise</returns>
        public bool IsOptionSettingModified(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            // If the option setting is not displayable, that means its dependencies are not satisfied and it does not play any role in the deployment.
            // We do not need to evaluate whether it has been modified or not.
            if (!IsOptionSettingDisplayable(recommendation, optionSetting))
            {
                return false;
            }

            if (optionSetting.Type.Equals(OptionSettingValueType.List))
            {
                var currentSet = GetOptionSettingValue<SortedSet<string>>(recommendation, optionSetting) ?? new SortedSet<string>();
                var defaultSet = GetOptionSettingDefaultValue<SortedSet<string>>(recommendation, optionSetting) ?? new SortedSet<string>();

                // return true if both have different lengths or all elements in currentSet are not present in defaultSet
                return defaultSet.Count != currentSet.Count || !currentSet.All(x => defaultSet.Contains(x));
            }

            if (optionSetting.Type.Equals(OptionSettingValueType.KeyValue))
            {
                var currentDict = GetOptionSettingValue<Dictionary<string, string>>(recommendation, optionSetting) ?? new Dictionary<string, string>();
                var defaultDict = GetOptionSettingDefaultValue<Dictionary<string, string>>(recommendation, optionSetting) ?? new Dictionary<string, string>();

                // return true if both have different lengths or all keyValue pairs are not equal between currentDict and defaultDict
                return defaultDict.Count != currentDict.Count ||
                    !currentDict.All(keyPair => defaultDict.ContainsKey(keyPair.Key) && string.Equals(defaultDict[keyPair.Key], currentDict[keyPair.Key]));
            }

            if (optionSetting.Type.Equals(OptionSettingValueType.Int))
            {
                var currentValue = GetOptionSettingValue<int>(recommendation, optionSetting);
                var defaultValue = GetOptionSettingDefaultValue<int>(recommendation, optionSetting);
                return defaultValue != currentValue;
            }

            if (optionSetting.Type.Equals(OptionSettingValueType.Double))
            {
                var currentValue = GetOptionSettingValue<double>(recommendation, optionSetting);
                var defaultValue = GetOptionSettingDefaultValue<double>(recommendation, optionSetting);
                return defaultValue != currentValue;
            }

            if (optionSetting.Type.Equals(OptionSettingValueType.Bool))
            {
                var currentValue = GetOptionSettingValue<bool>(recommendation, optionSetting);
                var defaultValue = GetOptionSettingDefaultValue<bool>(recommendation, optionSetting);
                return defaultValue != currentValue;
            }

            if (optionSetting.Type.Equals(OptionSettingValueType.String))
            {
                var currentValue = GetOptionSettingValue<string>(recommendation, optionSetting);
                var defaultValue = GetOptionSettingDefaultValue<string>(recommendation, optionSetting);

                if (string.IsNullOrEmpty(currentValue) && string.IsNullOrEmpty(defaultValue))
                    return false;

                return !string.Equals(currentValue, defaultValue);
            }

            // The option setting is of type Object and it has nested child settings.
            // return true is any of the child settings are modified.
            foreach (var childSetting in optionSetting.ChildOptionSettings)
            {
                if (IsOptionSettingModified(recommendation, childSetting))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// <para>Returns a Dictionary containing the configurable option settings for the specified recommendation. The returned dictionary can contain specific types of option settings depending on the value of <see cref="OptionSettingsType"/>.</para>
        /// <para>The key in the dictionary is the fully qualified ID of each option setting</para>
        /// <para>The value in the dictionary is the value of each option setting</para>
        /// </summary>
        public Dictionary<string, object> GetOptionSettingsMap(Recommendation recommendation, ProjectDefinition projectDefinition, IDirectoryManager directoryManager, OptionSettingsType optionSettingsType = OptionSettingsType.All)
        {
            var projectDirectory = Path.GetDirectoryName(projectDefinition.ProjectPath);
            if (string.IsNullOrEmpty(projectDirectory))
            {
                var message = $"Failed to get deployment settings container because {projectDefinition.ProjectPath} is null or empty";
                throw new InvalidOperationException(message);
            }

            var settingsContainer = new Dictionary<string, object>();

            IEnumerable<string> optionSettingsId;
            var recipeOptionSettingsId = recommendation.GetConfigurableOptionSettingItems().Select(x => x.FullyQualifiedId);
            var deploymentBundleOptionSettingsId = recommendation.Recipe.DeploymentBundleSettings.Select(x => x.FullyQualifiedId);

            switch (optionSettingsType)
            {
                case OptionSettingsType.Recipe:
                    optionSettingsId = recipeOptionSettingsId.Except(deploymentBundleOptionSettingsId);
                    break;
                case OptionSettingsType.DeploymentBundle:
                    optionSettingsId = deploymentBundleOptionSettingsId;
                    break;
                case OptionSettingsType.All:
                    optionSettingsId = recipeOptionSettingsId.Union(deploymentBundleOptionSettingsId);
                    break;
                default:
                    throw new InvalidOperationException($"{nameof(optionSettingsType)} doest not have a valid type");
            }

            foreach (var optionSettingId in optionSettingsId)
            {
                var optionSetting = GetOptionSetting(recommendation, optionSettingId);
                var value = GetOptionSettingValue(recommendation, optionSetting);
                if (optionSetting.TypeHint.HasValue && (optionSetting.TypeHint == OptionSettingTypeHint.FilePath || optionSetting.TypeHint == OptionSettingTypeHint.DockerExecutionDirectory))
                {
                    var path = value?.ToString();
                    if (string.IsNullOrEmpty(path))
                    {
                        continue;
                    }

                    // All file paths or directory paths must be persisted relative the the customers .NET project.
                    // This is a done to ensure that the resolved paths work correctly across all cloned repos.
                    // The relative path is also canonicalized to work across Unix and Windows OS.
                    var absolutePath = directoryManager.GetAbsolutePath(projectDirectory, path);
                    value = directoryManager.GetRelativePath(projectDirectory, absolutePath)
                                .Replace(Path.DirectorySeparatorChar, '/');
                }

                if (value != null)
                {
                    settingsContainer[optionSetting.FullyQualifiedId] = value;
                }
            }

            return settingsContainer;
        }
    }
}
