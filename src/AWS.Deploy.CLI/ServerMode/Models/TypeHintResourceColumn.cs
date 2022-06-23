// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.CLI.ServerMode.Models
{
    /// <summary>
    /// Represents a column for a list/grid of <see cref="TypeHintResourceSummary"/> rows
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
