using Amazon.CDK;
using AWS.Deploy.Recipes.CDK.Common;
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
            var app = new App();

            var builder = new ConfigurationBuilder().AddAWSDeployToolConfiguration(app);
            var recipeConfiguration = builder.Build().Get<RecipeConfiguration<Configuration>>();

            var zipPublisher = new ZipPublisher();
            recipeConfiguration.Settings.AssetPath = zipPublisher.GetZipPath(recipeConfiguration.Settings, recipeConfiguration.ProjectPath);

            CDKRecipeSetup.RegisterStack<Configuration>(new AppStack(app, recipeConfiguration, new StackProps
            {
                Env = new Environment
                {
                    Account = "AWSAccountId",
                    Region = "AWSRegion"
                }
            }), recipeConfiguration);

            app.Synth();
        }
    }
}
