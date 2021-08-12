// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;
using Amazon.Runtime;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes.Validation;

namespace AWS.Deploy.Orchestration
{
    /// <summary>
    /// The Orchestrator session holds the relevant metadata about the project that needs to be deployed
    /// and also contains information about the AWS account and region used for deployment.
    /// </summary>
    public class OrchestratorSession : IDeployToolValidationContext
    {
        public ProjectDefinition ProjectDefinition { get; set; }
        public string? AWSProfileName { get; set; }
        public AWSCredentials? AWSCredentials { get; set; }
        public string? AWSRegion { get; set; }
        public string? AWSAccountId { get; set; }

        public OrchestratorSession(
            ProjectDefinition projectDefinition,
            AWSCredentials awsCredentials,
            string awsRegion,
            string awsAccountId)
        {
            ProjectDefinition = projectDefinition;
            AWSCredentials = awsCredentials;
            AWSRegion = awsRegion;
            AWSAccountId = awsAccountId;
        }

        public OrchestratorSession(ProjectDefinition projectDefinition)
        {
            ProjectDefinition = projectDefinition;
        }
    }
}
