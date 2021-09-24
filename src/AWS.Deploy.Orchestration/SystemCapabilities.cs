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
            DockerContainerType = dockerContainerType;
        }
    }

    public class SystemCapability
    {
        public string Name {  get; set; }
        public bool Installed { get; set; }
        public bool Available { get; set; }
        public string? Message { get; set; }
        public string? InstallationUrl { get; set; }

        public SystemCapability(string name, bool installed, bool available)
        {
            Name = name;
            Installed = installed;
            Available = available;
        }

        public string GetMessage()
        {
            if (!string.IsNullOrEmpty(Message))
                return Message;

            var availabilityMessage = Available ? "and available" : "but not available";
            var installationMessage = Installed ? $"installed {availabilityMessage}" : "not installed";
            return $"The system capability '{Name}' is {installationMessage}";
        }
    }
}
