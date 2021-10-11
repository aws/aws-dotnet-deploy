// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.CLI.ServerMode.Models
{
    public class TypeHintResourceSummary
    {
        public string SystemName { get; set; }
        public string DisplayName { get; set; }

        public TypeHintResourceSummary(string systemName, string displayName)
        {
            SystemName = systemName;
            DisplayName = displayName;
        }
    }
}
