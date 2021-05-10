// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;

namespace AWS.Deploy.CLI
{
    /// <summary>
    /// <see cref="UserInputConfiguration{T}"/> encapsulates the input configuration
    /// when the user is asked to select an existing option or create new
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class UserInputConfiguration<T>
    {
        /// <summary>
        /// Func to select string property from the option type
        /// which is displayed to the user.
        /// </summary>
        public Func<T, string> DisplaySelector;

        /// <summary>
        /// Func to select default option from the options
        /// which is displayed as default option to the user.
        /// </summary>
        public Func<T, bool> DefaultSelector;

        /// <summary>
        /// The current value for the option setting.
        /// </summary>
        public object? CurrentValue;

        /// <summary>
        /// If true, ask for the new name
        /// <para />
        /// Default is <c>false</c>
        /// </summary>
        public bool AskNewName { get; set; }

        /// <summary>
        /// If <see cref="AskNewName"/> is set to true,
        /// then <see cref="DefaultNewName"/> is displayed as the default new name to the user.
        /// </summary>
        public string DefaultNewName { get; set; }

        /// <summary>
        /// If <see cref="CanBeEmpty" /> is set to true,
        /// then an empty user input is considered as a valid value.
        /// <para />
        /// Default is <c>false</c>
        /// </summary>
        public bool CanBeEmpty { get; set; }

        /// <summary>
        /// If <see cref="CreateNew" /> is set to true,
        /// then a "Create New" option will be added to the list of valid options.
        /// </summary>
        public bool CreateNew { get; set; } = true;

        /// <summary>
        /// If <see cref="EmptyOption" /> is set to true,
        /// then an "Empty" option will be added to the list of valid options.
        /// </summary>
        public bool EmptyOption { get; set; }

        public UserInputConfiguration(
            Func<T, string> displaySelector,
            Func<T, bool> defaultSelector,
            string defaultNewName = "")
        {
            DisplaySelector = displaySelector;
            DefaultSelector = defaultSelector;
            DefaultNewName = defaultNewName;
        }
    }
}
