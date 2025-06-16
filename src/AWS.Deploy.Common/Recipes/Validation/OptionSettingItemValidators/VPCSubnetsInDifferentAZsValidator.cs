// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.EC2.Model;
using AWS.Deploy.Common.Data;

namespace AWS.Deploy.Common.Recipes.Validation
{
    /// <summary>
    /// Validates that the selected VPC must have at least two subnets in two different Availability Zones
    /// </summary>
    public class VPCSubnetsInDifferentAZsValidator : IOptionSettingItemValidator
    {
        private static readonly string defaultValidationFailedMessage = "Selected VPC must have at least two subnets in two different Availability Zones.";
        public string ValidationFailedMessage { get; set; } = defaultValidationFailedMessage;

        private readonly IAWSResourceQueryer _awsResourceQueryer;

        public VPCSubnetsInDifferentAZsValidator(IAWSResourceQueryer awsResourceQueryer)
        {
            _awsResourceQueryer = awsResourceQueryer;
        }

        public async Task<ValidationResult> Validate(object input, Recommendation recommendation, OptionSettingItem optionSettingItem)
        {
            var vpcId = input?.ToString();
            if (string.IsNullOrEmpty(vpcId))
                return ValidationResult.Failed("A VPC ID is not specified. Please select a valid VPC ID.");

            var subnets = await _awsResourceQueryer.DescribeSubnets(vpcId) ?? new List<Subnet>();
            var availabilityZones = new HashSet<string>();
            foreach (var subnet in subnets)
                availabilityZones.Add(subnet.AvailabilityZoneId);

            if (availabilityZones.Count >= 2)
                return ValidationResult.Valid();
            else
                return ValidationResult.Failed(ValidationFailedMessage);
        }
    }
}
