// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.Common.Data;

namespace AWS.Deploy.Common.Recipes.Validation
{
    /// <summary>
    /// Validates that a VPC exists in the account
    /// </summary>
    public class VpcExistsValidator : IOptionSettingItemValidator
    {
        private static readonly string defaultValidationFailedMessage = "A VPC could not be found.";
        public string ValidationFailedMessage { get; set; } = defaultValidationFailedMessage;

        /// <summary>
        /// The value of the option setting that will cause the validator to fail if a VPC is not found.
        /// </summary>
        public object FailValue { get; set; } = true;

        /// <summary>
        /// The value type of the option setting and <see cref="FailValue"/>.
        /// </summary>
        public OptionSettingValueType ValueType { get; set; } = OptionSettingValueType.Bool;

        /// <summary>
        /// Indicates whether this validator will only check for the existence of the default VPC.
        /// </summary>
        public bool DefaultVpc { get; set; } = false;

        private readonly IAWSResourceQueryer _awsResourceQueryer;

        public VpcExistsValidator(IAWSResourceQueryer awsResourceQueryer)
        {
            _awsResourceQueryer = awsResourceQueryer;
        }

        public async Task<ValidationResult> Validate(object input, Recommendation recommendation, OptionSettingItem optionSettingItem)
        {
            // Check for the existence of VPCs which will cause the Validator to pass if VPCs exist
            if (DefaultVpc)
            {
                var vpc = await _awsResourceQueryer.GetDefaultVpc();
                if (vpc != null)
                    return ValidationResult.Valid();
            }
            else
            {
                var vpcs = await _awsResourceQueryer.GetListOfVpcs();
                if (vpcs.Any())
                    return ValidationResult.Valid();
            }

            // If VPCs don't exist, based on the type, check if the option setting value is equal to the FailValue
            var inputString = input?.ToString() ?? string.Empty;
            if (ValueType == OptionSettingValueType.Bool)
            {
                if (bool.TryParse(inputString, out var inputBool) && FailValue is bool FailValueBool)
                {
                    if (inputBool == FailValueBool)
                        return ValidationResult.Failed(ValidationFailedMessage);
                    else
                        return ValidationResult.Valid();
                }
                else
                {
                    return ValidationResult.Failed($"The option setting value or '{nameof(FailValue)}' are not of type '{ValueType}'.");
                }
            }
            else
            {
                return ValidationResult.Failed($"The value '{ValueType}' for '{nameof(ValueType)}' is not supported.");
            }
        }
    }
}
