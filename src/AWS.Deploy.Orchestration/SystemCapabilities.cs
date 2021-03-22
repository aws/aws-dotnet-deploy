namespace AWS.Deploy.Orchestration
{
    public class SystemCapabilities
    {
        public bool NodeJsMinVersionInstalled { get; set; }
        public DockerInfo DockerInfo { get; set; }
    }

    public class DockerInfo
    {
        public bool DockerInstalled { get; set; }
        public string DockerContainerType { get; set; }
    }
}
