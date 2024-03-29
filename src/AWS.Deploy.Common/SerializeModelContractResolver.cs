// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AWS.Deploy.Common
{
    public class SerializeModelContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            if (property != null && property.PropertyType != null && property.PropertyName != null && property.PropertyType != typeof(string))
            {
                if (property.PropertyType.GetInterface(nameof(IEnumerable)) != null)
                {
                    property.ShouldSerialize = instance =>
                    {
                        var instanceValue = instance?.GetType()?.GetProperty(property.PropertyName)?.GetValue(instance);
                        if (instanceValue is IEnumerable<object> list)
                        {
                            return list.Any();
                        }
                        else if(instanceValue is System.Collections.IDictionary map)
                        {
                            return map.Count > 0;
                        }
                            

                        return false;
                    };
                }
            }
            return property ?? throw new ArgumentException();
        }
    }
}
