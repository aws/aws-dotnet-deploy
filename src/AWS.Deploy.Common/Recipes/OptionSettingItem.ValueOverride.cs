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
        public T GetValue<T>(IDictionary<string, string> replacementTokens, IDictionary<string, bool>? displayableOptionSettings = null)
        {
            var value = GetValue(replacementTokens, displayableOptionSettings);

            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
        }

        public object GetValue(IDictionary<string, string> replacementTokens, IDictionary<string, bool>? displayableOptionSettings = null)
        {
            if (_value != null)
            {
                return _value;
            }

            if (Type == OptionSettingValueType.Object)
            {
                var objectValue = new Dictionary<string, object>();
                foreach (var childOptionSetting in ChildOptionSettings)
                {
                    var childValue = childOptionSetting.GetValue(replacementTokens);

                    if (
                        displayableOptionSettings != null &&
                        displayableOptionSettings.TryGetValue(childOptionSetting.Id, out bool isDisplayable))
                    {
                        if (!isDisplayable)
                            continue;
                    }

                    objectValue[childOptionSetting.Id] = childValue;
                }
                return objectValue;
            }

            if (DefaultValue == null)
            {
                return string.Empty;
            }

            if (DefaultValue is string defaultValueString)
            {
                return ApplyReplacementTokens(replacementTokens, defaultValueString);
            }

            return DefaultValue;
        }

        public T? GetDefaultValue<T>(IDictionary<string, string> replacementTokens)
        {
            var value = GetDefaultValue(replacementTokens);
            if (value == null)
            {
                return default;
            }

            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
        }

        public object? GetDefaultValue(IDictionary<string, string> replacementTokens)
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
        /// Assigns a value to the OptionSettingItem.
        /// </summary>
        /// <exception cref="ValidationFailedException">
        /// Thrown if one or more <see cref="Validators"/> determine
        /// <paramref name="valueOverride"/> is not valid.
        /// </exception>
        public void SetValue(IOptionSettingHandler optionSettingHandler, object valueOverride)
        {
            var isValid = true;
            var validationFailedMessage = string.Empty;
            foreach (var validator in this.BuildValidators())
            {
                var result = validator.Validate(valueOverride);
                if (!result.IsValid)
                {
                    isValid = false;
                    validationFailedMessage += result.ValidationFailedMessage + Environment.NewLine;
                }
            }
            if (!isValid)
                throw new ValidationFailedException(DeployToolErrorCode.OptionSettingItemValueValidationFailed, validationFailedMessage.Trim());

            if (AllowedValues != null && AllowedValues.Count > 0 && valueOverride != null &&
                !AllowedValues.Contains(valueOverride.ToString() ?? ""))
                throw new InvalidOverrideValueException(DeployToolErrorCode.InvalidValueForOptionSettingItem, $"Invalid value for option setting item '{Name}'");

            if (valueOverride is bool || valueOverride is int || valueOverride is long || valueOverride is double || valueOverride is Dictionary<string, string> || valueOverride is SortedSet<string>)
            {
                _value = valueOverride;
            }
            else if (Type.Equals(OptionSettingValueType.KeyValue))
            {
                var deserialized = JsonConvert.DeserializeObject<Dictionary<string, string>>(valueOverride?.ToString() ?? "");
                _value = deserialized;
            }
            else if (Type.Equals(OptionSettingValueType.List))
            {
                var deserialized = JsonConvert.DeserializeObject<SortedSet<string>>(valueOverride?.ToString() ?? "");
                _value = deserialized;
            }
            else if (valueOverride is string valueOverrideString)
            {
                if (bool.TryParse(valueOverrideString, out var valueOverrideBool))
                {
                    _value = valueOverrideBool;
                }
                else if (int.TryParse(valueOverrideString, out var valueOverrideInt))
                {
                    _value = valueOverrideInt;
                }
                else
                {
                    _value = valueOverrideString;
                }
            }
            else
            {
                var deserialized = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(valueOverride));
                foreach (var childOptionSetting in ChildOptionSettings)
                {
                    if (deserialized.TryGetValue(childOptionSetting.Id, out var childValueOverride))
                    {
                        optionSettingHandler.SetOptionSettingValue(childOptionSetting, childValueOverride);
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
