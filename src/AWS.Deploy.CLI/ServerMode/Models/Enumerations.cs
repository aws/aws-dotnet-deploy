// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.Deploy.CLI.ServerMode.Models
{
    public enum SystemStatus { Ready = 1, Error = 2 };

    public enum DeploymentStatus { NotStarted = 1, Executing = 2, Error = 3, Success = 4}
}
