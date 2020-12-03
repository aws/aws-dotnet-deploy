using System;

namespace AWS.DeploymentCommon
{
    public class ProjectFileNotFoundException : Exception
    {
        public ProjectFileNotFoundException(string message) : base(message)
        {
        }
    }
}
