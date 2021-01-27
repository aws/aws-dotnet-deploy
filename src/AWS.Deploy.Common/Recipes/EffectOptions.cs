// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.Common.Recipes
{
    /// <summary>
    /// The type of effects to apply.
    /// </summary>
    public class EffectOptions
    {
        /// <summary>
        /// When the recipe should be included or not.
        /// </summary>
        public bool? Include { get; set; }

        /// <summary>
        /// Adjust the priority or the recipe.
        /// </summary>
        public int? PriorityAdjustment { get; set; }
    }
}
