// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Threading.Tasks;
using AWS.Deploy.Orchestration.Utilities;

namespace AWS.Deploy.CLI.UnitTests.Utilities
{
    public class TestZipFileManager : IZipFileManager
    {
        public readonly List<string> CreatedZipFiles = new List<string>();

        public Task CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName)
        {
            CreatedZipFiles.Add(destinationArchiveFileName);
            return Task.CompletedTask;
        }
    }
}
