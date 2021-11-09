
using System;
using AWS.Deploy.Common;

namespace AWS.Deploy.Orchestration
{
    /// <summary>
    /// Exception is thrown if Microsoft Templating Engine is unable to generate a template
    /// </summary>
    public class TemplateGenerationFailedException : DeployToolException
    {
        public TemplateGenerationFailedException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Exception is thrown if Microsoft Templating Engine is unable to find location to install templates from
    /// </summary>
    public class DefaultTemplateInstallationFailedException : DeployToolException
    {
        public DefaultTemplateInstallationFailedException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Exception is thrown if Microsoft Templating Engine returns an error when running a command
    /// </summary>
    public class RunCommandFailedException : DeployToolException
    {
        public RunCommandFailedException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Exception is thrown if package.json file IO fails.
    /// </summary>
    public class PackageJsonFileException : DeployToolException
    {
        public PackageJsonFileException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Exception is thrown if docker build attempt failed
    /// </summary>
    public class DockerBuildFailedException : DeployToolException
    {
        public DockerBuildFailedException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Exception is thrown if npm command fails to execute.
    /// </summary>
    public class NPMCommandFailedException : DeployToolException
    {
        public NPMCommandFailedException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Exception is thrown if docker login attempt failed
    /// </summary>
    public class DockerLoginFailedException : DeployToolException
    {
        public DockerLoginFailedException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Exception is thrown if docker tag attempt failed
    /// </summary>
    public class DockerTagFailedException : DeployToolException
    {
        public DockerTagFailedException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Exception is thrown if docker push attempt failed
    /// </summary>
    public class DockerPushFailedException : DeployToolException
    {
        public DockerPushFailedException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Exception is thrown if we cannot retrieve recipe definitions
    /// </summary>
    public class NoRecipeDefinitionsFoundException : DeployToolException
    {
        public NoRecipeDefinitionsFoundException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Exception is thrown if dotnet publish attempt failed
    /// </summary>
    public class DotnetPublishFailedException : DeployToolException
    {
        public DotnetPublishFailedException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Throw if Zip File Manager fails to create a Zip File
    /// </summary>
    public class FailedToCreateZipFileException : DeployToolException
    {
        public FailedToCreateZipFileException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Exception thrown if Docker file could not be generated
    /// </summary>
    public class FailedToGenerateDockerFileException : DeployToolException
    {
        public FailedToGenerateDockerFileException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Exception thrown if RecipePath contains an invalid path
    /// </summary>
    public class InvalidRecipePathException : DeployToolException
    {
        public InvalidRecipePathException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Exception thrown if Solution Path contains an invalid path
    /// </summary>
    public class InvalidSolutionPathException : DeployToolException
    {
        public InvalidSolutionPathException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Exception thrown if AWS Deploy Recipes CDK Common Product Version is invalid.
    /// </summary>
    public class InvalidAWSDeployRecipesCDKCommonVersionException : DeployToolException
    {
        public InvalidAWSDeployRecipesCDKCommonVersionException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Exception thrown if the 'cdk deploy' command failed.
    /// </summary>
    public class FailedToDeployCDKAppException : DeployToolException
    {
        public FailedToDeployCDKAppException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Exception thrown if an AWS Resource is not found or does not exist.
    /// </summary>
    public class AWSResourceNotFoundException : DeployToolException
    {
        public AWSResourceNotFoundException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Exception thrown if the Local User Settings File is invalid.
    /// </summary>
    public class InvalidLocalUserSettingsFileException : DeployToolException
    {
        public InvalidLocalUserSettingsFileException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Exception thrown if a failure occured while trying to update the Local User Settings file.
    /// </summary>
    public class FailedToUpdateLocalUserSettingsFileException : DeployToolException
    {
        public FailedToUpdateLocalUserSettingsFileException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Throw if docker info failed to return output.
    /// </summary>
    public class DockerInfoException : DeployToolException
    {
        public DockerInfoException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }
}
