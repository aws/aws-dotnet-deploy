// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AWS.Deploy.Common.IO
{
    public interface IFileManager
    {
        bool Exists(string path);
        Task<string> ReadAllTextAsync(string path);
        Task WriteAllTextAsync(string filePath, string contents, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Wrapper for <see cref="File"/> class to allow mock-able behavior for static methods.
    /// </summary>
    public class FileManager : IFileManager
    {
        public bool Exists(string path)
        {
            return File.Exists(path);
        }

        public Task<string> ReadAllTextAsync(string path)
        {
            return File.ReadAllTextAsync(path);
        }

        public Task WriteAllTextAsync(string filePath, string contents, CancellationToken cancellationToken)
        {
            return File.WriteAllTextAsync(filePath, contents, cancellationToken);
        }
    }
}
