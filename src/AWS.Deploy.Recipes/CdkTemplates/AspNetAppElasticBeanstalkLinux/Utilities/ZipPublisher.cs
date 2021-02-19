using System;
using System.IO;
using System.IO.Compression;
using AspNetAppElasticBeanstalkLinux.Configurations;

namespace AspNetAppElasticBeanstalkLinux.Utilities
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
        public string GetZipPath(Configuration configuration, string projectPath)
        {
            var publishDirectoryInfo = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
            var additionalArguments = @"DotNetPublishAdditionalArguments-Placeholder";
            var runtimeArg =
               configuration.SelfContainedBuild &&
               !additionalArguments.Contains("--runtime ") &&
               !additionalArguments.Contains("-r ")
                     ? "--runtime linux-x64"
                     : "";
            var publishCommands = new []
            {
                $"dotnet publish {projectPath} -o {publishDirectoryInfo} -c DotnetBuildConfiguration-Placeholder" +
                $" --self-contained {configuration.SelfContainedBuild}" +
                $" {runtimeArg}" +
                $" {additionalArguments}"
            };

            _commandLineWrapper.Run(publishCommands);

            var zipFilePath = $"{publishDirectoryInfo.FullName}.zip";
            ZipFile.CreateFromDirectory(publishDirectoryInfo.FullName, zipFilePath);
            return zipFilePath;
        }
    }
}
