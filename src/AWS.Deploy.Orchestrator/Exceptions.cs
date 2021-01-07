using System;

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
        public DefaultTemplateInstallationFailedException(Exception innerException = null) : base("",innerException)
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
}
