namespace AWS.DeploymentNETCoreToolApp
{
    public interface IToolInteractiveService
    {
        void WriteLine(string message);

        void WriteErrorLine(string message);

        string ReadLine();
    }
}
