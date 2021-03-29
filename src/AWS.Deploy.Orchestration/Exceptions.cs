using System;
using AWS.Deploy.Common;

namespace AWS.Deploy.Orchestration
{
    /// <summary>
    /// Exception is thrown if Microsoft Templating Engine is unable to generate a template
    /// </summary>
    [AWSDeploymentExpectedException]
    public class TemplateGenerationFailedException : Exception { }

    /// <summary>
    /// Exception is thrown if Microsoft Templating Engine is unable to find location to install templates from
    /// </summary>
    [AWSDeploymentExpectedException]
    public class DefaultTemplateInstallationFailedException : Exception
    {
        public DefaultTemplateInstallationFailedException(Exception innerException = null) : base(string.Empty, innerException)
        {
        }
    }

    /// <summary>
    /// Exception is thrown if package.json file IO fails.
    /// </summary>
    [AWSDeploymentExpectedException]
    public class PackageJsonFileException : Exception
    {
        public PackageJsonFileException(Exception innerException = null) : base(string.Empty, innerException)
        {
        }
    }

    /// <summary>
    /// Exception is thrown if docker build attempt failed
    /// </summary>
    [AWSDeploymentExpectedException]
    public class DockerBuildFailedException : Exception { }

    /// <summary>
    /// Exception is thrown if npm command fails to execute.
    /// </summary>
    [AWSDeploymentExpectedException]
    public class NPMCommandFailedException : Exception
    {
        public NPMCommandFailedException(Exception innerException = null) : base(string.Empty, innerException)
        {
        }
    }

    /// <summary>
    /// Exception is thrown if docker login attempt failed
    /// </summary>
    [AWSDeploymentExpectedException]
    public class DockerLoginFailedException : Exception { }

    /// <summary>
    /// Exception is thrown if docker tag attempt failed
    /// </summary>
    [AWSDeploymentExpectedException]
    public class DockerTagFailedException : Exception { }

    /// <summary>
    /// Exception is thrown if docker push attempt failed
    /// </summary>
    [AWSDeploymentExpectedException]
    public class DockerPushFailedException : Exception { }

    /// <summary>
    /// Exception is thrown if we cannot retrieve deployment bundle definitions
    /// </summary>
    [AWSDeploymentExpectedException]
    public class NoDeploymentBundleDefinitionsFoundException : Exception { }

    /// <summary>
    /// Exception is thrown if dotnet publish attempt failed
    /// </summary>
    [AWSDeploymentExpectedException]
    public class DotnetPublishFailedException : Exception { }

    /// <summary>
    /// Throw if Zip File Manager fails to create a Zip File
    /// </summary>
    [AWSDeploymentExpectedException]
    public class FailedToCreateZipFileException : Exception { }

    /// <summary>
    /// Throw if Docker Engine fails to generate a dockerfile
    /// </summary>
    [AWSDeploymentExpectedException]
    public class FailedToGenerateDockerFileException : Exception
    {
        public FailedToGenerateDockerFileException(Exception innerException = null) : base(string.Empty, innerException)
        {

        }
    }
}
