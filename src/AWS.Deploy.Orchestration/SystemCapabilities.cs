namespace AWS.Deploy.Orchestration
{
    public class SystemCapabilities
    {
        public bool NodeJsMinVersionInstalled { get; set; }
        public DockerInfo DockerInfo { get; set; }

        public SystemCapabilities(
            bool nodeJsMinVersionInstalled,
            DockerInfo dockerInfo)
        {
            NodeJsMinVersionInstalled = nodeJsMinVersionInstalled;
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
}
