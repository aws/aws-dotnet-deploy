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
                var objectValue = ChildOptionSettings
                    .ToDictionary(childOptionSetting => childOptionSetting.Id, childOptionSetting => childOptionSetting.GetValue(replacementTokens, ignoreDefaultValue));

                return objectValue;
            }

            if (ignoreDefaultValue)
            {
                return null;
            }

            if (DefaultValue == null)
            {
                return null;
            }

            var mappedValue = string.Copy(DefaultValue);
            if (ValueMapping != null && ValueMapping.ContainsKey(DefaultValue))
            {
                mappedValue = ValueMapping[DefaultValue];
            }

            mappedValue = ApplyReplacementTokens(replacementTokens, mappedValue);
            return mappedValue;
        }

        public void SetValueOverride(object valueOverride)
        {
            if (valueOverride is bool || valueOverride is int || valueOverride is long)
            {
                _valueOverride = valueOverride;
            }
            else if (valueOverride is string valueOverrideString)
            {
                if (ValueMapping != null && ValueMapping.ContainsKey(valueOverrideString))
                {
                    valueOverrideString = ValueMapping[valueOverrideString];
                }

                _valueOverride = valueOverrideString;
            }
            else
            {
                var deserialized = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(valueOverride));
                foreach (var childOptionSetting in ChildOptionSettings)
                {
                    childOptionSetting.SetValueOverride(deserialized[childOptionSetting.Id]);
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
