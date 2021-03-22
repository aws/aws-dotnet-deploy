// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;

namespace AWS.Deploy.Orchestration.RecommendationEngine
{
    public class NuGetPackageReferenceTest : BaseRecommendationTest
    {
        public override string Name => "NuGetPackageReference";

        public override Task<bool> Execute(RecommendationTestInput input)
        {
            var result = !string.IsNullOrEmpty(input.ProjectDefinition.GetPackageReferenceVersion(input.Test.Condition.NuGetPackageName));
            return Task.FromResult(result);
        }
    }
}
