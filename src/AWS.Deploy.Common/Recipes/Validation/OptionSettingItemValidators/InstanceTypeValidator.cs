// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;
using Amazon.EC2;
using Amazon.EC2.Model;
using AWS.Deploy.Common.Data;

namespace AWS.Deploy.Common.Recipes.Validation
{
    /// <summary>
    /// Validates that a given EC2 instance is valid for the deployment region
    /// </summary>
    public class InstanceTypeValidator : IOptionSettingItemValidator
    {
        private readonly IAWSResourceQueryer _awsResourceQueryer;

        public InstanceTypeValidator(IAWSResourceQueryer awsResourceQueryer)
        {
            _awsResourceQueryer = awsResourceQueryer;
        }

        public async Task<ValidationResult> Validate(object input, Recommendation recommendation)
        {
            var rawInstanceType = Convert.ToString(input);
            InstanceTypeInfo? instanceTypeInfo;

            if (string.IsNullOrEmpty(rawInstanceType))
            {
                return ValidationResult.Valid();
            }

            try
            {
                instanceTypeInfo = await _awsResourceQueryer.DescribeInstanceType(rawInstanceType);
            }
            catch (ResourceQueryException ex)
            {
                // Check for the expected exception if the provided instance type is invalid
                if (ex.InnerException is AmazonEC2Exception ec2Exception &&
                    string.Equals(ec2Exception.ErrorCode, "InvalidInstanceType", StringComparison.InvariantCultureIgnoreCase))
                {
                    instanceTypeInfo = null;
                }
                else // Anything else is unexpected, so proceed with usual exception handling
                {
                   throw ex;
                }
            }
            if (instanceTypeInfo != null)
            {
                return ValidationResult.Valid();
            }
            else
            {
                return ValidationResult.Failed($"The specified instance type {rawInstanceType} does not exist in the deployment region.");
            }
        }
    }
}
