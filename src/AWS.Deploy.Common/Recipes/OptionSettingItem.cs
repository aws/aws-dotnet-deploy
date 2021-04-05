// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using AWS.Deploy.Common.Recipes.Validation;
using Newtonsoft.Json;

namespace AWS.Deploy.Common.Recipes
{
    /// <summary>
    /// Container for the setting values
    /// </summary>
    public partial class OptionSettingItem
    {
        /// <summary>
        /// The unique id of setting. This value will be persisted in other config files so its value should never change once a recipe is released.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The id of the parent option setting. This is used for tooling that wants to look up the existing resources for a setting based on the TypeHint but needs
        /// to know the parent AWS resource. For example if listing the available Beanstalk environments the listing should be for the environments of the Beanstalk application.
        /// </summary>
        public string ParentSettingId { get; set; }

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
        public OptionSettingValueType Type { get; set; }

        /// <summary>
        /// Hint the the UI what type of setting this is optionally add additional UI features to select a value.
        /// For example a value of BeanstalkApplication tells the UI it can display the list of available Beanstalk applications for the user to pick from.
        /// </summary>
        public OptionSettingTypeHint? TypeHint { get; set; }

        /// <summary>
        /// The default value used for the recipe if the user doesn't override the value.
        /// </summary>
        public object DefaultValue { get; set; }

        /// <summary>
        /// UI can use this to reduce the amount of settings to show to the user when confirming the recommendation. This can make it so the user sees only the most important settings
        /// they need to know about before deploying.
        /// </summary>
        public bool AdvancedSetting { get; set; }

        /// <summary>
        /// If true the setting can be changed during a redeployment.
        /// </summary>
        public bool Updatable { get; set; }

        /// <summary>
        /// TODO
        /// </summary>
        public List<OptionSettingItemValidatorConfig> Validators { get; set; } = new ();
        
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
        /// Property will be displayed if specified dependencies are true
        /// </summary>
        public IList<PropertyDependency> DependsOn { get; set; } = new List<PropertyDependency>();

        /// <summary>
        /// Child option settings for <see cref="OptionSettingValueType.Object"/> value types
        /// <see cref="ChildOptionSettings"/> value depends on the values of <see cref="ChildOptionSettings"/>
        /// </summary>
        public List<OptionSettingItem> ChildOptionSettings { get; set; } = new ();

        /// <summary>
        /// Type hint additional data required to facilitate handling of the option setting.
        /// </summary>
        public Dictionary<string, object> TypeHintData { get; set; } = new ();

        /// <summary>
        /// Helper method to get strongly type <see cref="TypeHintData"/>.
        /// </summary>
        /// <typeparam name="T">Type of the type hint data</typeparam>
        /// <returns>Returns strongly type type hint data. Returns default value if <see cref="TypeHintData"/> is empty.</returns>
        public T GetTypeHintData<T>()
        {
            if (!TypeHintData.Any())
            {
                return default;
            }

            var serialized = JsonConvert.SerializeObject(TypeHintData);
            return JsonConvert.DeserializeObject<T>(serialized);
        }
    }
}
