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
        bool Exists(string path);
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
        public bool Exists(string path) => IsFileValid(path);

        public Task<string> ReadAllTextAsync(string path) => File.ReadAllTextAsync(path);

        public Task<string[]> ReadAllLinesAsync(string path) => File.ReadAllLinesAsync(path);

        public Task WriteAllTextAsync(string filePath, string contents, CancellationToken cancellationToken) =>
            File.WriteAllTextAsync(filePath, contents, cancellationToken);

        public FileStream OpenRead(string filePath) => File.OpenRead(filePath);

        public string GetExtension(string filePath) => Path.GetExtension(filePath);

        public long GetSizeInBytes(string filePath) => new FileInfo(filePath).Length;

        private bool IsFileValid(string filePath)
        {
            if (!PathUtilities.IsPathValid(filePath))
                return false;

            if (!File.Exists(filePath))
                return false;

            return true;
        }
    }
}
