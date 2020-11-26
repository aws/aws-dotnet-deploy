using System;
using System.IO;
using System.Linq;
using System.Reflection;
using AWS.Deploy.Common;
using AWS.Deploy.Recipes;
using Should;
using Xunit;

namespace AWS.Deploy.CLI.UnitTests
{
    public class RecommendationTests
    {
        private const string ASPNET_CORE_ASPNET_CORE_FARGATE_RECIPE_ID = "ASPNETCoreECSFargate";
        private const string ASPNET_CORE_BEANSTALK_RECIPE_ID = "ASPNETCoreElasticBeanstalkLinux";

        private const string CONSOLE_APP_FARGATE_SERVICE_RECIPE_ID = "ConsoleAppECSFargateService";
        private const string CONSOLE_APP_FARGATE_TASK_RECIPE_ID = "ConsoleAppECSFargateTask";

        [Fact]
        public void WebAppNoDockerFileTest()
        {
            var projectPath = ResolvePath("WebAppNoDockerFile");
            
            var engine = new RecommendationEngine(RecipeLocator.FindRecipeDefinitionsPath());

            var recommendations = engine.ComputeRecommendations(projectPath);

            recommendations
                .Any(r => r.Recipe.Id == ASPNET_CORE_BEANSTALK_RECIPE_ID)
                .ShouldBeTrue("Failed to receive Recommendation: " + ASPNET_CORE_BEANSTALK_RECIPE_ID);

            recommendations
                .Any(r => r.Recipe.Id == ASPNET_CORE_ASPNET_CORE_FARGATE_RECIPE_ID)
                .ShouldBeTrue("Failed to receive Recommendation: " + ASPNET_CORE_ASPNET_CORE_FARGATE_RECIPE_ID);
        }

        [Fact]
        public void WebAppWithDockerFileTest()
        {
            var projectPath = ResolvePath("WebAppWithDockerFile");

            var engine = new RecommendationEngine(RecipeLocator.FindRecipeDefinitionsPath());

            var recommendations = engine.ComputeRecommendations(projectPath);

            recommendations
                .Any(r => r.Recipe.Id == ASPNET_CORE_ASPNET_CORE_FARGATE_RECIPE_ID)
                .ShouldBeTrue("Failed to receive Recommendation: " + ASPNET_CORE_ASPNET_CORE_FARGATE_RECIPE_ID);

            recommendations
                .Any(r => r.Recipe.Id == ASPNET_CORE_BEANSTALK_RECIPE_ID)
                .ShouldBeTrue("Failed to receive Recommendation: " + ASPNET_CORE_BEANSTALK_RECIPE_ID);
        }

        [Fact]
        public void MessageProcessingAppTest()
        {
            var projectPath = ResolvePath("MessageProcessingApp");

            var engine = new RecommendationEngine(RecipeLocator.FindRecipeDefinitionsPath());

            var recommendations = engine.ComputeRecommendations(projectPath);

            recommendations
                .Any(r => r.Recipe.Id == CONSOLE_APP_FARGATE_SERVICE_RECIPE_ID)
                .ShouldBeTrue("Failed to receive Recommendation: " + CONSOLE_APP_FARGATE_SERVICE_RECIPE_ID);

            recommendations
                .Any(r => r.Recipe.Id == CONSOLE_APP_FARGATE_TASK_RECIPE_ID)
                .ShouldBeTrue("Failed to receive Recommendation: " + CONSOLE_APP_FARGATE_TASK_RECIPE_ID);
        }

        private string ResolvePath(string projectName)
        {
            var testsPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            while(testsPath != null && !string.Equals(new DirectoryInfo(testsPath).Name, "test", StringComparison.OrdinalIgnoreCase))
            {
                testsPath = Directory.GetParent(testsPath).FullName;
            }

            return Path.Combine(testsPath, "..", "testapps", projectName);
        }
    }
}
