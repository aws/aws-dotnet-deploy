// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using AWS.Deploy.Common.Recipes.Validation;
using Newtonsoft.Json;

namespace AWS.Deploy.Common.Recipes
{
    /// <see cref="GetValue{T}"/>, <see cref="GetValue"/> and <see cref="SetValueOverride"/> methods
    public partial class OptionSettingItem
    {
        private object _valueOverride;

        public T GetValue<T>(IDictionary<string, string> replacementTokens = null, bool ignoreDefaultValue = false, IDictionary<string, bool> displayableOptionSettings = null)
        {
            var value = GetValue(replacementTokens, ignoreDefaultValue, displayableOptionSettings);
            if (value == null)
            {
                return default;
            }

            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
        }

        public object GetValue(IDictionary<string, string> replacementTokens = null, bool ignoreDefaultValue = false, IDictionary<string, bool> displayableOptionSettings = null)
        {
            replacementTokens ??= new Dictionary<string, string>();

            if (_valueOverride != null)
            {
                return _valueOverride;
            }

            if (Type == OptionSettingValueType.Object)
            {
                var objectValue = new Dictionary<string, object>();
                foreach (var childOptionSetting in ChildOptionSettings)
                {
                    var childValue = childOptionSetting.GetValue(replacementTokens, ignoreDefaultValue);

                    if (
                        displayableOptionSettings != null &&
                        displayableOptionSettings.TryGetValue(childOptionSetting.Id, out bool isDisplayable))
                    {
                        if (!isDisplayable)
                            continue;
                    }

                    if (childValue != null)
                    {
                        objectValue[childOptionSetting.Id] = childValue;
                    }
                }
                return objectValue.Any() ? objectValue : null;
            }

            if (ignoreDefaultValue)
            {
                return null;
            }

            if (DefaultValue == null)
            {
                return null;
            }

            if (DefaultValue is string defaultValueString)
            {
                return ApplyReplacementTokens(replacementTokens, defaultValueString);
            }

            return DefaultValue;
        }

        public T GetDefaultValue<T>(IDictionary<string, string> replacementTokens)
        {
            var value = GetDefaultValue(replacementTokens);
            if (value == null)
            {
                return default;
            }

            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
        }

        public object GetDefaultValue(IDictionary<string, string> replacementTokens)
        {
            if (DefaultValue == null)
            {
                return null;
            }

            if (DefaultValue is string defaultValueString)
            {
                return ApplyReplacementTokens(replacementTokens, defaultValueString);
            }

            return DefaultValue;
        }

        /// <summary>
        /// Assigns this Item a new value.
        /// </summary>
        /// <exception cref="ValidationFailedException">
        /// Thrown if one or more <see cref="Validators"/> determine
        /// <paramref name="valueOverride"/> is not valid.
        /// </exception>
        public void SetValueOverride(object valueOverride)
        {
            foreach (var validator in this.BuildValidators())
            {
                var result = validator.Validate(valueOverride);
                if (!result.IsValid)
                    throw new ValidationFailedException
                    {
                        ValidationResult = result
                    };
            }

            if (valueOverride is bool || valueOverride is int || valueOverride is long)
            {
                _valueOverride = valueOverride;
            }
            else if (valueOverride is string valueOverrideString)
            {
                if (bool.TryParse(valueOverrideString, out var valueOverrideBool))
                {
                    _valueOverride = valueOverrideBool;
                }
                else if (int.TryParse(valueOverrideString, out var valueOverrideInt))
                {
                    _valueOverride = valueOverrideInt;
                }
                else
                {
                    if (AllowedValues != null && AllowedValues.Count > 0 && !AllowedValues.Contains(valueOverrideString))
                        throw new InvalidOverrideValueException("Invalid value for option setting item");
                    _valueOverride = valueOverrideString;
                }
            }
            else
            {
                var deserialized = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(valueOverride));
                foreach (var childOptionSetting in ChildOptionSettings)
                {
                    if (deserialized.TryGetValue(childOptionSetting.Id, out var childValueOverride))
                    {
                        childOptionSetting.SetValueOverride(childValueOverride);
                    }
                }
            }
        }

        private string ApplyReplacementTokens(IDictionary<string, string> replacementTokens, string defaultValue)
        {
            foreach (var token in replacementTokens)
            {
                defaultValue = defaultValue.Replace(token.Key, token.Value);
            }

            return defaultValue;
        }
    }
}
