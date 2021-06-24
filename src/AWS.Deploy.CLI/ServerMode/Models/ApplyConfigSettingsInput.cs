// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace AWS.Deploy.CLI.ServerMode.Models
{
    public class ApplyConfigSettingsInput
    {
        public Dictionary<string, string> UpdatedSettings { get; set; } = new Dictionary<string, string>();
    }
}
