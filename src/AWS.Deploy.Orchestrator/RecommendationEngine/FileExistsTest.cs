// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;

namespace AWS.Deploy.Orchestrator.RecommendationEngine
{
    /// <summary>
    /// This test checks to see if a file exists within the project directory.
    /// </summary>
    public class FileExistsTest : BaseRecommendationTest
    {
        public override string Name => "FileExists";

        public override Task<bool> Execute(RecommendationTestInput input)
        {
            var directory = Path.GetDirectoryName(input.ProjectDefinition.ProjectPath);
            var result = (Directory.GetFiles(directory, input.Test.Condition.FileName).Length == 1);
            return Task.FromResult(result);
        }
    }
}
