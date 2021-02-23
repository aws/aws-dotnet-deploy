// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace AWS.Deploy.Common.Recipes
{
    /// <see cref="GetValue{T}"/>, <see cref="GetValue"/> and <see cref="SetValueOverride"/> methods
    public partial class OptionSettingItem
    {
        private object _valueOverride;

        public T GetValue<T>(IDictionary<string, string> replacementTokens, bool ignoreDefaultValue = false)
        {
            var value = GetValue(replacementTokens, ignoreDefaultValue);
            if (value == null)
            {
                return default;
            }

            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
        }

        public object GetValue(IDictionary<string, string> replacementTokens, bool ignoreDefaultValue = false)
        {
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

        public void SetValueOverride(object valueOverride)
        {
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
                    if (AllowedValues != null && !AllowedValues.Contains(valueOverrideString))
                        throw new InvalidOverrideValueException();
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
