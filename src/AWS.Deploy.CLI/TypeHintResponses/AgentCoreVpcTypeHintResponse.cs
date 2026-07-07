// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace AWS.Deploy.CLI.TypeHintResponses
{
    public class AgentCoreVpcTypeHintResponse : IDisplayable
    {
        public bool UseVPC { get; set; }
        public bool CreateNew { get; set; }
        public string? VpcId { get; set; }
        public SortedSet<string> SecurityGroups { get; set; } = new SortedSet<string>();

        public string? ToDisplayString()
        {
            if (!UseVPC)
                return "*** Not using VPC ***";
            if (CreateNew)
                return "*** Create new VPC ***";
            return string.IsNullOrEmpty(VpcId) ? "*** Not using VPC ***" : VpcId;
        }
    }
}
