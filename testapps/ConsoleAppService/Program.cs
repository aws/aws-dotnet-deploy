using System;
using System.Threading.Tasks;

namespace ConsoleAppService
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            while (true)
            {
                Console.WriteLine("Hello World!");
                await Task.Delay(500);
            }
        }
    }
}
