// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace AWS.Deploy.CLI.ServerMode.Models
{
    public class GetConfigSettingResourcesOutput
    {
        public List<TypeHintResourceSummary>? Resources { get; set; }
    }
}
