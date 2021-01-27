// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.Common
{
    /// <summary>
    /// Contains CloudFormation specific configurations
    /// </summary>
    public class CloudApplication
    {
        /// <summary>
        /// Name of the CloudApplication
        /// used to create CloudFormation stack
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Name of CloudFormation stack
        /// </summary>
        /// <remarks>
        /// <see cref="Name"/> and <see cref="StackName"/> are two different properties and just happens to be same value at this moment.
        /// </remarks>
        public string StackName => Name;

        /// <summary>
        /// Tag key of the CloudFormation stack
        /// used to uniquely identify a stack that is deployed by aws-dotnet-deploy
        /// </summary>
        public const string StackTagKey = "aws-dotnet-deploy";
    }
}
