// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.IO;

namespace AWS.Deploy.Common.IO
{
    public interface IDirectoryManager
    {
        DirectoryInfo CreateDirectory(string path);
        bool Exists(string path);
    }

    public class DirectoryManager : IDirectoryManager
    {
        public DirectoryInfo CreateDirectory(string path)
        {
            return Directory.CreateDirectory(path);
        }

        public bool Exists(string path)
        {
            return Directory.Exists(path);
        }
    }
}
