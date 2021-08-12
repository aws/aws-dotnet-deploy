// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.CLI.ServerMode.Models
{
    public class SystemCapabilitySummary
    {
        public string Name { get; set; }
        public bool Installed { get; set; }
        public bool Available { get; set; }
        public string? Message { get; set; }
        public string? InstallationUrl { get; set; }

        public SystemCapabilitySummary(string name, bool installed, bool available)
        {
            Name = name;
            Installed = installed;
            Available = available;
        }
    }
}
