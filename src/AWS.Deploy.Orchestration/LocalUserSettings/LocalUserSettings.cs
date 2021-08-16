// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace AWS.Deploy.Orchestration.LocalUserSettings
{
    public class LocalUserSettings
    {
        public List<LastDeployedStack>? LastDeployedStacks { get; set; }

        public LocalUserSettings(List<LastDeployedStack> lastDeployedStacks)
        {
            LastDeployedStacks = lastDeployedStacks;
        }
    }
}
