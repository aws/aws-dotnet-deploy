// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using AWS.Deploy.Common.Recipes;

namespace AWS.Deploy.CLI.TypeHintResponses
{
    /// <summary>
    /// The <see cref="VPCConnectorTypeHintResponse"/> class encapsulates
    /// <see cref="OptionSettingTypeHint.VPCConnector"/> type hint response
    /// </summary>
    public class VPCConnectorTypeHintResponse : IDisplayable
    {
        public string? VpcConnectorId { get; set; }
        public bool UseVPCConnector { get; set; }
        public bool CreateNew { get; set; }
        public string? VpcId { get; set; }
        public SortedSet<string> Subnets { get; set; } = new SortedSet<string>();
        public SortedSet<string> SecurityGroups { get; set; } = new SortedSet<string>();

        /// <summary>
        /// Returning null will default to the tool's default display.
        /// </summary>
        public string? ToDisplayString() => null;
    }
}
