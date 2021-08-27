// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Reflection;

namespace AWS.Deploy.CLI.UnitTests.Utilities
{
    internal static class SystemIOUtilities
    {
        public static string ResolvePath(string projectName)
        {
            var testsPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            while (testsPath != null && !string.Equals(new DirectoryInfo(testsPath).Name, "test", StringComparison.OrdinalIgnoreCase))
            {
                testsPath = Directory.GetParent(testsPath).FullName;
            }

            return Path.Combine(testsPath, "..", "testapps", projectName);
        }

        public static string ResolvePathToSolution()
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            while (path != null && !string.Equals(new DirectoryInfo(path).Name, "aws-dotnet-deploy", StringComparison.OrdinalIgnoreCase))
            {
                path = Directory.GetParent(path).FullName;
            }

            return new DirectoryInfo(Path.Combine(path, "AWS.Deploy.sln")).FullName;
        }

    }
}
