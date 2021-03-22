// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;

namespace AWS.Deploy.Orchestration.RecommendationEngine
{
    /// <summary>
    /// The base class for all recommendation tests used to run the logic for a model test to see if a recipe is valid for a given project.
    /// The RecommendationEngine will load up all types that extends from this base class and register them by their Name. 
    /// </summary>
    public abstract class BaseRecommendationTest
    {
        /// <summary>
        /// The name of the test. This will match the value used in recipes when defining the test they want to perform.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Executes the test
        /// </summary>
        /// <param name="input"></param>
        /// <returns>True for successful test pass, otherwise false.</returns>
        public abstract Task<bool> Execute(RecommendationTestInput input);
    }
}
