// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Threading.Tasks;
using AWS.Deploy.Orchestrator.CDK;
using Xunit;

namespace AWS.Deploy.Orchestrator.UnitTest.CDK
{
    public class NodeInitializerTests
    {
        private readonly TestCommandLineWrapperImpl _testCommandLineWrapper;
        private readonly INodeInitializer _nodeInitializer;
        private readonly TestFileManagerImpl _fileManager;
        private const string _workingDirectory = @"c:\fake\path";

        private const string _packageJsonContent = @"
                {{
                    ""devDependencies"": {{
                        ""aws-cdk"": ""1.0.1""
                    }},
                    ""scripts"": {{
                    ""cdk"": ""cdk""
                }}
            }}";

        private const string _packageJsonTemplate =
            @"
                {{
                    ""devDependencies"": {{
                        ""aws-cdk"": ""{aws-cdk-version}""
                    }},
                    ""scripts"": {{
                    ""cdk"": ""cdk""
                }}
            }}";

        private const string _packageJsonFileName = "package.json";

        public NodeInitializerTests()
        {
            _fileManager = new TestFileManagerImpl();
            _testCommandLineWrapper = new TestCommandLineWrapperImpl();
            var templateManager = new TemplateWriter(_packageJsonFileName, _fileManager);
            _nodeInitializer = new NodeInitializer(_testCommandLineWrapper, templateManager, _fileManager);
        }

        [Fact]
        public async Task IsInitialized_PackagesJsonExists()
        {
            await _fileManager.WriteAllTextAsync(Path.Combine(_workingDirectory, _packageJsonFileName), _packageJsonContent);

            Assert.True(_nodeInitializer.IsInitialized(_workingDirectory));
        }

        [Fact]
        public void IsInitialized_PackagesJsonDoesNotExist()
        {
            Assert.False(_nodeInitializer.IsInitialized(_workingDirectory));
        }

        [Fact]
        public async Task Initialize()
        {
            // Setup template file
            _fileManager.InMemoryStore[_packageJsonFileName] = _packageJsonTemplate;

            // Initialize node app
            await _nodeInitializer.Initialize(_workingDirectory, Version.Parse("1.0.1"));

            // Verify initialized package.json
            var actualPackageJsonContent = await _fileManager.ReadAllTextAsync(Path.Combine(_workingDirectory, _packageJsonFileName));
            Assert.Equal(_packageJsonContent, actualPackageJsonContent);

            Assert.Contains(("npm install", _workingDirectory, false), _testCommandLineWrapper.Commands);
        }
    }
}
