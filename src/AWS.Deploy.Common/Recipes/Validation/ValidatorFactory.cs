// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace AWS.Deploy.Common.Recipes.Validation
{
    /// <summary>
    /// Factory that builds the validators for a given option or recipe
    /// </summary>
    public interface IValidatorFactory
    {
        /// <summary>
        /// Builds the validators that apply to the given option
        /// </summary>
        /// <param name="optionSettingItem">Option to validate</param>
        /// <param name="filter">Applies a filter to the list of validators</param>
        /// <returns>Array of validators for the given option</returns>
        IOptionSettingItemValidator[] BuildValidators(OptionSettingItem optionSettingItem, Func<OptionSettingItemValidatorConfig, bool>? filter = null);

        /// <summary>
        /// Builds the validators that apply to the given recipe
        /// </summary>
        /// <param name="recipeDefinition">Recipe to validate</param>
        /// <returns>Array of validators for the given recipe</returns>
        IRecipeValidator[] BuildValidators(RecipeDefinition recipeDefinition);
    }

    /// <summary>
    /// Builds <see cref="IOptionSettingItemValidator"/> and <see cref="IRecipeValidator"/> instances.
    /// </summary>
    public class ValidatorFactory : IValidatorFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ValidatorFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        private static readonly Dictionary<OptionSettingItemValidatorList, Type> _optionSettingItemValidatorTypeMapping = new()
        {
            { OptionSettingItemValidatorList.Range, typeof(RangeValidator) },
            { OptionSettingItemValidatorList.Regex, typeof(RegexValidator) },
            { OptionSettingItemValidatorList.Required, typeof(RequiredValidator) },
            { OptionSettingItemValidatorList.DirectoryExists, typeof(DirectoryExistsValidator) },
            { OptionSettingItemValidatorList.DockerBuildArgs, typeof(DockerBuildArgsValidator) },
            { OptionSettingItemValidatorList.DotnetPublishArgs, typeof(DotnetPublishArgsValidator) },
            { OptionSettingItemValidatorList.ExistingResource, typeof(ExistingResourceValidator) },
            { OptionSettingItemValidatorList.FileExists, typeof(FileExistsValidator) },
            { OptionSettingItemValidatorList.StringLength, typeof(StringLengthValidator) },
            { OptionSettingItemValidatorList.InstanceType, typeof(InstanceTypeValidator) },
            { OptionSettingItemValidatorList.SubnetsInVpc, typeof(SubnetsInVpcValidator) },
            { OptionSettingItemValidatorList.SecurityGroupsInVpc, typeof(SecurityGroupsInVpcValidator) },
            { OptionSettingItemValidatorList.Uri, typeof(UriValidator) },
            { OptionSettingItemValidatorList.Comparison, typeof(ComparisonValidator) }
        };

        private static readonly Dictionary<RecipeValidatorList, Type> _recipeValidatorTypeMapping = new()
        {
            { RecipeValidatorList.FargateTaskSizeCpuMemoryLimits, typeof(FargateTaskCpuMemorySizeValidator) },
            { RecipeValidatorList.ValidDockerfilePath, typeof(DockerfilePathValidator) }
        };

        public IOptionSettingItemValidator[] BuildValidators(OptionSettingItem optionSettingItem, Func<OptionSettingItemValidatorConfig, bool>? filter = null)
        {
            return optionSettingItem.Validators
                .Where(validator => filter != null ? filter(validator) : true)
                .Select(v => Activate(v.ValidatorType, v.Configuration, _optionSettingItemValidatorTypeMapping))
                .OfType<IOptionSettingItemValidator>()
                .ToArray();
        }

        public IRecipeValidator[] BuildValidators(RecipeDefinition recipeDefinition)
        {
            return recipeDefinition.Validators
                .Select(v => Activate(v.ValidatorType, v.Configuration, _recipeValidatorTypeMapping))
                .OfType<IRecipeValidator>()
                .ToArray();
        }

        private object? Activate<TValidatorList>(TValidatorList validatorType, object? configuration, Dictionary<TValidatorList, Type> typeMappings) where TValidatorList : struct
        {
            if (null == configuration)
            {
                var validatorInstance = ActivatorUtilities.CreateInstance(_serviceProvider, typeMappings[validatorType]);
                if (validatorInstance == null)
                    throw new InvalidValidatorTypeException(DeployToolErrorCode.UnableToCreateValidatorInstance, $"Could not create an instance of validator type {validatorType}");
                return validatorInstance;
            }

            if (configuration is JObject jObject)
            {
                var validatorInstance = JsonConvert.DeserializeObject(
                    JsonConvert.SerializeObject(jObject),
                    typeMappings[validatorType],
                    new JsonSerializerSettings
                    {
                        ContractResolver = new ServiceContractResolver(_serviceProvider)
                    });

                if (validatorInstance == null)
                    throw new InvalidValidatorTypeException(DeployToolErrorCode.UnableToCreateValidatorInstance, $"Could not create an instance of validator type {validatorType}");
                return validatorInstance;
            }

            return configuration;
        }
    }

    /// <summary>
    /// Custom contract resolver that can inject services from an IServiceProvider
    /// into the constructor of the type that is being deserialized from Json
    /// </summary>
    public class ServiceContractResolver : DefaultContractResolver
    {
        private readonly IServiceProvider _serviceProvider;

        public ServiceContractResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            var contract = base.CreateObjectContract(objectType);
            
            contract.DefaultCreator = () => ActivatorUtilities.CreateInstance(_serviceProvider, objectType);

            return contract;
        }
    }
}
