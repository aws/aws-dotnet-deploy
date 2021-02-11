// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.Common.Recipes
{
    /// <summary>
    /// Holds additional data for <see cref="OptionSettingTypeHint"/> processing.
    /// </summary>
    public class OptionSettingTypeHintData
    {
        /// <summary>
        /// ServicePrincipal to filter IAM roles while handling <see cref="OptionSettingTypeHint.IAMRole"/>
        /// </summary>
        public string ServicePrincipal { get; set; }
    }
}
