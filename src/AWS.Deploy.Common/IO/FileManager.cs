// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AWS.Deploy.Common.IO
{
    public interface IFileManager
    {
        bool Exists(string path);
        Task<string> ReadAllTextAsync(string path);
        Task<string[]> ReadAllLinesAsync(string path);
        Task WriteAllTextAsync(string filePath, string contents, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Wrapper for <see cref="File"/> class to allow mock-able behavior for static methods.
    /// </summary>
    public class FileManager : IFileManager
    {
        public bool Exists(string path) => File.Exists(path);

        public Task<string> ReadAllTextAsync(string path) => File.ReadAllTextAsync(path);

        public Task<string[]> ReadAllLinesAsync(string path) => File.ReadAllLinesAsync(path);

        public Task WriteAllTextAsync(string filePath, string contents, CancellationToken cancellationToken) =>
            File.WriteAllTextAsync(filePath, contents, cancellationToken);
    }
}
