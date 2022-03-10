// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;

namespace AWS.Deploy.Orchestration.RecommendationEngine
{
    /// <summary>
    /// This test checks the value of the Sdk attribute of the root Project node of a .NET project file.
    /// </summary>
    public class MSProjectSdkAttributeTest : BaseRecommendationTest
    {
        public override string Name => "MSProjectSdkAttribute";

        public override Task<bool> Execute(RecommendationTestInput input)
        {
            bool result = false;
            if(!string.IsNullOrEmpty(input.Test.Condition.Value))
            {
                result = string.Equals(input.ProjectDefinition.SdkType, input.Test.Condition.Value, StringComparison.InvariantCultureIgnoreCase);
            }
            else if(input.Test.Condition.AllowedValues?.Count > 0)
            {
                result = input.Test.Condition.AllowedValues.Contains(input.ProjectDefinition.SdkType);
            }

            return Task.FromResult(result);
        }
    }
}
