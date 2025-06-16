namespace AWS.Deploy.Orchestration;

/// <summary>
/// Information about the user's container app installation (Docker or Podman)
/// </summary>
public class ContainerAppInfo(
    string appName,
    string installUrl,
    bool isInstalled,
    string dockerContainerType)
{
    /// <summary>
    /// Container app name
    /// </summary>
    public string AppName { get; set; } = appName;

    /// <summary>
    /// Installation URL
    /// </summary>
    public string InstallationUrl { get; set; } = installUrl;

    /// <summary>
    /// Whether or not app is installed
    /// </summary>
    public bool IsInstalled { get; set; } = isInstalled;

    /// <summary>
    /// Container app's current OS type, expected to be "windows" or "linux"
    /// </summary>
    public string ContainerType { get; set; } = dockerContainerType.Trim();
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
