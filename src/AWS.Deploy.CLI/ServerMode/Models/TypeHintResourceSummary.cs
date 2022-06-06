// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace AWS.Deploy.CLI.ServerMode.Models
{
    /// <summary>
    /// Represents a single type hint option, generally used when selecting from
    /// a list of existing AWS resources to set an OptionSettingItem
    /// </summary>
    public class TypeHintResourceSummary
    {
        /// <summary>
        /// Resource Id, used when saving a selected resource to an OptionSettingItem
        /// </summary>
        public string SystemName { get; set; }

        /// <summary>
        /// Resource name, used for display
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Additional data about the resource, which may be used when displaying a table
        /// or grid of options for the user to select from. The indices into this list should
        /// match the column indicies of a list of <see cref="TypeHintResourceColumn"/>
        /// </summary>
        public List<string> ColumnDisplayValues { get; set; }

        public TypeHintResourceSummary(string systemName, string displayName, List<string> columnDisplayValues)
        {
            SystemName = systemName;
            DisplayName = displayName;
            ColumnDisplayValues = new List<string>(columnDisplayValues);

        }
    }
}
