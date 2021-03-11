// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Threading.Tasks;
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.Orchestrator.CDK;
using Xunit;

namespace AWS.Deploy.Orchestrator.UnitTests.CDK
{
    public class NPMPackageInitializerTests
    {
        private readonly TestCommandLineWrapper _testCommandLineWrapper;
        private readonly INPMPackageInitializer _npmPackageInitializer;
        private readonly TestFileManager _fileManager;
        private readonly TestDirectoryManager _directoryManager;
        private const string _workingDirectory = @"c:\fake\path";

        private const string _packageJsonContent =
            @"
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

        public NPMPackageInitializerTests()
        {
            _fileManager = new TestFileManager();
            _directoryManager = new TestDirectoryManager();
            _testCommandLineWrapper = new TestCommandLineWrapper();
            var packageJsonGenerator = new PackageJsonGenerator(_packageJsonTemplate);
            _npmPackageInitializer = new NPMPackageInitializer(_testCommandLineWrapper, packageJsonGenerator, _fileManager, _directoryManager);
        }

        [Fact]
        public async Task IsInitialized_PackagesJsonExists()
        {
            // Arrange: create a fake package.json file
            await _fileManager.WriteAllTextAsync(Path.Combine(_workingDirectory, _packageJsonFileName), _packageJsonContent);

            // Act
            var isInitialized = _npmPackageInitializer.IsInitialized(_workingDirectory);

            // Assert
            Assert.True(isInitialized);
        }

        [Fact]
        public void IsInitialized_PackagesJsonDoesNotExist()
        {
            Assert.False(_npmPackageInitializer.IsInitialized(_workingDirectory));
        }

        [Fact]
        public async Task Initialize_PackageJsonDoesNotExist()
        {
            // Arrange: Setup template file
            _fileManager.InMemoryStore[_packageJsonFileName] = _packageJsonTemplate;

            // Act: Initialize node app
            await _npmPackageInitializer.Initialize(_workingDirectory, Version.Parse("1.0.1"));

            // Assert: verify initialized package.json
            var actualPackageJsonContent = await _fileManager.ReadAllTextAsync(Path.Combine(_workingDirectory, _packageJsonFileName));
            Assert.Equal(_packageJsonContent, actualPackageJsonContent);
            Assert.Contains(("npm install", _workingDirectory, false), _testCommandLineWrapper.Commands);
            Assert.True(_directoryManager.Exists(_workingDirectory));
        }
    }
}
