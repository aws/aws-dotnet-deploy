// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Threading.Tasks;
using AWS.Deploy.Common.IO;

namespace AWS.Deploy.Orchestrator.CDK
{
    public interface ITemplateWriter
    {
        Task Write(string filePath, Dictionary<string, string> replacementToken);
    }

    public class TemplateWriter : ITemplateWriter
    {
        private readonly string _templateFilePath;
        private readonly IFileManager _fileManager;

        public TemplateWriter(string templateFilePath, IFileManager fileManager)
        {
            _templateFilePath = templateFilePath;
            _fileManager = fileManager;
        }

        public async Task Write(string filePath, Dictionary<string, string> replacementToken)
        {
            var allText = await _fileManager.ReadAllTextAsync(_templateFilePath);

            foreach (var (key, value) in replacementToken)
            {
                allText = allText.Replace(key, value);
            }

            await _fileManager.WriteAllTextAsync(filePath, allText);
        }
    }
}
