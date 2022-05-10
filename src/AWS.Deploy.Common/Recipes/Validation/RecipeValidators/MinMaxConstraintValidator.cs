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
        public string MinValueOptionSettingsId { get; set; } = string.Empty;
        public string MaxValueOptionSettingsId { get; set; } = string.Empty;
        public string ValidationFailedMessage { get; set; } = "The value specified for {{MinValueOptionSettingsId}} must be less than or equal to the value specified for {{MaxValueOptionSettingsId}}";

        public ValidationResult Validate(Recommendation recommendation, IDeployToolValidationContext deployValidationContext, IOptionSettingHandler optionSettingHandler)
        {
            double minVal;
            double maxValue;

            try
            {
                minVal = optionSettingHandler.GetOptionSettingValue<double>(recommendation, optionSettingHandler.GetOptionSetting(recommendation, MinValueOptionSettingsId));
                maxValue = optionSettingHandler.GetOptionSettingValue<double>(recommendation, optionSettingHandler.GetOptionSetting(recommendation, MaxValueOptionSettingsId));
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
