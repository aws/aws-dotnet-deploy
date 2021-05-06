// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.Deploy.CLI.ServerMode.Models
{
    public class SetDeploymentTargetInput
    {
        public string NewDeploymentName { get; set; }

        public string NewDeploymentRecipeId { get; set; }

        public string ExistingDeploymentName { get; set; }
    }
}
