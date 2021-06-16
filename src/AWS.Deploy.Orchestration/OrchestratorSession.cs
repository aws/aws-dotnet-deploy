// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;
using Amazon.Runtime;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes.Validation;

namespace AWS.Deploy.Orchestration
{
    public class OrchestratorSession : IDeployToolValidationContext
    {
        public ProjectDefinition ProjectDefinition { get; set; }
        public string? AWSProfileName { get; set; }
        public AWSCredentials AWSCredentials { get; set; }
        public string AWSRegion { get; set; }
        /// <remarks>
        /// Calculating the current <see cref="SystemCapabilities"/> can take several seconds
        /// and is not needed immediately so it is run as a background Task.
        /// <para />
        /// It's safe to repeatedly await this property; evaluation will only be done once.
        /// </remarks>
        public Task<SystemCapabilities>? SystemCapabilities { get; set; }
        public string AWSAccountId { get; set; }

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
    }
}
