// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace AWS.Deploy.Common.Recipes.Validation
{
    /// <summary>
    /// Builds <see cref="IOptionSettingItemValidator"/> and <see cref="IRecipeValidator"/> instances.
    /// </summary>
    public static class ValidatorFactory
    {
        private static readonly Dictionary<OptionSettingItemValidatorList, Type> _optionSettingItemValidatorTypeMapping = new()
        {
            { OptionSettingItemValidatorList.Range, typeof(RangeValidator) },
            { OptionSettingItemValidatorList.Regex, typeof(RegexValidator) },
            { OptionSettingItemValidatorList.Required, typeof(RequiredValidator) }
        };

        private static readonly Dictionary<RecipeValidatorList, Type> _recipeValidatorTypeMapping = new()
        {
            { RecipeValidatorList.FargateTaskSizeCpuMemoryLimits, typeof(FargateTaskCpuMemorySizeValidator) }
        };

        public static IOptionSettingItemValidator[] BuildValidators(this OptionSettingItem optionSettingItem)
        {
            return optionSettingItem.Validators
                .Select(v => Activate(v.ValidatorType, v.Configuration, _optionSettingItemValidatorTypeMapping))
                .OfType<IOptionSettingItemValidator>()
                .ToArray();
        }

        public static IRecipeValidator[] BuildValidators(this RecipeDefinition recipeDefinition)
        {
            return recipeDefinition.Validators
                .Select(v => Activate(v.ValidatorType, v.Configuration,_recipeValidatorTypeMapping))
                .OfType<IRecipeValidator>()
                .ToArray();
        }

        private static object? Activate<TValidatorList>(TValidatorList validatorType, object? configuration, Dictionary<TValidatorList, Type> typeMappings) where TValidatorList : struct
        {
            if (null == configuration)
            {
                var validatorInstance = Activator.CreateInstance(typeMappings[validatorType]);
                if (validatorInstance == null)
                    throw new InvalidValidatorTypeException($"Could not create an instance of validator type {validatorType}");
                return validatorInstance;
            }

            if (configuration is JObject jObject)
            {
                var validatorInstance = jObject.ToObject(typeMappings[validatorType]);
                if (validatorInstance == null)
                    throw new InvalidValidatorTypeException($"Could not create an instance of validator type {validatorType}");
                return validatorInstance;
            }

            return configuration;
        }
    }
}
