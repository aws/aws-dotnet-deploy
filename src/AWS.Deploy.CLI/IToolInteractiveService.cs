namespace AWS.Deploy.CLI
{
    public interface IToolInteractiveService
    {
        void WriteLine(string message);

        void WriteErrorLine(string message);

        string ReadLine();
    }
}
