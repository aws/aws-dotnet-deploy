using Amazon.CDK;
using System.Threading.Tasks;
using AspNetAppElasticBeanstalkLinux.Configurations;
using AspNetAppElasticBeanstalkLinux.Utilities;
using Microsoft.Extensions.Configuration;

namespace AspNetAppElasticBeanstalkLinux
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder().AddJsonFile("appsettings.json", false, false);
            var configuration = builder.Build().Get<Configuration>();

            var zipPublisher = new ZipPublisher();
            configuration.AssetPath = zipPublisher.GetZipPath(configuration);

            var app = new App();
            new AppStack(app, configuration.StackName, configuration, new StackProps
            {
                Env = new Environment
                {
                    Account = "AWSAccountId",
                    Region = "AWSRegion"
                }
            });
            app.Synth();
        }
    }
}
