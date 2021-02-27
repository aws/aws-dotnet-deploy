using System;
using System.IO;
using System.IO.Compression;
using BlazorWasm.Configurations;

namespace BlazorWasm.Utilities
{
    /// <summary>
    /// Publishes dotnet project and creates zip of the published artifact
    /// </summary>
    public class DotnetPublisher
    {
        private readonly CommandLineWrapper _commandLineWrapper;

        public DotnetPublisher()
        {
            _commandLineWrapper = new CommandLineWrapper();
        }

        /// <summary>
        /// Publishes given csproj located at given path in a temporary directory and creates zip of the published artifact
        /// and returns the path to the zip file
        /// </summary>
        /// <returns></returns>
        public string GetPublishFolder(Configuration configuration, string projectPath)
        {
            var publishDirectoryInfo = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
            var additionalArguments = @"DotNetPublishAdditionalArguments-Placeholder";
            var runtimeArg =
               !additionalArguments.Contains("--runtime ") &&
               !additionalArguments.Contains("-r ")
                     ? "--runtime linux-x64"
                     : "";
            var publishCommands = new string[]
            {
                $"dotnet publish {projectPath} -o {publishDirectoryInfo} -c DotnetBuildConfiguration-Placeholder" +
                $" {runtimeArg}" +
                $" {additionalArguments}"
            };

            _commandLineWrapper.Run(publishCommands);

            return publishDirectoryInfo.FullName;
        }
    }
}
