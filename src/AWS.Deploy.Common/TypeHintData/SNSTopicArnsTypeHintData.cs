// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using AWS.Deploy.Common.Recipes;

namespace AWS.Deploy.Common.TypeHintData
{
    /// <summary>
    /// Holds additional data for <see cref="OptionSettingTypeHint.SNSTopicArn"/> processing.
    /// </summary>
    public class SNSTopicArnsTypeHintData
    {
        /// <summary>
        /// Determines whether to allow no value or not.
        /// </summary>
        public bool AllowNoValue { get; set; }

        public SNSTopicArnsTypeHintData(bool allowNoValue)
        {
            AllowNoValue = allowNoValue;
        }
    }
}
