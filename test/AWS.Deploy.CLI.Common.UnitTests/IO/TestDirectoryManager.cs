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

        public void Delete(string path, bool recursive = false) =>
            throw new NotImplementedException("If your test needs this method, you'll need to implement this.");

        public bool ExistsInsideDirectory(string parentDirectoryPath, string childPath) =>
            throw new NotImplementedException("If your test needs this method, you'll need to implement this.");

        public bool Exists(string path)
        {
            return CreatedDirectories.Contains(path);
        }

        public string[] GetDirectories(string path, string searchPattern = null, SearchOption searchOption = SearchOption.TopDirectoryOnly) =>
            throw new NotImplementedException("If your test needs this method, you'll need to implement this.");

        public DirectoryInfo GetDirectoryInfo(string path) =>
            throw new NotImplementedException("If your test needs this method, you'll need to implement this.");

        public string[] GetFiles(string path, string searchPattern = null, SearchOption searchOption = SearchOption.TopDirectoryOnly) =>
            throw new NotImplementedException("If your test needs this method, you'll need to implement this.");

        public bool IsEmpty(string path) =>
            throw new NotImplementedException("If your test needs this method, you'll need to implement this.");

        public string GetRelativePath(string referenceFullPath, string targetFullPath) =>
            throw new NotImplementedException("If your test needs this method, you'll need to implement this.");
    }
}
