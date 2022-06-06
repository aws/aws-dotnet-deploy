// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace AWS.Deploy.CLI.ServerMode.Models
{
    /// <summary>
    /// Represents a list or table of options, generally used when selecting from
    /// a list of existing AWS resources to set an OptionSettingItem
    /// </summary>
    public class GetConfigSettingResourcesOutput
    {
        /// <summary>
        /// Columns that should appear above the list of resources when
        /// presenting the user a table to select from
        /// </summary>
        /// <remarks>
        /// If this is null or empty, it implies that there is only a single column.
        /// This may be better suited for a simple dropdown as opposed to a table or modal select.
        /// </remarks>
        public List<TypeHintResourceColumn>? Columns { get; set; }

        /// <summary>
        /// List of resources that the user could select from
        /// </summary>
        public List<TypeHintResourceSummary>? Resources { get; set; }
    }
}
