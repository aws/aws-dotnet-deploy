// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Threading.Tasks;

namespace AWS.Deploy.Orchestration.RecommendationEngine
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

            if (directory == null ||
                input.Test.Condition.FileName == null)
                return Task.FromResult(false);

            var result = (Directory.GetFiles(directory, input.Test.Condition.FileName).Length == 1);
            return Task.FromResult(result);
        }
    }
}
