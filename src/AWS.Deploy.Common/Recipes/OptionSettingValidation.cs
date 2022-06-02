// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.Common.Recipes
{
    public enum ValidationStatus
    {
        Valid,
        Invalid
    }

    public class OptionSettingValidation
    {
        /// <summary>
        /// Determines whether the current value as set by the user is in a valid or invalid state.
        /// </summary>
        public ValidationStatus ValidationStatus { get; set; } = ValidationStatus.Valid;

        /// <summary>
        /// The validation message in the case where the value set by the user is an invalid one.
        /// This is empty in the case where the value set by the user is a valid one.
        /// </summary>
        public string ValidationMessage { get; set; } = string.Empty;

        /// <summary>
        /// The value last attempted to be set by the user in the case where that value is invalid.
        /// This is null in the case where the value is valid.
        /// </summary>
        public object? InvalidValue { get; set; }
    }
}
