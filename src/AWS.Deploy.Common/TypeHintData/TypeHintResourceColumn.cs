// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.Common.TypeHintData
{
    /// <summary>
    /// Represents the column for a list/grid of <see cref="TypeHintResource"/> rows
    /// </summary>
    public class TypeHintResourceColumn
    {
        /// <summary>
        /// Name of the column to be displayed to users
        /// </summary>
        public string DisplayName { get; set; }

        public TypeHintResourceColumn(string displayName)
        {
            DisplayName = displayName;
        }
    }
}
