// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using AWS.Deploy.Common.Recipes;

namespace AWS.Deploy.Common.TypeHintData
{
    /// <summary>
    /// Holds additional data for <see cref="OptionSettingTypeHint.IAMRole"/> processing.
    /// </summary>
    public class IAMTypeHintData
    {
        /// <summary>
        /// ServicePrincipal to filter IAM roles.
        /// </summary>
        public string ServicePrincipal { get; set; }
    }
}
