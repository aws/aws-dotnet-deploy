using Amazon.CDK;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace ConsoleAppECSFargateService
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder().AddJsonFile("appsettings.json", false, false);
            var configuration = new Configuration(builder.Build());

            var app = new App();
            new ConsoleAppECSFargateServiceStack(app, configuration.StackName, configuration);
            app.Synth();
        }
    }
}
