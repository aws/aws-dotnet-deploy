// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace AWS.Deploy.Orchestration.LocalUserSettings
{
    public class LastDeployedStack
    {
        public string AWSAccountId { get; set; }

        public string AWSRegion { get; set; }

        public string ProjectName { get; set; }

        public List<string> Stacks { get; set; }

        public LastDeployedStack(string awsAccountId, string awsRegion, string projectName, List<string> stacks)
        {
            AWSAccountId = awsAccountId;
            AWSRegion = awsRegion;
            ProjectName = projectName;
            Stacks = stacks;
        }

        public bool Exists(string? awsAccountId, string? awsRegion, string? projectName)
        {
            if (string.IsNullOrEmpty(AWSAccountId) ||
                string.IsNullOrEmpty(AWSRegion) ||
                string.IsNullOrEmpty(ProjectName))
                return false;

            if (string.IsNullOrEmpty(awsAccountId) ||
                string.IsNullOrEmpty(awsRegion) ||
                string.IsNullOrEmpty(projectName))
                return false;

            if (AWSAccountId.Equals(awsAccountId) &&
                AWSRegion.Equals(awsRegion) &&
                ProjectName.Equals(projectName))
                return true;

            return false;
        }
    }
}
