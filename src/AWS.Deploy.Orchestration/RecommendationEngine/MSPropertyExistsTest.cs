// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;

namespace AWS.Deploy.Orchestration.RecommendationEngine
{
    /// <summary>
    /// This test checks to see if a property in a PropertyGroup of the .NET project exists.
    /// </summary>
    public class MSPropertyExistsTest : BaseRecommendationTest
    {
        public override string Name => "MSPropertyExists";

        public override Task<bool> Execute(RecommendationTestInput input)
        {
            var result = !string.IsNullOrEmpty(input.ProjectDefinition.GetMSPropertyValue(input.Test.Condition.PropertyName));
            return Task.FromResult(result);
        }
    }
}
