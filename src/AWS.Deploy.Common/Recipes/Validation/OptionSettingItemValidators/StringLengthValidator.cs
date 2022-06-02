// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AWS.Deploy.Common.Recipes.Validation
{
    /// <summary>
    /// Validates that an OptionSettingItem of type string satisifes the required length constraints
    /// </summary>
    public class StringLengthValidator : IOptionSettingItemValidator
    {
        public int MinLength { get; set; } = 0;
        public int MaxLength { get; set; } = 1000;
        public string ValidationFailedMessage { get; set; } = "Invalid value. Number of characters must be between {{min}} and {{max}}";

        public Task<ValidationResult> Validate(object input, Recommendation recommendation)
        {
            var inputString = input?.ToString() ?? string.Empty;
            var stringLength = inputString.Length;

            if (stringLength < MinLength || stringLength > MaxLength)
            {
                var message = ValidationFailedMessage
                                    .Replace("{{min}}", MinLength.ToString())
                                    .Replace("{{max}}", MaxLength.ToString());
                
                return ValidationResult.FailedAsync(message);
            }

            return ValidationResult.ValidAsync();
        }
    }
}
