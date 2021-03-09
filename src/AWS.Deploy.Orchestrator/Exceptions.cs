using System;
using AWS.Deploy.Orchestrator.CDK;

namespace AWS.Deploy.Orchestrator
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
    /// Exception is thrown if npm command fails to execute.
    /// </summary>
    public class NPMCommandFailedException : Exception
    {
        public NPMCommandFailedException(string message, Exception innerException = null) : base(message, innerException)
        {
        }
    }
}
