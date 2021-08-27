// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Linq;

namespace AWS.Deploy.Common.Utilities
{
    public class PathUtilities
    {
        public static bool IsPathValid(string path)
        {
            path = path.Trim();

            if (string.IsNullOrEmpty(path))
                return false;

            if (path.StartsWith(@"\\"))
                return false;

            if (path.Contains("&"))
                return false;

            if (Path.GetInvalidPathChars().Any(x => path.Contains(x)))
                return false;

            return true;
        }
    }
}
