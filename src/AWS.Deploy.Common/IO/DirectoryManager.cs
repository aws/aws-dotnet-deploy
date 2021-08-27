// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AWS.Deploy.Common.Utilities;

namespace AWS.Deploy.Common.IO
{
    public interface IDirectoryManager
    {
        DirectoryInfo CreateDirectory(string path);
        DirectoryInfo GetDirectoryInfo(string path);
        bool Exists(string path);
        string[] GetFiles(string path, string? searchPattern = null, SearchOption searchOption = SearchOption.TopDirectoryOnly);
        string[] GetDirectories(string path, string? searchPattern = null, SearchOption searchOption = SearchOption.TopDirectoryOnly);
        bool IsEmpty(string path);
        bool ExistsInsideDirectory(string parentDirectoryPath, string childPath);
        void Delete(string path, bool recursive = false);
        string GetRelativePath(string referenceFullPath, string targetFullPath);
        string GetAbsolutePath(string referenceFullPath, string targetRelativePath);
        public string[] GetProjFiles(string path);
    }

    public class DirectoryManager : IDirectoryManager
    {
        private readonly HashSet<string> _projFileExtensions = new()
        {
            "csproj",
            "fsproj"
        };

        public DirectoryInfo CreateDirectory(string path) => Directory.CreateDirectory(path);

        public DirectoryInfo GetDirectoryInfo(string path) => new DirectoryInfo(path);

        public bool Exists(string path) => IsDirectoryValid(path);

        public string[] GetFiles(string path, string? searchPattern = null, SearchOption searchOption = SearchOption.TopDirectoryOnly)
            => Directory.GetFiles(path, searchPattern ?? "*", searchOption);

        public string[] GetDirectories(string path, string? searchPattern = null, SearchOption searchOption = SearchOption.TopDirectoryOnly)
            => Directory.GetDirectories(path, searchPattern ?? "*", searchOption);

        public bool IsEmpty(string path) => GetFiles(path).Length == 0 && GetDirectories(path).Length == 0;

        public bool ExistsInsideDirectory(string parentDirectoryPath, string childPath)
        {
            var parentDirectoryFullPath = GetDirectoryInfo(parentDirectoryPath).FullName;
            var childFullPath = GetDirectoryInfo(childPath).FullName;
            return childFullPath.Contains(parentDirectoryFullPath + Path.DirectorySeparatorChar, StringComparison.InvariantCulture);
        }

        public void Delete(string path, bool recursive = false) => Directory.Delete(path, recursive);

        public string GetRelativePath(string referenceFullPath, string targetFullPath) => Path.GetRelativePath(referenceFullPath, targetFullPath);
        public string GetAbsolutePath(string referenceFullPath, string targetRelativePath) => Path.GetFullPath(targetRelativePath, referenceFullPath);

        public string[] GetProjFiles(string path)
        {
            return Directory.GetFiles(path).Where(filePath => _projFileExtensions.Contains(Path.GetExtension(filePath).ToLower())).ToArray();
        }

        private bool IsDirectoryValid(string directoryPath)
        {
            if (!PathUtilities.IsPathValid(directoryPath))
                return false;

            if (!Directory.Exists(directoryPath))
                return false;

            return true;
        }
    }
}
