// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using AWS.Deploy.Common.Recipes;

namespace AWS.Deploy.Common.TypeHintData
{
    /// <summary>
    /// Holds additional data for <see cref="OptionSettingTypeHint.S3BucketName"/> processing.
    /// </summary>
    public class S3BucketNameTypeHintData
    {
        /// <summary>
        /// Determines whether to allow no value or not.
        /// </summary>
        public bool AllowNoValue { get; set; }

        public S3BucketNameTypeHintData(bool allowNoValue)
        {
            AllowNoValue = allowNoValue;
        }
    }
}
