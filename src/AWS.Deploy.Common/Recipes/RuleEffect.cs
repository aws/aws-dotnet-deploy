// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.Common.Recipes
{
    /// <summary>
    /// The effect to apply for the test run.
    /// </summary>
    public class RuleEffect
    {
        /// <summary>
        /// The effects to run if all the test pass.
        /// </summary>
        public EffectOptions? Pass { get; set; }

        /// <summary>
        /// The effects to run if all the test fail.
        /// </summary>
        public EffectOptions? Fail { get; set; }
    }

}
