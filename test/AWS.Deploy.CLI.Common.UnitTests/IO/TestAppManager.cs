// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AWS.Deploy.CLI.Common.UnitTests.Extensions;

namespace AWS.Deploy.CLI.Common.UnitTests.IO
{
    public class TestAppManager
    {
        public string GetProjectPath(string path)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var sourceTestAppsDir = new DirectoryInfo("testapps");
            var tempTestAppsPath = Path.Combine(tempDir, "testapps");
            Directory.CreateDirectory(tempTestAppsPath);
            sourceTestAppsDir.CopyTo(tempTestAppsPath, true);
            return Path.Combine(tempDir, path);
        }
    }
}
