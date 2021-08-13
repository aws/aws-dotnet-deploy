// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.IO;

namespace AWS.Deploy.CLI.Common.UnitTests.Extensions
{
    public static class DirectoryCopyExtension
    {
        /// <summary>
        /// <see cref="https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories"/>
        /// </summary>
        public static void CopyTo(this DirectoryInfo dir, string destDirName, bool copySubDirs)
        {
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: {dir.FullName}");
            }

            var dirs = dir.GetDirectories();

            Directory.CreateDirectory(destDirName);

            var files = dir.GetFiles();
            foreach (var file in files)
            {
                var tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, false);
            }

            if (copySubDirs)
            {
                foreach (var subdir in dirs)
                {
                    var tempPath = Path.Combine(destDirName, subdir.Name);
                    var subDir = new DirectoryInfo(subdir.FullName);
                    subDir.CopyTo(tempPath, copySubDirs);
                }
            }
        }
    }
}
