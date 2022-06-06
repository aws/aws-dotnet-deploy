// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace AWS.Deploy.Common.TypeHintData
{
    /// <summary>
    /// Represents a single AWS resource, generally used when selecting from
    /// a list of existing resources to set an OptionSettingItem
    /// </summary>
    public class TypeHintResource
    {
        /// <summary>
        /// Resource id, used when saving a selected resource to an OptionSettingItem
        /// </summary>
        public string SystemName { get; set; }

        /// <summary>
        /// Resource name, used for display
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Additional data about the resource, which may be used when displaying a table
        /// or grid of options for the user to select from. The indices into this list should
        /// match the column indicies of <see cref="TypeHintResourceTable.Columns"/>
        /// </summary>
        public List<string> ColumnValues { get; set; } = new List<string>();

        public TypeHintResource(string systemName, string displayName)
        {
            SystemName = systemName;
            DisplayName = displayName;
        }
    }
}
