// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;

namespace AWS.Deploy.CLI.IntegrationTests.Helpers
{
    public class ConfigFileHelper
    {
        public static void ReplacePlaceholders(string configFilePath)
        {
            var suffix = Guid.NewGuid().ToString().Split('-').Last();
            var json = File.ReadAllText(configFilePath);
            json = json.Replace("{Suffix}", suffix);
            File.WriteAllText(configFilePath, json);
        }
    }
}
