using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BlazorWasm.Utilities
{
    public class CommandLineWrapper
    {
        public void Run(IEnumerable<string> commands, string workingDirectory = "")
        {
            var process = new Process();
            var shell = GetSystemShell();
            var processStartInfo = new ProcessStartInfo
            {
                FileName = shell,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = workingDirectory
            };
            process.StartInfo = processStartInfo;
            process.Start();
            process.OutputDataReceived += (sender, e) => { Console.WriteLine(e.Data); };
            process.ErrorDataReceived += (sender, e) => { Console.WriteLine(e.Data); };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            using (var streamWriter = process.StandardInput)
            {
                foreach (var command in commands)
                {
                    streamWriter.WriteLine(command);
                }
            }
            process.WaitForExit();
        }

        private string GetSystemShell()
        {
            var comspec = Environment.GetEnvironmentVariable("COMSPEC");
            if (!string.IsNullOrEmpty(comspec))
            {
                Console.WriteLine($"OS Version {Environment.OSVersion}. Using {comspec} as default shell.");
                return comspec;
            }

            var shell = Environment.GetEnvironmentVariable("SHELL");
            if (!string.IsNullOrEmpty(shell))
            {
                Console.WriteLine($"OS Version {Environment.OSVersion}. Using {shell} as default shell.");
                return shell;
            }

            throw new NotSupportedException($"{Environment.OSVersion} isn't supported");
        }
    }
}
