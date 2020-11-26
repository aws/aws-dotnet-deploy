using Amazon.CDK;
using Microsoft.Extensions.Configuration;

namespace ASPNETCoreECSFargate
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder().AddJsonFile("appsettings.json", false, false);
            var configuration = new Configuration(builder.Build());

            var app = new App();
            new ASPNETCoreECSFargateStack(app, configuration.StackName, configuration);
            app.Synth();
        }
    }
}
