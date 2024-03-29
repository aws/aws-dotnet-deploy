// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;

namespace AWS.Deploy.Common.Recipes.Validation
{
    /// <summary>
    /// Validator for additional arguments to 'dotnet publish'
    /// </summary>
    public class DotnetPublishArgsValidator : IOptionSettingItemValidator
    {
        /// <summary>
        /// Validates that additional 'dotnet publish' arguments do not collide with those used by the deploy tool
        /// </summary>
        /// <param name="input">Additional publish arguments</param>
        /// <param name="recommendation">Selected recommendation, which may be used if the validator needs to consider properties other than itself</param>
        /// <param name="optionSettingItem">Selected option setting item, which may be used if the validator needs to consider properties other than itself</param>
        /// <returns>Valid if the arguments don't interfere with the deploy tool, invalid otherwise</returns>
        public Task<ValidationResult> Validate(object input, Recommendation recommendation, OptionSettingItem optionSettingItem)
        {
            var publishArgs = Convert.ToString(input);
            var errorMessage = string.Empty;

            if (string.IsNullOrEmpty(publishArgs))
            {
                return ValidationResult.ValidAsync();
            }

            if (publishArgs.Contains("-o ") || publishArgs.Contains("--output "))
                errorMessage += "You must not include -o/--output as an additional argument as it is used internally." + Environment.NewLine;

            if (publishArgs.Contains("-c ") || publishArgs.Contains("--configuration "))
                errorMessage += "You must not include -c/--configuration as an additional argument. You can set the build configuration in the advanced settings." + Environment.NewLine;

            if (publishArgs.Contains("--self-contained") || publishArgs.Contains("--no-self-contained"))
                errorMessage += "You must not include --self-contained/--no-self-contained as an additional argument. You can set the self-contained property in the advanced settings." + Environment.NewLine;

            if (!string.IsNullOrEmpty(errorMessage))
                return ValidationResult.FailedAsync("Invalid value for Dotnet Publish Arguments." + Environment.NewLine + errorMessage.Trim());

            return ValidationResult.ValidAsync();
        }
    }
}
