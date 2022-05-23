// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudControlApi.Model;
using AWS.Deploy.Common.Data;

namespace AWS.Deploy.Common.Recipes.Validation
{
    public class ExistingResourceValidator : IOptionSettingItemValidator
    {
        private readonly IAWSResourceQueryer _awsResourceQueryer;

        /// <summary>
        /// The Cloud Control API resource type that will be used to query Cloud Control API for the existance of a resource.
        /// </summary>
        public string? ResourceType { get; set; }

        public ExistingResourceValidator(IAWSResourceQueryer awsResourceQueryer)
        {
            _awsResourceQueryer = awsResourceQueryer;
        }

        public async Task<ValidationResult> Validate(object input)
        {
            if (string.IsNullOrEmpty(ResourceType))
                throw new MissingValidatorConfigurationException(DeployToolErrorCode.MissingValidatorConfiguration, $"The validator of type '{typeof(ExistingResourceValidator)}' is missing the configuration property '{nameof(ResourceType)}'.");
            var resourceName = input.ToString();
            if (string.IsNullOrEmpty(resourceName))
                return ValidationResult.Failed($"The resource name is empty and cannot be validated.");

            switch (ResourceType)
            {
                case "AWS::ElasticBeanstalk::Application":
                    var beanstalkApplications = await _awsResourceQueryer.ListOfElasticBeanstalkApplications(input.ToString());
                    if (beanstalkApplications.Any(x => x.ApplicationName.Equals(input.ToString())))
                        return ValidationResult.Failed($"An Elastic Beanstalk application already exists with the name '{input.ToString()}'. Check the AWS Console for more information on the existing resource.");
                    break;

                case "AWS::ElasticBeanstalk::Environment":
                    var beanstalkEnvironments = await _awsResourceQueryer.ListOfElasticBeanstalkEnvironments(environmentName: input.ToString());
                    if (beanstalkEnvironments.Any(x => x.EnvironmentName.Equals(input.ToString())))
                        return ValidationResult.Failed($"An Elastic Beanstalk environment already exists with the name '{input.ToString()}'. Check the AWS Console for more information on the existing resource.");
                    break;

                default:
                    try
                    {
                        var resource = await _awsResourceQueryer.GetCloudControlApiResource(ResourceType, resourceName);
                        return ValidationResult.Failed($"A resource of type '{ResourceType}' and name '{resourceName}' already exists. Check the AWS Console for more information on the existing resource.");
                    }
                    catch (ResourceQueryException ex) when (ex.InnerException is ResourceNotFoundException)
                    {
                        break;
                    }
            }

            return ValidationResult.Valid();
        }
    }
}
