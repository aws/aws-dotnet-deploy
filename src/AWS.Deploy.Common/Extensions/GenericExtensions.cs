// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using Newtonsoft.Json;

namespace AWS.Deploy.Common.Extensions
{
    public static class GenericExtensions
    {
        public static T DeepCopy<T>(this T obj)
        {
            var serializedObject = JsonConvert.SerializeObject(obj);
            return JsonConvert.DeserializeObject<T>(serializedObject) ??
                throw new FailedToDeserializeException(DeployToolErrorCode.FailedToCreateDeepCopy, "Failed to create a deep copy.");
        }

        public static bool TryDeserialize<T>(this object obj, out T? inputList)
        {
            try
            {
                string serializedObject = "";
                if (obj is string)
                {
                    serializedObject = obj.ToString() ?? string.Empty;
                }
                else
                {
                    serializedObject = JsonConvert.SerializeObject(obj);
                }
                inputList = JsonConvert.DeserializeObject<T>(serializedObject);

                if (inputList == null)
                    return false;

                return true;
            }
            catch (Exception)
            {
                inputList = default(T);
                return false;
            }
        }
    }
}
