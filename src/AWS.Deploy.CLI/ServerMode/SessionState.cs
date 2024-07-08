// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Threading.Tasks;
using AWS.Deploy.CLI.ServerMode.Services;
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

        public SessionAWSResourceQuery? AWSResourceQueryService { get; set; }

        public SystemCapabilityEvaluator? SystemCapabilityEvaluator { get; set; }

        public Task? DeploymentTask { get; set; }

        public SessionState(
            string sessionId,
            string projectPath,
            string awsRegion,
            ProjectDefinition projectDefinition
        )
        {
            SessionId = sessionId;
            ProjectPath = projectPath;
            AWSRegion = awsRegion;
            ProjectDefinition = projectDefinition;
            AWSAccountId = string.Empty;
        }
    }
}
