// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.Common.Recipes.Validation
{
    public class RequiredValidator : IOptionSettingItemValidator
    {
        public string ValidationFailedMessage { get; set; } = "Value can not be empty";

        public ValidationResult Validate(object input) =>
            new()
            {
                IsValid = !string.IsNullOrEmpty(input?.ToString()),
                ValidationFailedMessage = ValidationFailedMessage
            };
    }
}
