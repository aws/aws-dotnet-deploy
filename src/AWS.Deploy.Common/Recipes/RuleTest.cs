// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.Common.Recipes
{
    /// <summary>
    /// Test for a rule
    /// </summary>
    public class RuleTest
    {
        /// <summary>
        /// The type of test to run
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The conditions for the tests
        /// </summary>
        public RuleCondition Condition { get; set; }
    }
}
