// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using AWS.Deploy.Common.Recipes;

namespace AWS.Deploy.CLI.ServerMode.Models
{
    /// <summary>
    /// Represents a recipe <see cref="OptionSettingItem"/> returned through server mode.
    /// </summary>
    public class RecipeOptionSettingSummary
    {
        /// <summary>
        /// The unique id of setting.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The display friendly name of the setting.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The description of what the setting is used for.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The type of primitive value expected for this setting.
        /// For example String, Int
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Child option settings for <see cref="OptionSettingValueType.Object"/> value types.
        /// </summary>
        public List<RecipeOptionSettingSummary> Settings { get; set; } = new List<RecipeOptionSettingSummary>();

        public RecipeOptionSettingSummary(string id, string name, string description, string type)
        {
            Id = id;
            Name = name;
            Description = description;
            Type = type;
        }
    }
}
