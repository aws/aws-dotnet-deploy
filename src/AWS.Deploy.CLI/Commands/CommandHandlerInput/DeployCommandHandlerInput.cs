// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AWS.Deploy.CLI.Commands.CommandHandlerInput
{
    public class DeployCommandHandlerInput
    {
        public string? Profile { get; set; }
        public string? Region { get; set; }
        public string? ProjectPath { get; set; }
        public string? StackName { get; set; }
        public string? Apply { get; set; }
        public bool Diagnostics { get; set; }
        public bool Silent { get; set; }
        public string? DeploymentProject { get; set; }
    }
}
