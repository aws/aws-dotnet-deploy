// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using AWS.Deploy.Common;

namespace AWS.Deploy.CLI
{
    /// <summary>
    /// Throw if no AWS credentials were found.
    /// </summary>
    [AWSDeploymentExpectedException]
    public class NoAWSCredentialsFoundException : Exception
    {
        public NoAWSCredentialsFoundException(string message, Exception innerException = null) : base(message, innerException) { }
    }

    /// <summary>
    /// Throw if Delete Command is unable to delete
    /// the specified stack
    /// </summary>
    [AWSDeploymentExpectedException ]
    public class FailedToDeleteException : Exception
    {
        public FailedToDeleteException(string message, Exception innerException = null) : base(message, innerException) { }
    }

    /// <summary>
    /// Throw if Deploy Command is unable to find a target to deploy.
    /// Currently, this is limited to .csproj or .fsproj files.
    /// </summary>
    [AWSDeploymentExpectedException]
    public class FailedToFindDeployableTargetException : Exception
    {
        public FailedToFindDeployableTargetException(string message, Exception innerException = null) : base(message, innerException) { }
    }
}
