// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;

namespace AWS.Deploy.Common.Recipes.Validation
{
    /// <summary>
    /// This interface outlines the framework for <see cref="OptionSettingItem"/> validators.
    /// Validators such as <see cref="RegexValidator"/> implement this interface and provide custom validation logic
    /// on OptionSettingItems
    /// </summary>
    public interface IOptionSettingItemValidator
    {
        /// <summary>
        /// Validates an override value for an <see cref="OptionSettingItem"/>
        /// </summary>
        /// <param name="input">Raw input for an option</param>
        /// <param name="recommendation">Selected recommendation, which may be used if the validator needs to consider properties other than itself</param>
        /// <returns>Whether or not the input is valid</returns>
        Task<ValidationResult> Validate(object input, Recommendation recommendation);
    }
}
