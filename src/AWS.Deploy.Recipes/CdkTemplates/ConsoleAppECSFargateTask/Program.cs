using Amazon.CDK;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace ConsoleAppEcsFargateTask
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder().AddJsonFile("appsettings.json", false, false);
            var configuration = builder.Build().Get<Configuration>();

            var app = new App();
            new AppStack(app, configuration.StackName, configuration);
            app.Synth();
        }
    }
}
