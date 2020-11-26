using System;

namespace AWS.DeploymentDockerEngine
{
    public class DockerFileTemplateException : Exception
    {
        public DockerFileTemplateException(string message) : base(message) { }
    }

    public class DockerEngineException : Exception
    {
        public DockerEngineException(string message) : base(message) { }
    }
    
    public class UnknownDockerImageException : Exception
    {
        public UnknownDockerImageException(string message) : base(message) { }
    }
}
