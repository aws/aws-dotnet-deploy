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
        public NoAWSCredentialsFoundException(string message, Exception? innerException = null) : base(message, innerException) { }
    }

    /// <summary>
    /// Throw if Delete Command is unable to delete
    /// the specified stack
    /// </summary>
    [AWSDeploymentExpectedException ]
    public class FailedToDeleteException : Exception
    {
        public FailedToDeleteException(string message, Exception? innerException = null) : base(message, innerException) { }
    }

    /// <summary>
    /// Throw if Deploy Command is unable to find a target to deploy.
    /// Currently, this is limited to .csproj or .fsproj files.
    /// </summary>
    [AWSDeploymentExpectedException]
    public class FailedToFindDeployableTargetException : Exception
    {
        public FailedToFindDeployableTargetException(string message, Exception? innerException = null) : base(message, innerException) { }
    }

    /// <summary>
    /// Throw if prompting the user for a name returns a null value.
    /// </summary>
    [AWSDeploymentExpectedException]
    public class UserPromptForNameReturnedNullException : Exception
    {
        public UserPromptForNameReturnedNullException(string message, Exception? innerException = null) : base(message, innerException) { }
    }

    /// <summary>
    /// Throw if the system capabilities were not provided.
    /// </summary>
    [AWSDeploymentExpectedException]
    public class SystemCapabilitiesNotProvidedException : Exception
    {
        public SystemCapabilitiesNotProvidedException(string message, Exception? innerException = null) : base(message, innerException) { }
    }

    /// <summary>
    /// Throw if TCP port is in use by another process.
    /// </summary>
    [AWSDeploymentExpectedException]
    public class TcpPortInUseException : Exception
    {
        public TcpPortInUseException(string message, Exception? innerException = null) : base(message, innerException) { }
    }

    /// <summary>
    /// Throw if unable to find a compatible recipe.
    /// </summary>
    [AWSDeploymentExpectedException]
    public class FailedToFindCompatibleRecipeException : Exception
    {
        public FailedToFindCompatibleRecipeException(string message, Exception? innerException = null) : base(message, innerException) { }
    }

    /// <summary>
    /// Throw if the directory specified to save the CDK deployment project is invalid.
    /// </summary>
    [AWSDeploymentExpectedException]
    public class InvalidSaveDirectoryForCdkProject : Exception
    {
        public InvalidSaveDirectoryForCdkProject(string message, Exception? innerException = null) : base(message, innerException) { }
    }

    public class FailedToFindDeploymentProjectRecipeIdException : Exception
    {
        public FailedToFindDeploymentProjectRecipeIdException(string message, Exception? innerException = null) : base(message, innerException) { }
    }
}
