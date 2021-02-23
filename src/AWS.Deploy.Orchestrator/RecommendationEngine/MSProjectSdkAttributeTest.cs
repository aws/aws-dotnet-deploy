// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;

namespace AWS.Deploy.Orchestrator.RecommendationEngine
{
    /// <summary>
    /// This test checks the value of the Sdk attribute of the root Project node of a .NET project file.
    /// </summary>
    public class MSProjectSdkAttributeTest : BaseRecommendationTest
    {
        public override string Name => "MSProjectSdkAttribute";

        public override Task<bool> Execute(RecommendationTestInput input)
        {
            var result = string.Equals(input.ProjectDefinition.SdkType, input.Test.Condition.Value, StringComparison.InvariantCultureIgnoreCase);
            return Task.FromResult(result);
        }
    }
}
