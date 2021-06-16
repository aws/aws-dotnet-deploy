// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.Common.Recipes.Validation
{
    public enum OptionSettingItemValidatorList
    {
        /// <summary>
        /// Must be paired with <see cref="RangeValidator"/>
        /// </summary>
        Range,
        /// <summary>
        /// Must be paired with <see cref="RegexValidator"/>
        /// </summary>
        Regex,
        /// <summary>
        /// Must be paired with <see cref="RequiredValidator"/>
        /// </summary>
        Required
    }
}
