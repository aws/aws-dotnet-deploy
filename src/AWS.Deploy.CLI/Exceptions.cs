// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using AWS.Deploy.Common;

namespace AWS.Deploy.CLI
{
    [AWSDeploymentExpectedException]
    public class NoAWSCredentialsFoundException : Exception { }

    /// <summary>
    /// Throw if Delete Command is unable to delete
    /// the specified stack
    /// </summary>
    [AWSDeploymentExpectedException ]
    public class FailedToDeleteException : Exception
    {
        public FailedToDeleteException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Throw if Deploy Command is unable to find a target to deploy.
    /// Currently, this is limited to .csproj or .fsproj files.
    /// </summary>
    [AWSDeploymentExpectedException]
    public class FailedToFindDeployableTargetException : Exception
    {
        public FailedToFindDeployableTargetException(Exception innerException = null) : base(string.Empty, innerException)
        {
        }
    }
}
