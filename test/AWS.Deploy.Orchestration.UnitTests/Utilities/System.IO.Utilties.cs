// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Reflection;

namespace AWS.Deploy.Orchestration.UnitTests.Utilities
{
    internal static class SystemIOUtilities
    {
        public static string ResolvePath(string projectName)
        {
            var testsPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            while (testsPath != null && !string.Equals(new DirectoryInfo(testsPath).Name, "test", StringComparison.OrdinalIgnoreCase))
            {
                testsPath = Directory.GetParent(testsPath)!.FullName;
            }

            return Path.Combine(testsPath!, "..", "testapps", projectName);
        }

    }
}
