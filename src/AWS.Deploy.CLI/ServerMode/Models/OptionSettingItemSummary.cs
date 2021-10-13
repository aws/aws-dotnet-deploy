// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;

namespace AWS.Deploy.CLI.ServerMode.Models
{
    public class OptionSettingItemSummary
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public object? Value { get; set; }

        public string Type { get; set; }

        public string? TypeHint { get; set; }

        public bool Advanced { get; set; }

        public bool ReadOnly { get; set; }

        public bool Visible { get; set; }

        public bool SummaryDisplayable { get; set; }

        public IList<string> AllowedValues { get; set; } = new List<string>();

        public IDictionary<string, string> ValueMapping { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public List<OptionSettingItemSummary> ChildOptionSettings { get; set; } = new();

        public OptionSettingItemSummary(string id, string name, string description, string type)
        {
            Id = id;
            Name = name;
            Description = description;
            Type = type;
        }
    }
}
