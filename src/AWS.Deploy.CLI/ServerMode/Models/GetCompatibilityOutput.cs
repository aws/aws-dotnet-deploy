// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace AWS.Deploy.CLI.ServerMode.Models
{
    public class GetCompatibilityOutput
    {
        public IEnumerable<SystemCapabilitySummary> Capabilities { get; set; } = new List<SystemCapabilitySummary>();
    }
}
