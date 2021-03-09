// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using AWS.Deploy.Common.IO;

namespace AWS.Deploy.CLI.UnitTests.Utilities
{
    public class TestZipFileManager : IZipFileManager
    {
        public readonly List<string> CreatedZipFiles = new List<string>();

        public void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName)
        {
            CreatedZipFiles.Add(destinationArchiveFileName);
        }
    }
}
