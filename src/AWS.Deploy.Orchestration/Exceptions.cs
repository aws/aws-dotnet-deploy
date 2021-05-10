using System;
using AWS.Deploy.Common;

namespace AWS.Deploy.Orchestration
{
    /// <summary>
    /// Exception is thrown if Microsoft Templating Engine is unable to generate a template
    /// </summary>
    [AWSDeploymentExpectedException]
    public class TemplateGenerationFailedException : Exception
    {
        public TemplateGenerationFailedException(string message, Exception? innerException = null) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception is thrown if Microsoft Templating Engine is unable to find location to install templates from
    /// </summary>
    [AWSDeploymentExpectedException]
    public class DefaultTemplateInstallationFailedException : Exception
    {
        public DefaultTemplateInstallationFailedException(string message, Exception? innerException = null) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception is thrown if Microsoft Templating Engine returns an error when running a command
    /// </summary>
    [AWSDeploymentExpectedException]
    public class RunCommandFailedException : Exception
    {
        public RunCommandFailedException(string message, Exception? innerException = null) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception is thrown if package.json file IO fails.
    /// </summary>
    [AWSDeploymentExpectedException]
    public class PackageJsonFileException : Exception
    {
        public PackageJsonFileException(string message, Exception? innerException = null) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception is thrown if docker build attempt failed
    /// </summary>
    [AWSDeploymentExpectedException]
    public class DockerBuildFailedException : Exception
    {
        public DockerBuildFailedException(string message, Exception? innerException = null) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception is thrown if npm command fails to execute.
    /// </summary>
    [AWSDeploymentExpectedException]
    public class NPMCommandFailedException : Exception
    {
        public NPMCommandFailedException(string message, Exception? innerException = null) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception is thrown if docker login attempt failed
    /// </summary>
    [AWSDeploymentExpectedException]
    public class DockerLoginFailedException : Exception
    {
        public DockerLoginFailedException(string message, Exception? innerException = null) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception is thrown if docker tag attempt failed
    /// </summary>
    [AWSDeploymentExpectedException]
    public class DockerTagFailedException : Exception
    {
        public DockerTagFailedException(string message, Exception? innerException = null) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception is thrown if docker push attempt failed
    /// </summary>
    [AWSDeploymentExpectedException]
    public class DockerPushFailedException : Exception
    {
        public DockerPushFailedException(string message, Exception? innerException = null) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception is thrown if we cannot retrieve deployment bundle definitions
    /// </summary>
    [AWSDeploymentExpectedException]
    public class NoDeploymentBundleDefinitionsFoundException : Exception
    {
        public NoDeploymentBundleDefinitionsFoundException(string message, Exception? innerException = null) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception is thrown if dotnet publish attempt failed
    /// </summary>
    [AWSDeploymentExpectedException]
    public class DotnetPublishFailedException : Exception
    {
        public DotnetPublishFailedException(string message, Exception? innerException = null) : base(message, innerException) { }
    }

    /// <summary>
    /// Throw if Zip File Manager fails to create a Zip File
    /// </summary>
    [AWSDeploymentExpectedException]
    public class FailedToCreateZipFileException : Exception
    {
        public FailedToCreateZipFileException(string message, Exception? innerException = null) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown if Docker file could not be generated
    /// </summary>
    [AWSDeploymentExpectedException]
    public class FailedToGenerateDockerFileException : Exception
    {
        public FailedToGenerateDockerFileException(string message, Exception? innerException = null) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown if RecipePath contains an invalid path
    /// </summary>
    [AWSDeploymentExpectedException]
    public class InvalidRecipePathException : Exception
    {
        public InvalidRecipePathException(string message, Exception? innerException = null) : base(message, innerException) { }
    }
}
