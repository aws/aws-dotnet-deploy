// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.Deploy.Common.Data;

namespace AWS.Deploy.Common.Recipes.Validation;

public class BeanstalkInstanceTypeValidator : IRecipeValidator
{
    private readonly IOptionSettingHandler _optionSettingHandler;
    private readonly IAWSResourceQueryer _awsResourceQueryer;

    public BeanstalkInstanceTypeValidator(IAWSResourceQueryer awsResourceQueryer, IOptionSettingHandler optionSettingHandler)
    {
        _awsResourceQueryer = awsResourceQueryer;
        _optionSettingHandler = optionSettingHandler;
    }

    public string InstanceTypeOptionSettingsId { get; set; } = "InstanceType";
    public string ApplicationNameOptionSettingsId { get; set; } = "BeanstalkApplication.ApplicationName";
    public string EnvironmentNameOptionSettingsId { get; set; } = "BeanstalkEnvironment.EnvironmentName";

    public async Task<ValidationResult> Validate(Recommendation recommendation, IDeployToolValidationContext deployValidationContext)
    {
        string? instanceType;

        try
        {
            instanceType = _optionSettingHandler.GetOptionSettingValue<string>(recommendation, _optionSettingHandler.GetOptionSetting(recommendation, InstanceTypeOptionSettingsId));
        }
        catch (OptionSettingItemDoesNotExistException)
        {
            return await ValidationResult.FailedAsync("Could not find a valid value for Instance Type " +
                "as part of of the Elastic Beanstalk deployment configuration. Please provide a valid value and try again.");
        }

        if (!recommendation.IsExistingCloudApplication)
            return await ValidationResult.ValidAsync();


        if (string.IsNullOrEmpty(instanceType))
        {
            string? applicationName;
            string? environmentName;

            try
            {
                applicationName = _optionSettingHandler.GetOptionSettingValue<string>(recommendation, _optionSettingHandler.GetOptionSetting(recommendation, ApplicationNameOptionSettingsId));
                environmentName = _optionSettingHandler.GetOptionSettingValue<string>(recommendation, _optionSettingHandler.GetOptionSetting(recommendation, EnvironmentNameOptionSettingsId));
            }
            catch (OptionSettingItemDoesNotExistException)
            {
                return await ValidationResult.FailedAsync("Could not find a valid value for Environment Name " +
                    "as part of of the Elastic Beanstalk deployment configuration. Please provide a valid value and try again.");
            }

            if (string.IsNullOrEmpty(applicationName))
                return await ValidationResult.FailedAsync("Could not find a valid value for Application Name " +
                "as part of of the Elastic Beanstalk deployment configuration. Please provide a valid value and try again.");
            if (string.IsNullOrEmpty(environmentName))
                return await ValidationResult.FailedAsync("Could not find a valid value for Environment Name " +
                "as part of of the Elastic Beanstalk deployment configuration. Please provide a valid value and try again.");

            var environmentSettings = await _awsResourceQueryer.DescribeElasticBeanstalkConfigurationSettings(applicationName, environmentName);
            var environmentInstanceTypes = new HashSet<string>();
            foreach (var environmentSetting in environmentSettings)
            {
                foreach (var optionSetting in environmentSetting.OptionSettings)
                {
                    if (optionSetting.Namespace.Equals("aws:autoscaling:launchconfiguration") &&
                        optionSetting.OptionName.Equals("InstanceType"))
                    {
                        environmentInstanceTypes.Add(optionSetting.Value);
                    }
                }
            }

            var environmentArchitectures = new HashSet<string>();
            foreach (var environmentInstanceType in environmentInstanceTypes)
            {
                var describeInstanceTypeResponse = await _awsResourceQueryer.DescribeInstanceType(environmentInstanceType);
                describeInstanceTypeResponse?.ProcessorInfo.SupportedArchitectures.ForEach((x) => environmentArchitectures.Add(x));
            }

            if (!environmentArchitectures.Contains(recommendation.DeploymentBundle.EnvironmentArchitecture.ToString(), StringComparer.InvariantCultureIgnoreCase))
            {
                return await ValidationResult.FailedAsync(
                    $"The Elastic Beanstalk application is currently using the Instance Types '{string.Join(",", environmentInstanceTypes)}' " +
                    $"which do not support the currently selected Environment Architecture '{recommendation.DeploymentBundle.EnvironmentArchitecture}'. " +
                    "Please select an Instance Type that supports the currently selected Environment Architecture.");
            }
        }
        else
        {
            var describeInstanceTypeResponse = await _awsResourceQueryer.DescribeInstanceType(instanceType);
            var environmentArchitectures = describeInstanceTypeResponse?.ProcessorInfo.SupportedArchitectures ?? new List<string>();
            if (!environmentArchitectures.Contains(recommendation.DeploymentBundle.EnvironmentArchitecture.ToString(), StringComparer.InvariantCultureIgnoreCase))
            {
                return await ValidationResult.FailedAsync(
                    $"The Elastic Beanstalk application is currently using the Instance Type '{string.Join(",", instanceType)}' " +
                    $"which do not support the currently selected Environment Architecture '{recommendation.DeploymentBundle.EnvironmentArchitecture}'. " +
                    "Please select an Instance Type that supports the currently selected Environment Architecture.");
            }
        }

        return await ValidationResult.ValidAsync();
    }
}
