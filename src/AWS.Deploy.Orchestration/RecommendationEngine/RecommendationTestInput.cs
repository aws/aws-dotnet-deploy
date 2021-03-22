// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;

namespace AWS.Deploy.Orchestration.RecommendationEngine
{
    /// <summary>
    /// The input fields passed into recommendation tests.
    /// </summary>
    public class RecommendationTestInput
    {
        /// <summary>
        /// The modeled test and its conditions from the recipe.
        /// </summary>
        public RuleTest Test { get; set; }

        /// <summary>
        /// The definition of the project which provides access to project metadata.
        /// </summary>
        public ProjectDefinition ProjectDefinition { get; set; }

        /// <summary>
        /// The session that provides access to the AWS credentials and region configured. This allows
        /// potential tests to check for AWS resources in the account being deployed to.
        /// </summary>
        public OrchestratorSession Session { get; set; }
    }
}
