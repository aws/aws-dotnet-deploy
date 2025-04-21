// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.Deploy.Common.Data;

namespace AWS.Deploy.Common.Recipes.Validation;

/// <summary>
/// Validates that the chosen Elastic Beanstalk instance type(s) support
/// the target environment architecture in a deployment recommendation.
/// </summary>
public class BeanstalkInstanceTypeValidator : IRecipeValidator
{
    private readonly IOptionSettingHandler _optionSettingHandler;
    private readonly IAWSResourceQueryer _awsResourceQueryer;

    /// <summary>
    /// Initializes a new instance of the <see cref="BeanstalkInstanceTypeValidator"/> class.
    /// </summary>
    public BeanstalkInstanceTypeValidator(IAWSResourceQueryer awsResourceQueryer, IOptionSettingHandler optionSettingHandler)
    {
        _awsResourceQueryer = awsResourceQueryer;
        _optionSettingHandler = optionSettingHandler;
    }

    public string InstanceTypeOptionSettingsId { get; set; } = "InstanceType";
    public string ApplicationNameOptionSettingsId { get; set; } = "BeanstalkApplication.ApplicationName";
    public string EnvironmentNameOptionSettingsId { get; set; } = "BeanstalkEnvironment.EnvironmentName";

    /// <summary>
    /// Validates that the instance type(s) configured for an Elastic Beanstalk
    /// deployment recommendation support the deployment bundle’s environment architecture.
    /// </summary>
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

        // If the instance type is null and this is a new deployment, CDK & Beanstalk will automatically set the appropriate instance type
        // based on the defined environment architecture. However, on a redeployment, if the user changes the environment architecture
        // but does not explicitly update the instance type, then CDK & Beanstalk do not automatically update the instance type
        // which would cause a failed deployment. In this case, we need to let the user know that the instance type needs to be updated.
        if (string.IsNullOrEmpty(instanceType))
        {
            // If this is a new deployment, CDK & Beanstalk will set up the proper instance type and we do not need to perform validation
            if (!recommendation.IsExistingCloudApplication)
                return await ValidationResult.ValidAsync();

            string? applicationName;
            string? environmentName;

            try
            {
                applicationName = _optionSettingHandler.GetOptionSettingValue<string>(recommendation, _optionSettingHandler.GetOptionSetting(recommendation, ApplicationNameOptionSettingsId));
                if (string.IsNullOrEmpty(applicationName))
                    return await ValidationResult.FailedAsync("Could not find a valid value for Application Name " +
                    "as part of of the Elastic Beanstalk deployment configuration. Please provide a valid value and try again.");
                environmentName = _optionSettingHandler.GetOptionSettingValue<string>(recommendation, _optionSettingHandler.GetOptionSetting(recommendation, EnvironmentNameOptionSettingsId));
                if (string.IsNullOrEmpty(environmentName))
                    return await ValidationResult.FailedAsync("Could not find a valid value for Environment Name " +
                    "as part of of the Elastic Beanstalk deployment configuration. Please provide a valid value and try again.");
            }
            catch (OptionSettingItemDoesNotExistException)
            {
                return await ValidationResult.FailedAsync("Could not find a valid value for Environment Name " +
                    "as part of of the Elastic Beanstalk deployment configuration. Please provide a valid value and try again.");
            }

            // In order to retrieve the Instance Type from Elastic Beanstalk, we need to list the Option Settings of the configuration settings and look for the option
            // with the namespace 'aws:autoscaling:launchconfiguration' and name 'InstanceType'.
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

            // Once we have the instance types, we need to retrieve the supported architecture for those instance types.
            var environmentArchitectures = new HashSet<string>();
            foreach (var environmentInstanceType in environmentInstanceTypes)
            {
                var describeInstanceTypeResponse = await _awsResourceQueryer.DescribeInstanceType(environmentInstanceType);
                describeInstanceTypeResponse?.ProcessorInfo.SupportedArchitectures.ForEach((x) => environmentArchitectures.Add(x));
            }

            // We check if the selected instance types support the architecture we are trying to deploy to.
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
            // We need to retrieve the supported architecture for the selected instance type.
            var describeInstanceTypeResponse = await _awsResourceQueryer.DescribeInstanceType(instanceType);
            var environmentArchitectures = describeInstanceTypeResponse?.ProcessorInfo.SupportedArchitectures ?? new List<string>();
            // We check if the selected instance types support the architecture we are trying to deploy to.
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
