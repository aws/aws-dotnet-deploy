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

        /// <summary>
        /// Determines whether the given path refers to an existing directory on disk.
        /// This can either be an absolute path or relative to the current working directory.
        /// </summary>
        /// <param name="path">The path to test</param>
        /// <returns>
        /// true if path refers to an existing directory;
        /// false if the directory does not exist or an error occurs when trying to determine if the specified directory exists
        /// </returns>
        bool Exists(string path);

        /// <summary>
        /// Determines whether the given path refers to an existing directory on disk.
        /// This can either be an absolute path or relative to the given directory.
        /// </summary>
        /// <param name="path">The path to test</param>
        /// <param name="relativeTo">Directory to consider the path as relative to</param>
        /// <returns>
        /// true if path refers to an existing directory;
        /// false if the directory does not exist or an error occurs when trying to determine if the specified directory exists
        /// </returns>
        bool Exists(string path, string relativeTo);

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
            ".csproj",
            ".fsproj"
        };

        public DirectoryInfo CreateDirectory(string path) => Directory.CreateDirectory(path);

        public DirectoryInfo GetDirectoryInfo(string path) => new DirectoryInfo(path);

        public bool Exists(string path) => IsDirectoryValid(path);

        public bool Exists(string path, string relativeTo)
        {
            if (Path.IsPathRooted(path))
            {
                return Exists(path);
            }
            else
            {
                return Exists(Path.Combine(relativeTo, path));
            }
        }

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
