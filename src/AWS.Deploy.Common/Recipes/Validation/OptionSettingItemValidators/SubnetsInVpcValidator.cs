// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Common.Extensions;

namespace AWS.Deploy.Common.Recipes.Validation
{
    /// <summary>
    /// Validates that the selected subnets are part of the selected VPC
    /// </summary>
    public class SubnetsInVpcValidator : IOptionSettingItemValidator
    {
        private static readonly string defaultValidationFailedMessage = "The selected subnets are not part of the selected VPC.";

        /// <summary>
        /// Json path to an option setting that stores the selected VPC Id
        /// </summary>
        public string VpcId { get; set; } = "";

        /// <summary>
        /// Overrideable message to display when a single specified subnet is not valid.
        /// A computed message is used for multiple subnets.
        /// </summary>
        public string ValidationFailedMessage { get; set; } = defaultValidationFailedMessage;

        /// <summary>
        /// Json path to an option setting that stores whether the default VPC should be used
        /// </summary>
        public string DefaultVpcOptionPath { get; set; } = "";

        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IOptionSettingHandler _optionSettingHandler;

        public SubnetsInVpcValidator(IAWSResourceQueryer awsResourceQueryer, IOptionSettingHandler optionSettingHandler)
        {
            _awsResourceQueryer = awsResourceQueryer;
            _optionSettingHandler = optionSettingHandler;
        }

        public async Task<ValidationResult> Validate(object input, Recommendation recommendation, OptionSettingItem optionSettingItem)
        {
            if (string.IsNullOrEmpty(VpcId))
                return ValidationResult.Failed($"The '{nameof(SubnetsInVpcValidator)}' validator is missing the '{nameof(VpcId)}' configuration.");
            var vpcIdSetting = _optionSettingHandler.GetOptionSetting(recommendation, VpcId);
            var vpcId = _optionSettingHandler.GetOptionSettingValue<string>(recommendation, vpcIdSetting);
            if (string.IsNullOrEmpty(vpcId))
                return ValidationResult.Failed("The VpcId setting is not set or is empty. Make sure to set the VPC Id first.");

            if (!string.IsNullOrEmpty(DefaultVpcOptionPath))
            {
                var useDefaultVpcSetting = _optionSettingHandler.GetOptionSetting(recommendation, DefaultVpcOptionPath);
                var useDefaultVpcValue = _optionSettingHandler.GetOptionSettingValue<bool>(recommendation, useDefaultVpcSetting);

                // For at least the ECS Fargate recipes, it's possible for the user to:
                //   1. Select a non-default, existing VPC
                //   2. Then set "Default VPC?" to true
                // Because we have the default VPC selection take precedence in CDK projects over an explicit VPC Id,
                // we want to validate the subnets against the default VPC in this case.
                if (useDefaultVpcValue == true)
                {
                    var defaultVPC = await _awsResourceQueryer.GetDefaultVpc();

                    if (defaultVPC != null)
                    {
                        vpcId = defaultVPC.VpcId;
                    }
                }
            }

            var subnetIds = (await _awsResourceQueryer.DescribeSubnets(vpcId)).Select(x => x.SubnetId);
            if (input?.TryDeserialize<SortedSet<string>>(out var inputList) ?? false)
            {
                var invalidSubnets = new List<string>();
                foreach (var subnet in inputList!)
                {
                    if (!subnetIds.Contains(subnet))
                        invalidSubnets.Add(subnet);
                }

                if (invalidSubnets.Any())
                {
                    return ValidationResult.Failed($"The selected subnet(s) ({string.Join(", ", invalidSubnets)}) are invalid since they do not belong to the currently selected VPC {vpcId}.");

                }
                return ValidationResult.Valid();
            }

            // Subnets were added to the ECS Fargate recipes after GA, so could not be marked as required.
            // It is valid for a user to not specify subnets and then fallback on CDK's defaulting logic.
            if (string.IsNullOrEmpty(input?.ToString()))
            {
                return ValidationResult.Valid();
            }

            return new ValidationResult
            {
                IsValid = subnetIds.Contains(input?.ToString()),
                ValidationFailedMessage = ValidationFailedMessage
            };
        }
    }
}
