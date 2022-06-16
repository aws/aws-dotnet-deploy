// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Common.Extensions;

namespace AWS.Deploy.Common.Recipes.Validation
{
    /// <summary>
    /// Validates that the selected security groups are part of the selected VPC
    /// </summary>
    public class SecurityGroupsInVpcValidator : IOptionSettingItemValidator
    {
        private static readonly string defaultValidationFailedMessage = "The selected security groups are not part of the selected VPC.";
        public string VpcId { get; set; } = "";
        public string ValidationFailedMessage { get; set; } = defaultValidationFailedMessage;

        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IOptionSettingHandler _optionSettingHandler;

        public SecurityGroupsInVpcValidator(IAWSResourceQueryer awsResourceQueryer, IOptionSettingHandler optionSettingHandler)
        {
            _awsResourceQueryer = awsResourceQueryer;
            _optionSettingHandler = optionSettingHandler;
        }

        public async Task<ValidationResult> Validate(object input, Recommendation recommendation, OptionSettingItem optionSettingItem)
        {
            if (string.IsNullOrEmpty(VpcId))
                return ValidationResult.Failed($"The '{nameof(SecurityGroupsInVpcValidator)}' validator is missing the '{nameof(VpcId)}' configuration.");
            var vpcIdSetting = _optionSettingHandler.GetOptionSetting(recommendation, VpcId);
            var vpcId = _optionSettingHandler.GetOptionSettingValue<string>(recommendation, vpcIdSetting);
            if (string.IsNullOrEmpty(vpcId))
                return ValidationResult.Failed("The VpcId setting is not set or is empty. Make sure to set the VPC Id first.");

            var securityGroupIds = (await _awsResourceQueryer.DescribeSecurityGroups(vpcId)).Select(x => x.GroupId);
            if (input?.TryDeserialize<SortedSet<string>>(out var inputList) ?? false)
            {
                foreach (var securityGroup in inputList!)
                {
                    if (!securityGroupIds.Contains(securityGroup))
                        return ValidationResult.Failed("The selected security group(s) are invalid since they do not belong to the currently selected VPC.");
                }

                return ValidationResult.Valid();
            }

            return new ValidationResult
            {
                IsValid = securityGroupIds.Contains(input?.ToString()),
                ValidationFailedMessage = ValidationFailedMessage
            };
        }
    }
}
