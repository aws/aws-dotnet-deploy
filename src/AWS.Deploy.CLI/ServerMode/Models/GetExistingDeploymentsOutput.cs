// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AWS.Deploy.CLI.ServerMode.Models
{
    public class GetExistingDeploymentsOutput
    {
        public IList<ExistingDeploymentSummary> ExistingDeployments { get; set; } = new List<ExistingDeploymentSummary>();
    }
}
