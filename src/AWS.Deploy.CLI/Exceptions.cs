// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using AWS.DeploymentCommon;

namespace AWS.Deploy.CLI
{
    [AWSDeploymentExpectedException]
    public class NoAWSCredentialsFoundException : Exception { }

    /// <summary>
    /// Throw if Delete Command is unable to delete
    /// the specified stack
    /// </summary>
    [AWSDeploymentExpectedExceptionAttribute ]
    public class FailedToDeleteException : Exception
    {
        public FailedToDeleteException(string message) : base(message)
        {
        }
    }
}
