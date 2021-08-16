// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.IO;
using System.Linq;
using AWS.Deploy.Orchestration.CDK;
using Xunit;

namespace AWS.Deploy.Orchestration.UnitTests.CDK
{
    public class CDKVersionDetectorTests
    {
        private readonly ICDKVersionDetector _cdkVersionDetector;

        public CDKVersionDetectorTests()
        {
            _cdkVersionDetector = new CDKVersionDetector();
        }

        [Theory]
        [InlineData("MixedReferences.csproj", "1.109.0")]
        [InlineData("SameReferences.csproj", "1.108.0")]
        [InlineData("NoReferences.csproj", "1.107.0")]
        public void Detect_CSProjectPath(string fileName, string expectedVersion)
        {
            var csprojPath = Path.Combine("CDK", "CSProjFiles", fileName);
            var version = _cdkVersionDetector.Detect(csprojPath);
            Assert.Equal(expectedVersion, version.ToString());
        }

        [Fact]
        public void Detect_CSProjectPaths()
        {
            var csprojPaths = new []
            {
                "MixedReferences.csproj",
                "SameReferences.csproj",
                "NoReferences.csproj"
            }.Select(fileName => Path.Combine("CDK", "CSProjFiles", fileName));
            var version = _cdkVersionDetector.Detect(csprojPaths);
            Assert.Equal("1.109.0", version.ToString());
        }
    }
}
