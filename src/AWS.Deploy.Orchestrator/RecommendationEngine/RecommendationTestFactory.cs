// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AWS.Deploy.Orchestrator.RecommendationEngine
{
    public class RecommendationTestFactory
    {
        public static IDictionary<string, BaseRecommendationTest> LoadAvailableTests()
        {
            return
                 typeof(BaseRecommendationTest)
                 .Assembly
                 .GetTypes()
                 .Where(x => !x.IsAbstract && x.IsSubclassOf(typeof(BaseRecommendationTest)))
                 .Select(x => Activator.CreateInstance(x) as BaseRecommendationTest)
                 .ToDictionary(x => x.Name);
        }
    }
}
