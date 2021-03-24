// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.IO;

namespace AWS.Deploy.Common.IO
{
    public interface IDirectoryManager
    {
        DirectoryInfo CreateDirectory(string path);
        bool Exists(string path);
        string[] GetFiles(string projectPath, string searchPattern = null);
    }

    public class DirectoryManager : IDirectoryManager
    {
        public DirectoryInfo CreateDirectory(string path) => Directory.CreateDirectory(path);
        
        public bool Exists(string path) => Directory.Exists(path);

        public string[] GetFiles(string path, string searchPattern = null) => Directory.GetFiles(path, searchPattern ?? "*");
    }
}
