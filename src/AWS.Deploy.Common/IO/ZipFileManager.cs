// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.IO.Compression;

namespace AWS.Deploy.Common.IO
{
    public interface IZipFileManager
    {
        void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName);
    }

    public class ZipFileManager : IZipFileManager
    {
        public void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName)
        {
            ZipFile.CreateFromDirectory(sourceDirectoryName, destinationArchiveFileName);
        }
    }
}
