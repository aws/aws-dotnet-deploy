// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AWS.Deploy.Common.Utilities;

namespace AWS.Deploy.Common.IO
{
    public interface IFileManager
    {
        /// <summary>
        /// Determines whether the specified file is at a valid path and exists.
        /// This can either be an absolute path or relative to the current working directory.
        /// </summary>
        /// <param name="path">The file to check</param>
        /// <returns>
        /// True if the path is valid, the caller has the required permissions,
        /// and path contains the name of an existing file
        /// </returns>
        bool Exists(string path);

        /// <summary>
        /// Determines whether the specified file is at a valid path and exists.
        /// This can either be an absolute path or relative to the given directory.
        /// </summary>
        /// <param name="path">The file to check</param>
        /// <param name="directory">Directory to consider the path as relative to</param>
        /// <returns>
        /// True if the path is valid, the caller has the required permissions,
        /// and path contains the name of an existing file
        /// </returns>
        bool Exists(string path, string directory);

        /// <summary>
        /// Determines that the specified file path is structurally valid and its parent directory exists on disk.
        /// This file path can be absolute or relative to the current working directory.
        /// Note - This method does not check for the existence of a file at the specified path. Use <see cref="Exists(string)"/> or <see cref="Exists(string, string)"/> to check for existence of a file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        bool IsFileValidPath(string filePath);

        Task<string> ReadAllTextAsync(string path);
        Task<string[]> ReadAllLinesAsync(string path);
        Task WriteAllTextAsync(string filePath, string contents, CancellationToken cancellationToken = default);
        FileStream OpenRead(string filePath);
        string GetExtension(string filePath);
        long GetSizeInBytes(string filePath);
    }

    /// <summary>
    /// Wrapper for <see cref="File"/> class to allow mock-able behavior for static methods.
    /// </summary>
    public class FileManager : IFileManager
    {
        public bool Exists(string path)
        {
            if (!PathUtilities.IsPathValid(path))
                return false;

            return File.Exists(path);
        }

        public bool Exists(string path, string directory)
        {
            if (Path.IsPathRooted(path))
            {
                return Exists(path);
            }
            else
            {
                return Exists(Path.Combine(directory, path));
            }
        }

        public Task<string> ReadAllTextAsync(string path) => File.ReadAllTextAsync(path);

        public Task<string[]> ReadAllLinesAsync(string path) => File.ReadAllLinesAsync(path);

        public Task WriteAllTextAsync(string filePath, string contents, CancellationToken cancellationToken) =>
            File.WriteAllTextAsync(filePath, contents, cancellationToken);

        public FileStream OpenRead(string filePath) => File.OpenRead(filePath);

        public string GetExtension(string filePath) => Path.GetExtension(filePath);

        public long GetSizeInBytes(string filePath) => new FileInfo(filePath).Length;

        public bool IsFileValidPath(string filePath)
        {
            if (!PathUtilities.IsPathValid(filePath))
                return false;

            var parentDirectory = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(parentDirectory))
            {
                return false;
            }
            return PathUtilities.IsPathValid(parentDirectory) && Directory.Exists(parentDirectory);
        }
    }
}
