// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using AWS.Deploy.Common.IO;

namespace AWS.Deploy.CLI.Common.UnitTests.IO
{
    public class TestDirectoryManager : IDirectoryManager
    {
        public readonly HashSet<string> CreatedDirectories = new();

        public DirectoryInfo CreateDirectory(string path)
        {
            CreatedDirectories.Add(path);
            return new DirectoryInfo(path);
        }

        public bool Exists(string path)
        {
            return CreatedDirectories.Contains(path);
        }

        public string[] GetFiles(string projectPath, string searchPattern = null) =>
            throw new NotImplementedException("If your test needs this method, you'll need to implement this.");
    }
}
