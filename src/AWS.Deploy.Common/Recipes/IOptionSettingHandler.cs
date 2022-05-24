// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Threading.Tasks;
using AWS.Deploy.Common.Recipes.Validation;

namespace AWS.Deploy.Common.Recipes
{
    /// <summary>
    /// This interface defines a contract used to control access to getting an setting values for <see cref="OptionSettingItem"/>
    /// </summary>
    public interface IOptionSettingHandler
    {
        /// <summary>
        /// This method runs all the option setting validators for the configurable settings.
        /// In case of a first time deployment, all settings and validators are run.
        /// In case of a redeployment, only the updatable settings are considered.
        /// </summary>
        List<ValidationResult> RunOptionSettingValidators(Recommendation recommendation, IEnumerable<OptionSettingItem>? optionSettings = null);

        /// <summary>
        /// This method is used to set values for <see cref="OptionSettingItem"/>.
        /// Due to different validations that could be put in place, access to other services may be needed.
        /// This method is meant to control access to those services and determine the value to be set.
        /// </summary>
        Task SetOptionSettingValue(Recommendation recommendation, OptionSettingItem optionSettingItem, object value, bool skipValidation = false);

        /// <summary>
        /// This method retrieves the <see cref="OptionSettingItem"/> related to a specific <see cref="Recommendation"/>.
        /// </summary>
        OptionSettingItem GetOptionSetting(Recommendation recommendation, string? jsonPath);

        /// <summary>
        /// This method retrieves the <see cref="OptionSettingItem"/> related to a specific <see cref="Recipe"/>.
        /// </summary>
        OptionSettingItem GetOptionSetting(RecipeDefinition recipe, string? jsonPath);

        /// <summary>
        /// Retrieve the <see cref="OptionSettingItem"/> value for a specific <see cref="Recommendation"/>
        /// This method retrieves the value in a specified type.
        /// </summary>
        T GetOptionSettingValue<T>(Recommendation recommendation, OptionSettingItem optionSetting);

        /// <summary>
        /// Retrieve the <see cref="OptionSettingItem"/> value for a specific <see cref="Recommendation"/>
        /// This method retrieves the value as an object type.
        /// </summary>
        object GetOptionSettingValue(Recommendation recommendation, OptionSettingItem optionSetting);

        /// <summary>
        /// Retrieve the <see cref="OptionSettingItem"/> default value for a specific <see cref="Recommendation"/>
        /// This method retrieves the default value in a specified type.
        /// </summary>
        T? GetOptionSettingDefaultValue<T>(Recommendation recommendation, OptionSettingItem optionSetting);

        /// <summary>
        /// Retrieve the <see cref="OptionSettingItem"/> default value for a specific <see cref="Recommendation"/>
        /// This method retrieves the default value as an object type.
        /// </summary>
        object? GetOptionSettingDefaultValue(Recommendation recommendation, OptionSettingItem optionSetting);

        /// <summary>
        /// Checks whether all the dependencies are satisfied or not, if there exists an unsatisfied dependency then returns false.
        /// It allows caller to decide whether we want to display an <see cref="OptionSettingItem"/> to configure or not.
        /// </summary>
        /// <returns>Returns true, if all the dependencies are satisfied, else false.</returns>
        bool IsOptionSettingDisplayable(Recommendation recommendation, OptionSettingItem optionSetting);

        /// <summary>
        /// Checks whether the Option Setting Item can be displayed as part of the settings summary of the previous deployment.
        /// </summary>
        bool IsSummaryDisplayable(Recommendation recommendation, OptionSettingItem optionSettingItem);
    }

}
