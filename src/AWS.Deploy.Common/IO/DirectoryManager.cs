// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;

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
    }

    public class DirectoryManager : IDirectoryManager
    {
        public DirectoryInfo CreateDirectory(string path) => Directory.CreateDirectory(path);
            
        public DirectoryInfo GetDirectoryInfo(string path) => new DirectoryInfo(path);

        public bool Exists(string path) => Directory.Exists(path);

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
    }
}
