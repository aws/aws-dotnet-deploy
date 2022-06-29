// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;
using Amazon.EC2;
using Amazon.EC2.Model;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Constants;

namespace AWS.Deploy.Common.Recipes.Validation
{
    public class WindowsInstanceTypeValidator : InstanceTypeValidator
    {
        public WindowsInstanceTypeValidator(IAWSResourceQueryer awsResourceQueryer)
            : base(awsResourceQueryer, EC2.FILTER_PLATFORM_WINDOWS)
        {
        }
    }

    public class LinuxInstanceTypeValidator : InstanceTypeValidator
    {
        public LinuxInstanceTypeValidator(IAWSResourceQueryer awsResourceQueryer)
            : base(awsResourceQueryer, EC2.FILTER_PLATFORM_LINUX)
        {
        }
    }

    /// <summary>
    /// Validates that a given EC2 instance is valid for the deployment region
    /// </summary>
    public abstract class InstanceTypeValidator : IOptionSettingItemValidator
    {
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly string _platform;

        public InstanceTypeValidator(IAWSResourceQueryer awsResourceQueryer, string platform)
        {
            _awsResourceQueryer = awsResourceQueryer;
            _platform = platform;
        }

        public async Task<ValidationResult> Validate(object input, Recommendation recommendation, OptionSettingItem optionSettingItem)
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

            if(instanceTypeInfo == null)
            {
                return ValidationResult.Failed($"The specified instance type {rawInstanceType} does not exist in the deployment region.");
            }

            if (string.Equals(_platform, EC2.FILTER_PLATFORM_WINDOWS) && !instanceTypeInfo.ProcessorInfo.SupportedArchitectures.Contains(EC2.FILTER_ARCHITECTURE_X86_64))
            {
                return ValidationResult.Failed($"The specified instance type {rawInstanceType} does not support {EC2.FILTER_ARCHITECTURE_X86_64}.");
            }

            return ValidationResult.Valid();
        }
    }
}
