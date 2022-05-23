// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.Common.Recipes.Validation
{
    /// <summary>
    /// This validator enforces a constraint that the value for one option setting item is less than another option setting item.
    /// The setting that holds the minimum value is identified by the 'MinValueOptionSettingsId'.
    /// The setting that holds the maximum value is identified by the 'MaxValueOptionSettingsId'.
    /// </summary>
    public class MinMaxConstraintValidator : IRecipeValidator
    {
        private readonly IOptionSettingHandler _optionSettingHandler;

        public MinMaxConstraintValidator(IOptionSettingHandler optionSettingHandler)
        {
            _optionSettingHandler = optionSettingHandler;
        }

        public string MinValueOptionSettingsId { get; set; } = string.Empty;
        public string MaxValueOptionSettingsId { get; set; } = string.Empty;
        public string ValidationFailedMessage { get; set; } = "The value specified for {{MinValueOptionSettingsId}} must be less than or equal to the value specified for {{MaxValueOptionSettingsId}}";

        public ValidationResult Validate(Recommendation recommendation, IDeployToolValidationContext deployValidationContext)
        {
            double minVal;
            double maxValue;

            try
            {
                minVal = _optionSettingHandler.GetOptionSettingValue<double>(recommendation, _optionSettingHandler.GetOptionSetting(recommendation, MinValueOptionSettingsId));
                maxValue = _optionSettingHandler.GetOptionSettingValue<double>(recommendation, _optionSettingHandler.GetOptionSetting(recommendation, MaxValueOptionSettingsId));
            }
            catch (OptionSettingItemDoesNotExistException)
            {
                return ValidationResult.Failed($"Could not find a valid value for {MinValueOptionSettingsId} or {MaxValueOptionSettingsId}. Please provide a valid value and try again.");
            }

            if (minVal <= maxValue)
                return ValidationResult.Valid();

            var failureMessage =
                ValidationFailedMessage
                    .Replace("{{MinValueOptionSettingsId}}", MinValueOptionSettingsId)
                    .Replace("{{MaxValueOptionSettingsId}}", MaxValueOptionSettingsId);

            return ValidationResult.Failed(failureMessage);
        }
    }
}
