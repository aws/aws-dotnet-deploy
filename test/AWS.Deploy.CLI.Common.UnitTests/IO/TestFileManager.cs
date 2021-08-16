// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AWS.Deploy.Common.IO;

namespace AWS.Deploy.CLI.Common.UnitTests.IO
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

    public static class TestFileManagerExtensions
    {
        /// <summary>
        /// Adds a virtual csproj file with valid xml contents
        /// </summary>
        /// <returns>
        /// Returns the correct full path for <paramref name="relativePath"/>
        /// </returns>
        public static string AddEmptyProjectFile(this TestFileManager fileManager, string relativePath)
        {
            relativePath = relativePath.Replace('\\', Path.DirectorySeparatorChar);
            var fullPath = Path.Join(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "c:\\" : "/", relativePath);
            fileManager.InMemoryStore.Add(fullPath, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");

            return fullPath;
        }
    }
}
