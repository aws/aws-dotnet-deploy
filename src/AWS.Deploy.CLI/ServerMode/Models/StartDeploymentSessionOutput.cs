// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.Deploy.CLI.ServerMode.Models
{
    public class StartDeploymentSessionOutput
    {
        public string SessionId { get; set; }

        public string? DefaultDeploymentName { get; set; }

        public StartDeploymentSessionOutput(string sessionId)
        {
            SessionId = sessionId;
        }
    }
}
