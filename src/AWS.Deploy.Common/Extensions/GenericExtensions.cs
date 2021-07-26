// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using Newtonsoft.Json;

namespace AWS.Deploy.Common.Extensions
{
    public static class GenericExtensions
    {
        public static T DeepCopy<T>(this T obj)
        {
            var serializedObject = JsonConvert.SerializeObject(obj);
            return JsonConvert.DeserializeObject<T>(serializedObject);
        }
    }
}
