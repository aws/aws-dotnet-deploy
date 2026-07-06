// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.CLI.TypeHintResponses
{
    public class AgentCoreVpcTypeHintResponse : IDisplayable
    {
        public bool UseVPC { get; set; }
        public bool CreateNew { get; set; }
        public string? VpcId { get; set; }

        public string? ToDisplayString()
        {
            if (!UseVPC)
                return "*** Not using VPC ***";
            if (CreateNew)
                return "*** Create new VPC ***";
            return VpcId ?? "*** Not using VPC ***";
        }
    }
}
