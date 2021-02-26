// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AWS.Deploy.Orchestrator.RecommendationEngine
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
