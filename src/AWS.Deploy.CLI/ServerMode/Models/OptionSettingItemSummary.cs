// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using AWS.Deploy.Common.Recipes;

namespace AWS.Deploy.CLI.ServerMode.Models
{
    public class OptionSettingItemSummary
    {
        /// <summary>
        /// The unique id of setting. This value will be persisted in other config files so its value should never change once a recipe is released.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The fully qualified id of the setting that includes the Id and all of the parent's Ids.
        /// This helps easily reference the Option Setting without context of the parent setting.
        /// </summary>
        public string FullyQualifiedId { get; set; }

        /// <summary>
        /// The display friendly name of the setting.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The category for the setting. This value must match an id field in the list of categories.
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// The description of what the setting is used for.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The value used for the recipe if it is set by the user.
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// The type of primitive value expected for this setting.
        /// For example String, Int
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Hint the the UI what type of setting this is optionally add additional UI features to select a value.
        /// For example a value of BeanstalkApplication tells the UI it can display the list of available Beanstalk applications for the user to pick from.
        /// </summary>
        public string? TypeHint { get; set; }

        /// <summary>
        /// Type hint additional data required to facilitate handling of the option setting.
        /// </summary>
        public Dictionary<string, object> TypeHintData { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// UI can use this to reduce the amount of settings to show to the user when confirming the recommendation. This can make it so the user sees only the most important settings
        /// they need to know about before deploying.
        /// </summary>
        public bool Advanced { get; set; }

        /// <summary>
        /// Indicates whether the setting can be edited
        /// </summary>
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Indicates whether the setting is visible/displayed on the UI
        /// </summary>
        public bool Visible { get; set; }

        /// <summary>
        /// Indicates whether the setting can be displayed as part of the settings summary of the previous deployment.
        /// </summary>
        public bool SummaryDisplayable { get; set; }

        /// <summary>
        /// The allowed values for the setting.
        /// </summary>
        public IList<string> AllowedValues { get; set; } = new List<string>();

        /// <summary>
        /// The value mapping for allowed values. The key of the dictionary is what is sent to services
        /// and the value is the display value shown to users.
        /// </summary>
        public IDictionary<string, string> ValueMapping { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Child option settings for <see cref="OptionSettingValueType.Object"/> value types
        /// <see cref="ChildOptionSettings"/> value depends on the values of <see cref="ChildOptionSettings"/>
        /// </summary>
        public List<OptionSettingItemSummary> ChildOptionSettings { get; set; } = new();

        /// <summary>
        /// The validation state of the setting that contains the validation status and message.
        /// </summary>
        public OptionSettingValidation Validation { get; set; }

        public OptionSettingItemSummary(string id, string fullyQualifiedId, string name, string description, string type)
        {
            Id = id;
            FullyQualifiedId = fullyQualifiedId;
            Name = name;
            Description = description;
            Type = type;
            Validation = new OptionSettingValidation();
        }
    }
}
