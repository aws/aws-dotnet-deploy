// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.EC2;
using Amazon.EC2.Model;
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
        public string VpcId { get; set; } = "";
        public string ValidationFailedMessage { get; set; } = defaultValidationFailedMessage;

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

            var subnetIds = (await _awsResourceQueryer.DescribeSubnets(vpcId)).Select(x => x.SubnetId);
            if (input?.TryDeserialize<SortedSet<string>>(out var inputList) ?? false)
            {
                foreach (var subnet in inputList!)
                {
                    if (!subnetIds.Contains(subnet))
                        return ValidationResult.Failed("The selected subnet(s) are invalid since they do not belong to the currently selected VPC.");
                }

                return ValidationResult.Valid();
            }

            // Subnets were added to the ECS Fargate recipes after GA, so could not be marked required.
            // It is valid for a user to not specify subnets, and then fallback on CDK's defaulting logic.
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
