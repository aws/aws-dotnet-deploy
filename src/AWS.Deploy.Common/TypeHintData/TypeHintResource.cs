// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.Common.TypeHintData
{
    public class TypeHintResource
    {
        public string SystemName { get; set; }
        public string DisplayName { get; set; }

        public TypeHintResource(string systemName, string displayName)
        {
            SystemName = systemName;
            DisplayName = displayName;
        }
    }
}
