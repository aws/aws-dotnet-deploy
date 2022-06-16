// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AWS.Deploy.Common.Recipes.Validation
{
    /// <summary>
    /// Checks if a URI is structurally valid
    /// </summary>
    public class UriValidator : IOptionSettingItemValidator
    {
        public string ValidationFailedMessage { get; set; } = "{{URI}} is not a valid URI.";

        public Task<ValidationResult> Validate(object input, Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var uri = input?.ToString() ?? string.Empty;

            /// It is possible that a URI specific option setting item is optional and can be null or empty.
            /// To enforce the presence of a non-null or non-empty value, you must combine this validator with a <see cref="RequiredValidator"/>
            if (string.IsNullOrEmpty(uri))
            {
                return ValidationResult.ValidAsync();
            }

            var message = ValidationFailedMessage.Replace("{{URI}}", uri);
            try
            {
                var uriResult = new Uri(uri);
                if (!Uri.IsWellFormedUriString(uri, UriKind.Absolute))
                    return ValidationResult.FailedAsync(message);
            }
            catch (UriFormatException)
            {
                return ValidationResult.FailedAsync(message);
            }

            return ValidationResult.ValidAsync();
        }
    }
}
