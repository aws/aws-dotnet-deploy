using System;

namespace AWS.Deploy.Orchestration
{
    /// <summary>
    /// Exception is thrown if Microsoft Templating Engine is unable to generate a template
    /// </summary>
    public class TemplateGenerationFailedException : Exception
    {
        public TemplateGenerationFailedException() : base()
        {
        }
    }

    /// <summary>
    /// Exception is thrown if Microsoft Templating Engine is unable to find location to install templates from
    /// </summary>
    public class DefaultTemplateInstallationFailedException : Exception
    {
        public DefaultTemplateInstallationFailedException(Exception innerException = null) : base(innerException?.Message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception is thrown if Microsoft Templating Engine returns an error when running a command
    /// </summary>
    public class RunCommandFailedException : Exception
    {
        public RunCommandFailedException() : base()
        {
        }
    }

    /// <summary>
    /// Exception is thrown if package.json file IO fails.
    /// </summary>
    public class PackageJsonFileException : Exception
    {
        public PackageJsonFileException(string message, Exception innerException = null) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception is thrown if docker build attempt failed
    /// </summary>
    public class DockerBuildFailedException : Exception
    {
        public DockerBuildFailedException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Exception is thrown if npm command fails to execute.
    /// </summary>
    public class NPMCommandFailedException : Exception
    {
        public NPMCommandFailedException(string message, Exception innerException = null) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception is thrown if docker login attempt failed
    /// </summary>
    public class DockerLoginFailedException : Exception
    {
        public DockerLoginFailedException() : base()
        {
        }
    }

    /// <summary>
    /// Exception is thrown if docker tag attempt failed
    /// </summary>
    public class DockerTagFailedException : Exception
    {
        public DockerTagFailedException() : base()
        {
        }
    }

    /// <summary>
    /// Exception is thrown if docker push attempt failed
    /// </summary>
    public class DockerPushFailedException : Exception
    {
        public DockerPushFailedException() : base()
        {
        }
    }

    /// <summary>
    /// Exception is thrown if we cannot retrieve deployment bundle definitions
    /// </summary>
    public class NoDeploymentBundleDefinitionsFoundException : Exception
    {
        public NoDeploymentBundleDefinitionsFoundException() : base()
        {
        }
    }

    /// <summary>
    /// Exception is thrown if dotnet publish attempt failed
    /// </summary>
    public class DotnetPublishFailedException : Exception
    {
        public DotnetPublishFailedException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Throw if Zip File Manager fails to create a Zip File
    /// </summary>
    public class FailedToCreateZipFileException : Exception
    {
        public FailedToCreateZipFileException(string message) : base(message)
        {
        }
    }
}
