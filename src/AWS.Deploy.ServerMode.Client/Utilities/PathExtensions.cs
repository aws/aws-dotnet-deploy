// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Linq;

namespace AWS.Deploy.ServerMode.Client.Utilities
{
    public class PathUtilities
    {
        public static bool IsDeployToolPathValid(string deployToolPath)
        {
            deployToolPath = deployToolPath.Trim();

            if (string.IsNullOrEmpty(deployToolPath))
                return false;

            if (deployToolPath.StartsWith(@"\\"))
                return false;

            if (deployToolPath.Contains("&"))
                return false;

            if (Path.GetInvalidPathChars().Any(x => deployToolPath.Contains(x)))
                return false;

            if (!File.Exists(deployToolPath))
                return false;

            return true;
        }
    }
}
