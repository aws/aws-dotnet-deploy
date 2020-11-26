using System;

namespace AWS.DeploymentNETCoreToolApp
{
    public class NoAWSCredentialsFoundException : Exception
    {
        public const string UNABLE_RESOLVE_MESSAGE = "Unable to resolve AWS credentials to access AWS.";

        public NoAWSCredentialsFoundException(string message)
            : base(message) { }
    }
}
