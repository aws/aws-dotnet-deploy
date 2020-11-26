using Amazon.CDK;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ASPNETCoreElasticBeanstalkLinux.Utilities;
using Microsoft.Extensions.Configuration;

namespace ASPNETCoreElasticBeanstalkLinux
{
    sealed class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = new ConfigurationBuilder().AddJsonFile("appsettings.json", false, false);
            var configuration = new Configuration(builder.Build());

            var zipPublisher = new ZipPublisher();
            configuration.AssetPath = zipPublisher.GetZipPath(configuration.ProjectPath);

            var solutionStackNameProvider = new SolutionStackNameProvider();
            configuration.SolutionStackName = await solutionStackNameProvider.GetSolutionStackNameAsync();

            var app = new App();
            new ASPNETCoreElasticBeanstalkLinuxStack(app, configuration.StackName, configuration);
            app.Synth();
        }
    }
}
