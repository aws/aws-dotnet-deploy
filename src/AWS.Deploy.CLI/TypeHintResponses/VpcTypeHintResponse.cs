// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using AWS.Deploy.Common.Recipes;

namespace AWS.Deploy.CLI.TypeHintResponses
{
    /// <summary>
    /// <see cref="OptionSettingTypeHint.Vpc"/> type hint response
    /// </summary>
    public class VpcTypeHintResponse : IDisplayable
    {
        /// <summary>
        /// Indicates if the user has selected to use the default vpc.   Note: It's valid
        /// for this to be true without looking up an setting <see cref="VpcId"/>
        /// </summary>
        public bool IsDefault {get; set; }
        public bool CreateNew { get;set; }
        public string VpcId { get; set; }

        /// <summary>
        /// The IDs of the subnets that will be associated with the Fargate service.
        /// </summary>
        public SortedSet<string> Subnets { get; set; } = new SortedSet<string>();

        public VpcTypeHintResponse(
            bool isDefault,
            bool createNew,
            string vpcId)
        {
            IsDefault = isDefault;
            CreateNew = createNew;
            VpcId = vpcId;
        }

        /// <summary>
        /// Returns null to use to the CLI's default display, which will handle child options
        /// </summary>
        public string? ToDisplayString() => null;
    }
}
