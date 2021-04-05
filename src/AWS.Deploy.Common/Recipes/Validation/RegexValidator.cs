// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.Text.RegularExpressions;

namespace AWS.Deploy.Common.Recipes.Validation
{
    public class RegexValidator : IOptionSettingItemValidator
    {
        public string Regex { get; set; } = "(.*)";
        public string ValidationFailedMessage { get; set; } = "Value must match Regex {{Regex}}";

        public ValidationResult Validate(object input)
        {
            var regex = new Regex(Regex);

            var message = ValidationFailedMessage.Replace("{{Regex}}", Regex);

            return new ValidationResult
            {
                IsValid = regex.IsMatch(input?.ToString() ?? ""),
                ValidationFailedMessage = message
            };
        }
    }
}
