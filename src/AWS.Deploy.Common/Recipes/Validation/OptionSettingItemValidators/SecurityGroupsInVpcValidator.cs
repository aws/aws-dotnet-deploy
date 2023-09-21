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

        /// <summary>
        /// Path to the OptionSetting that stores a selected Vpc Id
        /// </summary>
        public string VpcId { get; set; } = "";

        /// <summary>
        /// Path to the OptionSetting that determines if the default VPC should be used
        /// </summary>
        public string IsDefaultVpcOptionSettingId { get; set; } = "";

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

            var vpcId = "";

            // The ECS Fargate recipes expose a separate radio button to select the default VPC which is mutually exclusive
            // with specifying an explicit VPC Id. Because we give preference to "UseDefault" in the CDK project,
            // we should do so here as well and validate the security groups against the default VPC if it's selected.
            if (!string.IsNullOrEmpty(IsDefaultVpcOptionSettingId))
            {
                var isDefaultVpcOptionSetting = _optionSettingHandler.GetOptionSetting(recommendation, IsDefaultVpcOptionSettingId);
                var shouldUseDefaultVpc = _optionSettingHandler.GetOptionSettingValue<bool>(recommendation, isDefaultVpcOptionSetting);

                if (shouldUseDefaultVpc)
                {
                    vpcId = (await _awsResourceQueryer.GetDefaultVpc())?.VpcId;
                }
            }

            // If the "Use default?" option doesn't exist in the recipe, or it does and was false, or
            // we failed to look up the default VPC, then use the explicity VPC Id
            if (string.IsNullOrEmpty(vpcId))
            {
                var vpcIdSetting = _optionSettingHandler.GetOptionSetting(recommendation, VpcId);
                vpcId = _optionSettingHandler.GetOptionSettingValue<string>(recommendation, vpcIdSetting);
            }

            if (string.IsNullOrEmpty(vpcId))
                return ValidationResult.Failed("The VpcId setting is not set or is empty. Make sure to set the VPC Id first.");

            var securityGroupIds = (await _awsResourceQueryer.DescribeSecurityGroups(vpcId)).Select(x => x.GroupId);

            // The ASP.NET Fargate recipe uses a list of security groups
            if (input?.TryDeserialize<SortedSet<string>>(out var inputList) ?? false)
            {
                var invalidSecurityGroups = new List<string>();
                foreach (var securityGroup in inputList!)
                {
                    if (!securityGroupIds.Contains(securityGroup))
                        invalidSecurityGroups.Add(securityGroup);
                }

                if (invalidSecurityGroups.Any())
                {
                    return ValidationResult.Failed($"The selected security group(s) ({string.Join(", ", invalidSecurityGroups)}) " +
                        $"are invalid since they do not belong to the currently selected VPC {vpcId}.");
                }

                return ValidationResult.Valid();
            }

            // The Console ECS Fargate Service recipe uses a comma-separated string, which will fall through the TryDeserialize above
            if (input is string)
            {
                // Security groups aren't required
                if (string.IsNullOrEmpty(input.ToString()))
                {
                    return ValidationResult.Valid();
                }

                var securityGroupList = input.ToString()?.Split(',') ?? new string[0];
                var invalidSecurityGroups = new List<string>();

                foreach (var securityGroup in securityGroupList)
                {
                    if (!securityGroupIds.Contains(securityGroup))
                        invalidSecurityGroups.Add(securityGroup);
                }

                if (invalidSecurityGroups.Any())
                {
                    return ValidationResult.Failed($"The selected security group(s) ({string.Join(", ", invalidSecurityGroups)}) " +
                        $"are invalid since they do not belong to the currently selected VPC {vpcId}.");
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
