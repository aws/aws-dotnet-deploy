using System;
using System.IO;
using System.Reflection;
using AWS.DefaultDotNETRecipes;
using AWS.DeploymentRecommendationEngine;
using Xunit;

namespace AWS.Deployment.Unit.Tests
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
            Assert.Equal(2, recommendations.Count);

            Assert.Equal(ASPNET_CORE_BEANSTALK_RECIPE_ID, recommendations[0].Recipe.Id);
            Assert.Equal(ASPNET_CORE_ASPNET_CORE_FARGATE_RECIPE_ID, recommendations[1].Recipe.Id);
        }

        [Fact]
        public void WebAppWithDockerFileTest()
        {
            var projectPath = ResolvePath("WebAppWithDockerFile");

            var engine = new RecommendationEngine(RecipeLocator.FindRecipeDefinitionsPath());

            var recommendations = engine.ComputeRecommendations(projectPath);
            Assert.Equal(2, recommendations.Count);

            Assert.Equal(ASPNET_CORE_ASPNET_CORE_FARGATE_RECIPE_ID, recommendations[0].Recipe.Id);
            Assert.Equal(ASPNET_CORE_BEANSTALK_RECIPE_ID, recommendations[1].Recipe.Id);
        }

        [Fact]
        public void MessageProcessingAppTest()
        {
            var projectPath = ResolvePath("MessageProcessingApp");

            var engine = new RecommendationEngine(RecipeLocator.FindRecipeDefinitionsPath());

            var recommendations = engine.ComputeRecommendations(projectPath);
            Assert.Equal(2, recommendations.Count);

            Assert.Equal(CONSOLE_APP_FARGATE_SERVICE_RECIPE_ID, recommendations[0].Recipe.Id);
            Assert.Equal(CONSOLE_APP_FARGATE_TASK_RECIPE_ID, recommendations[1].Recipe.Id);
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
