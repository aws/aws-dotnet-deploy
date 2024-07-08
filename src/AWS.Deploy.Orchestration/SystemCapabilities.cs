using System;

namespace AWS.Deploy.Orchestration
{
    /// <summary>
    /// Information about the user's Docker installation
    /// </summary>
    public class DockerInfo
    {
        /// <summary>
        /// Whether or not Docker is installed
        /// </summary>
        public bool DockerInstalled { get; set; }

        /// <summary>
        /// Docker's current OSType, expected to be "windows" or "linux"
        /// </summary>
        public string DockerContainerType { get; set; }

        public DockerInfo(
            bool dockerInstalled,
            string dockerContainerType)
        {
            DockerInstalled = dockerInstalled;
            DockerContainerType = dockerContainerType.Trim();
        }
    }

    /// <summary>
    /// Information about the user's NodeJS installation
    /// </summary>
    public class NodeInfo
    {
        /// <summary>
        /// Version of Node if it's installed, else null if not detected
        /// </summary>
        public Version? NodeJsVersion { get; set; }

        public NodeInfo(Version? version) => NodeJsVersion = version;
    }

    public class SystemCapability
    {
        public readonly string Name;
        public readonly string Message;
        public readonly string? InstallationUrl;

        public SystemCapability(string name, string message, string? installationUrl = null)
        {
            Name = name;
            Message = message;
            InstallationUrl = installationUrl;
        }

        public string GetMessage()
        {
            return string.IsNullOrEmpty(InstallationUrl)
                ? Message
                : $"{Message}{Environment.NewLine}You can install the missing {Name} dependency from: {InstallationUrl}";
        }
    }
}
