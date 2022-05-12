// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Extensions;
using AWS.Deploy.Common.Recipes;

namespace AWS.Deploy.Orchestration
{
    public class OptionSettingHandler : IOptionSettingHandler
    {
        /// <summary>
        /// Assigns a value to the OptionSettingItem.
        /// </summary>
        /// <exception cref="ValidationFailedException">
        /// Thrown if one or more <see cref="Validators"/> determine
        /// <paramref name="value"/> is not valid.
        /// </exception>
        public void SetOptionSettingValue(OptionSettingItem optionSettingItem, object value)
        {
            optionSettingItem.SetValue(this, value);
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
        /// Retrieves the value of the Option Setting Item in a given recommendation.
        /// </summary>
        public T GetOptionSettingValue<T>(Recommendation recommendation, OptionSettingItem optionSetting)
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
    }
}
