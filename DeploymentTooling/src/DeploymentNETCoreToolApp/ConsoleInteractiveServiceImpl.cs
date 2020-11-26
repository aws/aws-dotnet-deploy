using System;

namespace AWS.DeploymentNETCoreToolApp
{
    public class ConsoleInteractiveServiceImpl : IToolInteractiveService
    {
        public string ReadLine()
        {
            return Console.ReadLine();
        }

        public void WriteErrorLine(string message)
        {
            Console.Error.WriteLine(message);
        }

        public void WriteLine(string message)
        {
            Console.WriteLine(message);
        }
    }
}
