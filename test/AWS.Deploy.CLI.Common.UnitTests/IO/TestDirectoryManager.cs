// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using AWS.Deploy.Common.IO;
using System.Linq;

namespace AWS.Deploy.CLI.Common.UnitTests.IO
{
    public class TestDirectoryManager : IDirectoryManager
    {
        public readonly HashSet<string> CreatedDirectories = new();
        public readonly Dictionary<string, HashSet<string>> AddedFiles = new();

        public DirectoryInfo CreateDirectory(string path)
        {
            CreatedDirectories.Add(path);
            return new DirectoryInfo(path);
        }

        public DirectoryInfo GetDirectoryInfo(string path) => new DirectoryInfo(path);

        public string GetRelativePath(string referenceFullPath, string targetFullPath) => Path.GetRelativePath(referenceFullPath, targetFullPath);

        public string GetAbsolutePath(string referenceFullPath, string targetRelativePath) => Path.GetFullPath(targetRelativePath, referenceFullPath);
        public string[] GetProjFiles(string path) =>
            throw new NotImplementedException("If your test needs this method, you'll need to implement this.");

        public void Delete(string path, bool recursive = false) =>
            throw new NotImplementedException("If your test needs this method, you'll need to implement this.");

        public bool ExistsInsideDirectory(string parentDirectoryPath, string childPath) => childPath.Contains(parentDirectoryPath + Path.DirectorySeparatorChar, StringComparison.InvariantCulture);

        public bool Exists(string path)
        {
            return CreatedDirectories.Contains(path);
        }

        public bool Exists(string path, string relativeTo) =>
            throw new NotImplementedException("If your test needs this method, you'll need to implement this.");

        public string[] GetDirectories(string path, string? searchPattern = null, SearchOption searchOption = SearchOption.TopDirectoryOnly) =>
            CreatedDirectories.ToArray();

        public string[] GetFiles(string path, string? searchPattern = null, SearchOption searchOption = SearchOption.TopDirectoryOnly) =>
            AddedFiles.ContainsKey(path) ? AddedFiles[path].ToArray() : new string[0];

        public bool IsEmpty(string path) =>
            throw new NotImplementedException("If your test needs this method, you'll need to implement this.");
    }
}
