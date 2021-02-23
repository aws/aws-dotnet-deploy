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
    /// This test checks to see if the value of a property in a PropertyGroup of the .NET project exists.
    /// </summary>
    public class MSPropertyTest : BaseRecommendationTest
    {
        public override string Name => "MSProperty";

        public override Task<bool> Execute(RecommendationTestInput input)
        {
            var propertyValue = input.ProjectDefinition.GetMSPropertyValue(input.Test.Condition.PropertyName);
            var result = (propertyValue != null && input.Test.Condition.AllowedValues.Contains(propertyValue));
            return Task.FromResult(result);
        }
    }
}
