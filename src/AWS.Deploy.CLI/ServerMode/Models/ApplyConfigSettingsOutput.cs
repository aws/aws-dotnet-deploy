// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace AWS.Deploy.CLI.ServerMode.Models
{
    public class ApplyConfigSettingsOutput
    {
        public IDictionary<string, string> FailedConfigUpdates { get; set; } = new Dictionary<string, string>();
    }
}
