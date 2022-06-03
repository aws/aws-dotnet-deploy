// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AWS.Deploy.Common.Extensions;

namespace AWS.Deploy.Common.Recipes.Validation
{
    /// <summary>
    /// The validator is typically used with OptionSettingItems which have a string type.
    /// The regex string is specified in the deployment recipe
    /// and this validator checks if the set value of the OptionSettingItem matches the regex or not.
    /// </summary>
    public class RegexValidator : IOptionSettingItemValidator
    {
        private static readonly string defaultRegex = "(.*)";
        private static readonly string defaultValidationFailedMessage = "Value must match Regex {{Regex}}";

        public string Regex { get; set; } = defaultRegex;
        public string ValidationFailedMessage { get; set; } = defaultValidationFailedMessage;
        public bool AllowEmptyString { get; set; }

        public Task<ValidationResult> Validate(object input, Recommendation recommendation, OptionSettingItem optionSettingItem)
        {
            var regex = new Regex(Regex);

            var message = ValidationFailedMessage.Replace("{{Regex}}", Regex);


            if (input?.TryDeserialize<SortedSet<string>>(out var inputList) ?? false)
            {
                foreach (var item in inputList!)
                {
                    var valid = regex.IsMatch(item) || (AllowEmptyString && string.IsNullOrEmpty(item));
                    if (!valid)
                        return Task.FromResult<ValidationResult>(new ValidationResult
                        {
                            IsValid = false,
                            ValidationFailedMessage = message
                        });
                }
                return Task.FromResult<ValidationResult>(new ValidationResult
                {
                    IsValid = true,
                    ValidationFailedMessage = message
                });
            }

            return Task.FromResult<ValidationResult>(new ValidationResult
            {
                IsValid = regex.IsMatch(input?.ToString() ?? "") || (AllowEmptyString && string.IsNullOrEmpty(input?.ToString())),
                ValidationFailedMessage = message
            });
        }
    }
}
