using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.Deploy.Constants
{
    internal static class EC2
    {
        public const string FILTER_PLATFORM_WINDOWS = "windows";
        public const string FILTER_PLATFORM_LINUX = "linux";

        public const string FILTER_ARCHITECTURE_X86_64 = "x86_64";
        public const string FILTER_ARCHITECTURE_ARM64 = "arm64";
    }
}
