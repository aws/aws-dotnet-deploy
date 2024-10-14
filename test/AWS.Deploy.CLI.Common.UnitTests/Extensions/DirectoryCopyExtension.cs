// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.IO;

namespace AWS.Deploy.CLI.Common.UnitTests.Extensions
{
    public static class DirectoryCopyExtension
    {
        /// <summary>
        /// Copy the contents of a directory to another location including all subdirectories.
        /// </summary>
        public static void CopyTo(this DirectoryInfo dir, string destDirName)
        {
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: {dir.FullName}");
            }

            Directory.CreateDirectory(destDirName);

            var files = dir.GetFiles("*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(dir.FullName, file.FullName);
                var tempPath = Path.Combine(destDirName, relativePath);
                var tempDir = Path.GetDirectoryName(tempPath);
                if (!string.IsNullOrEmpty(tempDir) && !Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }
                file.CopyTo(tempPath, false);
            }
        }
    }
}
