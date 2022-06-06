// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace AWS.Deploy.Common.TypeHintData
{
    /// <summary>
    /// Represents a list or table of existing AWS resources to allow selecting from
    /// a list of existing resources when setting an OptionSettingItem
    /// </summary>
    public class TypeHintResourceTable
    {
        /// <summary>
        /// Columns that should appear above the list of resources when presenting the
        /// user a table or grid to select from
        /// </summary>
        /// <remarks>If this is null or empty, it implies that there is only a single column</remarks>
        public List<TypeHintResourceColumn>? Columns { get; set; }

        /// <summary>
        /// List of AWS resources that the user could select from
        /// </summary>
        public List<TypeHintResource> Rows { get; set; } = new List<TypeHintResource>();
    }
}
