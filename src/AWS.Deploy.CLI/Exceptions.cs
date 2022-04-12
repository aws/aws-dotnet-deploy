// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using AWS.Deploy.Common;

namespace AWS.Deploy.CLI
{
    /// <summary>
    /// Throw if no AWS credentials were found.
    /// </summary>
    public class NoAWSCredentialsFoundException : DeployToolException
    {
        public NoAWSCredentialsFoundException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Throw if Delete Command is unable to delete
    /// the specified stack
    /// </summary>
    public class FailedToDeleteException : DeployToolException
    {
        public FailedToDeleteException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Throw if Deploy Command is unable to find a target to deploy.
    /// Currently, this is limited to .csproj or .fsproj files.
    /// </summary>
    public class FailedToFindDeployableTargetException : DeployToolException
    {
        public FailedToFindDeployableTargetException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Throw if prompting the user for a name returns a null value.
    /// </summary>
    public class UserPromptForNameReturnedNullException : DeployToolException
    {
        public UserPromptForNameReturnedNullException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Throw if the system capabilities were not provided.
    /// </summary>
    public class SystemCapabilitiesNotProvidedException : DeployToolException
    {
        public SystemCapabilitiesNotProvidedException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Throw if TCP port is in use by another process.
    /// </summary>
    public class TcpPortInUseException : DeployToolException
    {
        public TcpPortInUseException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Throw if unable to find a compatible recipe.
    /// </summary>
    public class FailedToFindCompatibleRecipeException : DeployToolException
    {
        public FailedToFindCompatibleRecipeException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Throw if the directory specified to save the CDK deployment project is invalid.
    /// </summary>
    public class InvalidSaveDirectoryForCdkProject : DeployToolException
    {
        public InvalidSaveDirectoryForCdkProject(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    public class FailedToFindDeploymentProjectRecipeIdException : DeployToolException
    {
        public FailedToFindDeploymentProjectRecipeIdException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Throw if failed to retrieve credentials from the specified profile name.
    /// </summary>
    public class FailedToGetCredentialsForProfile : DeployToolException
    {
        public FailedToGetCredentialsForProfile(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }
}
