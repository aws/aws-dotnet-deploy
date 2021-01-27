// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace AWS.Deploy.Common.Recipes
{
    /// <summary>
    /// Container for the types of rules used by the recommendation engine.
    /// </summary>
    public class RecommendationRuleItem
    {
        /// <summary>
        /// The list of tests to run for the rule.
        /// </summary>
        public IList<RuleTest> Tests { get; set; } = new List<RuleTest>();

        /// <summary>
        /// The effect of the rule based on whether the test pass or not. If the effect is not defined
        /// the effect is the Include option matches the result of the test passing.
        /// </summary>
        public RuleEffect Effect { get; set; }
    }
}
