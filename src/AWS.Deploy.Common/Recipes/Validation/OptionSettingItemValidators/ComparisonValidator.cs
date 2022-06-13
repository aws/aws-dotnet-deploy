// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;

namespace AWS.Deploy.Common.Recipes.Validation
{
    public enum ComparisonValidatorOperation
    {
        GreaterThan
    }

    /// <summary>
    /// The validator is typically used with OptionSettingItems which have a numeric type.
    /// The validator requires two configuration options to be specific, the Operation and the SettingId.
    /// The validator checks if the set value of the OptionSettingItem satisfies the comparison operation with SettingId.
    /// </summary>
    public class ComparisonValidator : IOptionSettingItemValidator
    {
        public ComparisonValidatorOperation? Operation { get; set; }
        public string? SettingId { get;set; }

        private readonly IOptionSettingHandler _optionSettingHandler;

        public ComparisonValidator(IOptionSettingHandler optionSettingHandler)
        {
            _optionSettingHandler = optionSettingHandler;
        }

        public Task<ValidationResult> Validate(object input, Recommendation recommendation, OptionSettingItem optionSettingItem)
        {
            if (Operation == null)
                throw new MissingValidatorConfigurationException(DeployToolErrorCode.MissingValidatorConfiguration, $"The validator of type '{typeof(ComparisonValidator)}' is missing the configuration property '{nameof(Operation)}'.");
            if (string.IsNullOrEmpty(SettingId))
                throw new MissingValidatorConfigurationException(DeployToolErrorCode.MissingValidatorConfiguration, $"The validator of type '{typeof(ComparisonValidator)}' is missing the configuration property '{nameof(SettingId)}'.");
            if (!double.TryParse(input?.ToString(), out double inputDouble))
                return ValidationResult.FailedAsync($"The value of '{optionSettingItem.Name}' is not a numeric value.");
            var comparisonSetting = _optionSettingHandler.GetOptionSetting(recommendation, SettingId);
            var comparisonSettingValue = _optionSettingHandler.GetOptionSettingValue(recommendation, comparisonSetting);
            if (!double.TryParse(comparisonSettingValue?.ToString(), out double comparisonSettingValueDouble))
                return ValidationResult.FailedAsync($"The value of '{comparisonSetting.Name}' is not a numeric value.");

            if (Operation == ComparisonValidatorOperation.GreaterThan)
            {
                if (inputDouble > comparisonSettingValueDouble)
                    return ValidationResult.ValidAsync();
                else
                    return ValidationResult.FailedAsync($"The value of '{optionSettingItem.Name}' must be greater than the value of '{comparisonSetting.Name}'.");
            }
            else
            {
                return ValidationResult.FailedAsync($"The operation '{Operation}' is not yet supported.");
            }
        }
    }
}
