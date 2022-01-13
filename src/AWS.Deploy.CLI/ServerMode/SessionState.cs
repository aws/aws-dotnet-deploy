// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Orchestration;

namespace AWS.Deploy.CLI.ServerMode
{
    public class SessionState
    {
        public string SessionId { get; set; }

        public string ProjectPath { get; set; }

        public string AWSRegion { get; set; }

        public string AWSAccountId { get; set; }

        public ProjectDefinition ProjectDefinition { get; set; }

        public IList<Recommendation>? NewRecommendations { get; set; }

        public IList<CloudApplication>? ExistingDeployments { get; set; }

        public Recommendation? SelectedRecommendation { get; set; }

        public CloudApplication ApplicationDetails { get; } = new CloudApplication(string.Empty, string.Empty, CloudApplicationResourceType.None, string.Empty);

        public Task? DeploymentTask { get; set; }

        public SessionState(
            string sessionId,
            string projectPath,
            string awsRegion,
            string awsAccountId,
            ProjectDefinition projectDefinition
        )
        {
            SessionId = sessionId;
            ProjectPath = projectPath;
            AWSRegion = awsRegion;
            AWSAccountId = awsAccountId;
            ProjectDefinition = projectDefinition;
        }
    }
}
