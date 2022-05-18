// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;

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
        /// <returns>Valid if the options do not contain those set by the deploy tool, invalid otherwise</returns>
        public ValidationResult Validate(object input)
        {
            var buildArgs = Convert.ToString(input);
            var errorMessage = string.Empty;

            if (string.IsNullOrEmpty(buildArgs))
            {
                return ValidationResult.Valid();
            }

            if (buildArgs.Contains("-t ") || buildArgs.Contains("--tag "))
                errorMessage += "You must not include -t/--tag as an additional argument as it is used internally. " +
                    "You may set the Image Tag property in the advanced settings for some recipes." + Environment.NewLine;

            if (buildArgs.Contains("-f ") || buildArgs.Contains("--file "))
                errorMessage += "You must not include -f/--file as an additional argument as it is used internally." + Environment.NewLine;

            if (!string.IsNullOrEmpty(errorMessage))
                return ValidationResult.Failed("Invalid value for additional Docker build options." + Environment.NewLine + errorMessage.Trim());

            return ValidationResult.Valid();
        }
    }
}
