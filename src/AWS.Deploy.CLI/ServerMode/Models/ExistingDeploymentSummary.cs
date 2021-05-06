// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AWS.Deploy.CLI.ServerMode.Models
{
    public class ExistingDeploymentSummary
    {
        public string Name { get; set; }

        public string RecipeId { get; set; }
    }
}
