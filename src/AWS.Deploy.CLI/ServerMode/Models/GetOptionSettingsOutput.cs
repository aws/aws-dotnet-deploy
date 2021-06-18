// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace AWS.Deploy.CLI.ServerMode.Models
{
    public class GetOptionSettingsOutput
    {
        public IList<OptionSettingItemSummary> OptionSettings { get; set; } = new List<OptionSettingItemSummary>();
    }
}
