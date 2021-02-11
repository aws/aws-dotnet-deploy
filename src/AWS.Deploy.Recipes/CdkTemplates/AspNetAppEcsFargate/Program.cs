using Amazon.CDK;
using AspNetAppEcsFargate.Configurations;
using Microsoft.Extensions.Configuration;

namespace AspNetAppEcsFargate
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder().AddJsonFile("appsettings.json", false, false);
            var configuration = builder.Build().Get<Configuration>();

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
