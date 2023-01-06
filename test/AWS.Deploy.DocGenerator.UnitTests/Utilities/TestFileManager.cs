// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Deploy.Common.IO;

namespace AWS.Deploy.DocGenerator.UnitTests.Utilities
{
    public class TestFileManager : IFileManager
    {
        public readonly Dictionary<string, string> InMemoryStore = new Dictionary<string, string>();

        public bool Exists(string path) => throw new NotImplementedException();
        public bool Exists(string path, string directory) => throw new NotImplementedException();
        public string GetExtension(string filePath) => throw new NotImplementedException();
        public long GetSizeInBytes(string filePath) => throw new NotImplementedException();
        public bool IsFileValidPath(string filePath) => throw new NotImplementedException();
        public FileStream OpenRead(string filePath) => throw new NotImplementedException();
        public Task<string[]> ReadAllLinesAsync(string path) => throw new NotImplementedException();
        public Task<string> ReadAllTextAsync(string path) => throw new NotImplementedException();

        public Task WriteAllTextAsync(string filePath, string contents, CancellationToken cancellationToken = default)
        {
            InMemoryStore[filePath] = contents;
            return Task.CompletedTask;
        }
    }
}
