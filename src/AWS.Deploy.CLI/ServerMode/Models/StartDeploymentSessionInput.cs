// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.Deploy.CLI.ServerMode.Models
{
    public class StartDeploymentSessionInput
    {
        public string AWSRegion { get; set; }
        public string ProjectPath { get; set; }

        public StartDeploymentSessionInput(
            string awsRegion,
            string projectPath)
        {
            AWSRegion = awsRegion;
            ProjectPath = projectPath;
        }
    }
}
