// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;

namespace AWS.Deploy.Common.Recipes.Validation
{
    /// <summary>
    /// Validator for Docker build-time variables, passed via --build-arg
    /// </summary>
    public class DockerBuildArgsValidator : IOptionSettingItemValidator
    {
        /// <summary>
        /// Validates that additional Docker build options don't collide
        /// with those set by the deploy tool
        /// </summary>
        /// <param name="input">Proposed Docker build args</param>
        /// <param name="recommendation">Selected recommendation, which may be used if the validator needs to consider properties other than itself</param>
        /// <param name="optionSettingItem">Selected option setting item, which may be used if the validator needs to consider properties other than itself</param>
        /// <returns>Valid if the options do not contain those set by the deploy tool, invalid otherwise</returns>
        public Task<ValidationResult> Validate(object input, Recommendation recommendation, OptionSettingItem optionSettingItem)
        {
            var buildArgs = Convert.ToString(input);
            var errorMessage = string.Empty;

            if (string.IsNullOrEmpty(buildArgs))
            {
                return ValidationResult.ValidAsync();
            }

            if (buildArgs.Contains("-t ") || buildArgs.Contains("--tag "))
                errorMessage += "You must not include -t/--tag as an additional argument as it is used internally. " +
                    "You may set the Image Tag property in the advanced settings for some recipes." + Environment.NewLine;

            if (buildArgs.Contains("-f ") || buildArgs.Contains("--file "))
                errorMessage += "You must not include -f/--file as an additional argument as it is used internally." + Environment.NewLine;

            if (!string.IsNullOrEmpty(errorMessage))
                return ValidationResult.FailedAsync("Invalid value for additional Docker build options." + Environment.NewLine + errorMessage.Trim());

            return ValidationResult.ValidAsync();
        }
    }
}
