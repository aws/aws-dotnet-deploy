using System;
using System.IO;
using System.IO.Compression;

namespace ASPNETCoreElasticBeanstalkLinux.Utilities
{
    /// <summary>
    /// Publishes dotnet project and creates zip of the published artifact
    /// </summary>
    public class ZipPublisher
    {
        private readonly CommandLineWrapper _commandLineWrapper;

        public ZipPublisher()
        {
            _commandLineWrapper = new CommandLineWrapper();
        }

        /// <summary>
        /// Publishes given csproj located at given path in a temporary directory and creates zip of the published artifact
        /// and returns the path to the zip file
        /// </summary>
        /// <returns></returns>
        public string GetZipPath(string projectPath)
        {
            var publishDirectoryInfo = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
            var publishCommands = new []
            {
                $"dotnet publish {projectPath} -o {publishDirectoryInfo}"
            };

            _commandLineWrapper.Run(publishCommands);

            var zipFilePath = $"{publishDirectoryInfo.FullName}.zip";
            ZipFile.CreateFromDirectory(publishDirectoryInfo.FullName, zipFilePath);
            return zipFilePath;
        }
    }
}