// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using AWS.Deploy.Common.Recipes;

namespace AWS.Deploy.CLI.TypeHintResponses
{
    /// <summary>
    /// <see cref="OptionSettingTypeHint.Vpc"/> type hint response
    /// </summary>
    public class ElasticBeanstalkVpcTypeHintResponse : IDisplayable
    {
        public bool UseVPC { get; set; }
        public bool CreateNew { get; set; }
        public string? VpcId { get; set; }
        public SortedSet<string> Subnets { get; set; } = new SortedSet<string>();
        public SortedSet<string> SecurityGroups { get; set; } = new SortedSet<string>();

        public string? ToDisplayString() => null;
    }
}
