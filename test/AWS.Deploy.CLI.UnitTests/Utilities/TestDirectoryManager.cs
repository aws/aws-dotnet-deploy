// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.IO;
using AWS.Deploy.Common.IO;

namespace AWS.Deploy.CLI.UnitTests.Utilities
{
    public class TestDirectoryManager : IDirectoryManager
    {
        public readonly List<string> CreatedDirectories = new List<string>();

        public DirectoryInfo CreateDirectory(string path)
        {
            CreatedDirectories.Add(path);
            return new DirectoryInfo(path);
        }
    }
}
