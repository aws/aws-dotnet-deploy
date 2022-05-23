// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;

namespace AWS.Deploy.Common.Recipes.Validation
{
    /// <summary>
    /// The validator is used to enforce that a particular OptionSettingItem has a value before deployment.
    /// </summary>
    public class RequiredValidator : IOptionSettingItemValidator
    {
        private static readonly string defaultValidationFailedMessage = "Value can not be empty";
        public string ValidationFailedMessage { get; set; } = defaultValidationFailedMessage;

        public Task<ValidationResult> Validate(object input) =>
            Task.FromResult<ValidationResult>(new()
            {
                IsValid = !string.IsNullOrEmpty(input?.ToString()),
                ValidationFailedMessage = ValidationFailedMessage
            });
    }
}
