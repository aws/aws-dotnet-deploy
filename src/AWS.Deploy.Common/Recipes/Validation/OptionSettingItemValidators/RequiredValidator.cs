// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.Common.Extensions;

namespace AWS.Deploy.Common.Recipes.Validation
{
    /// <summary>
    /// The validator is used to enforce that a particular OptionSettingItem has a value before deployment.
    /// </summary>
    public class RequiredValidator : IOptionSettingItemValidator
    {
        private static readonly string defaultValidationFailedMessage = "The option setting '{{OptionSetting}}' can not be empty. Please select a valid value.";
        public string ValidationFailedMessage { get; set; } = defaultValidationFailedMessage;

        public Task<ValidationResult> Validate(object input, Recommendation recommendation, OptionSettingItem optionSettingItem)
        {
            var message = ValidationFailedMessage.Replace("{{OptionSetting}}", optionSettingItem.Name);
            if (input?.TryDeserialize<SortedSet<string>>(out var inputList) ?? false && inputList != null)
            {
                return Task.FromResult<ValidationResult>(new()
                {
                    IsValid = inputList!.Any(),
                    ValidationFailedMessage = message
                });
            }

            return Task.FromResult<ValidationResult>(new()
            {
                IsValid = !string.IsNullOrEmpty(input?.ToString()),
                ValidationFailedMessage = message
            });
        }
    }
}
