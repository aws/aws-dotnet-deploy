using System;

namespace AWS.Deploy.Orchestration
{
    public class SystemCapabilities
    {
        public Version? NodeJsVersion { get; set; }
        public DockerInfo DockerInfo { get; set; }

        public SystemCapabilities(
            Version? nodeJsVersion,
            DockerInfo dockerInfo)
        {
            NodeJsVersion = nodeJsVersion;
            DockerInfo = dockerInfo;
        }
    }

    public class DockerInfo
    {
        public bool DockerInstalled { get; set; }
        public string DockerContainerType { get; set; }

        public DockerInfo(
            bool dockerInstalled,
            string dockerContainerType)
        {
            DockerInstalled = dockerInstalled;
            DockerContainerType = dockerContainerType.Trim();
        }
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
