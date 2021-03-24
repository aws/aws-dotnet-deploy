// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AWS.Deploy.Common.IO;

namespace AWS.Deploy.Orchestration.UnitTests
{
    public class TestFileManager : IFileManager
    {
        public readonly Dictionary<string, string> InMemoryStore = new Dictionary<string, string>();

        public bool Exists(string path)
        {
            return InMemoryStore.ContainsKey(path);
        }

        public Task<string> ReadAllTextAsync(string path)
        {
            var text = InMemoryStore[path];
            return Task.FromResult(text);
        }

        public async Task<string[]> ReadAllLinesAsync(string path)
        {
            return (await ReadAllTextAsync(path)).Split(Environment.NewLine);
        }

        public Task WriteAllTextAsync(string filePath, string contents, CancellationToken cancellationToken = default)
        {
            InMemoryStore[filePath] = contents;
            return Task.CompletedTask;
        }
    }
}
