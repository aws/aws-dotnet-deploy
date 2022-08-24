// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Runtime;
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.CLI.TypeHintResponses;
using AWS.Deploy.CLI.UnitTests.Utilities;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Common.DeploymentManifest;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Recipes.Validation;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.RecommendationEngine;
using AWS.Deploy.Recipes;
using Moq;
using Should;
using Xunit;

namespace AWS.Deploy.CLI.UnitTests
{
    public class RecommendationTests
    {
        private OrchestratorSession _session;
        private readonly TestDirectoryManager _directoryManager;
        private readonly Mock<IValidatorFactory> _validatorFactory;
        private readonly IOptionSettingHandler _optionSettingHandler;
        private readonly Mock<IAWSResourceQueryer> _awsResourceQueryer;
        private readonly SubnetsInVpcValidator _subnetsInVpcValidator;
        private readonly Mock<IServiceProvider> _serviceProvider;
        private readonly IDeploymentManifestEngine _deploymentManifestEngine;
        private readonly IOrchestratorInteractiveService _orchestratorInteractiveService;
        private readonly TestFileManager _fileManager;
        private readonly IRecipeHandler _recipeHandler;

        public RecommendationTests()
        {
            _directoryManager = new TestDirectoryManager();
            _awsResourceQueryer = new Mock<IAWSResourceQueryer>();
            _serviceProvider = new Mock<IServiceProvider>();
            _serviceProvider
                .Setup(x => x.GetService(typeof(IAWSResourceQueryer)))
                .Returns(_awsResourceQueryer.Object);
            _validatorFactory = new Mock<IValidatorFactory>();
            _optionSettingHandler = new OptionSettingHandler(_validatorFactory.Object);
            _subnetsInVpcValidator = new SubnetsInVpcValidator(_awsResourceQueryer.Object, _optionSettingHandler);
            _serviceProvider
                .Setup(x => x.GetService(typeof(IOptionSettingItemValidator)))
                .Returns(_subnetsInVpcValidator);
            _directoryManager = new TestDirectoryManager();
            _fileManager = new TestFileManager();
            var recipeFiles = Directory.GetFiles(RecipeLocator.FindRecipeDefinitionsPath(), "*.recipe", SearchOption.TopDirectoryOnly);
            _directoryManager.AddedFiles.Add(RecipeLocator.FindRecipeDefinitionsPath(), new HashSet<string>(recipeFiles));
            foreach (var recipeFile in recipeFiles)
                _fileManager.InMemoryStore.Add(recipeFile, File.ReadAllText(recipeFile));
            _deploymentManifestEngine = new DeploymentManifestEngine(_directoryManager, _fileManager);
            _orchestratorInteractiveService = new TestToolOrchestratorInteractiveService();
            _recipeHandler = new RecipeHandler(_deploymentManifestEngine, _orchestratorInteractiveService, _directoryManager, _fileManager, _optionSettingHandler, _validatorFactory.Object);
        }

        private async Task<RecommendationEngine> BuildRecommendationEngine(string testProjectName)
        {
            var fullPath = SystemIOUtilities.ResolvePath(testProjectName);

            var parser = new ProjectDefinitionParser(new FileManager(), new DirectoryManager());
            var awsCredentials = new Mock<AWSCredentials>();
            _session =  new OrchestratorSession(
                await parser.Parse(fullPath),
                awsCredentials.Object,
                "us-west-2",
                "123456789012")
            {
                AWSProfileName = "default"
            };

            return new RecommendationEngine(_session, _recipeHandler);
        }

        [Fact]
        public async Task WebAppNoDockerFileTest()
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            recommendations
                .Any(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_LINUX_RECIPE_ID)
                .ShouldBeTrue("Failed to receive Recommendation: " + Constants.ASPNET_CORE_BEANSTALK_LINUX_RECIPE_ID);

            recommendations
                .Any(r => r.Recipe.Id == Constants.ASPNET_CORE_ASPNET_CORE_FARGATE_RECIPE_ID)
                .ShouldBeTrue("Failed to receive Recommendation: " + Constants.ASPNET_CORE_ASPNET_CORE_FARGATE_RECIPE_ID);
        }

        [Fact]
        public async Task WebApiNET6()
        {
            var engine = await BuildRecommendationEngine("WebApiNET6");

            var recommendations = await engine.ComputeRecommendations();

            recommendations
                .Any(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_LINUX_RECIPE_ID)
                .ShouldBeTrue("Failed to receive Recommendation: " + Constants.ASPNET_CORE_BEANSTALK_LINUX_RECIPE_ID);

            recommendations
                .Any(r => r.Recipe.Id == Constants.ASPNET_CORE_ASPNET_CORE_FARGATE_RECIPE_ID)
                .ShouldBeTrue("Failed to receive Recommendation: " + Constants.ASPNET_CORE_ASPNET_CORE_FARGATE_RECIPE_ID);

            recommendations
                .Any(r => r.Recipe.Id == Constants.ASPNET_CORE_APPRUNNER_ID)
                .ShouldBeTrue("Failed to receive Recommendation: " + Constants.ASPNET_CORE_APPRUNNER_ID);
        }

        [Fact]
        public async Task WebAppWithDockerFileTest()
        {

            var engine = await BuildRecommendationEngine("WebAppWithDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            recommendations
                .Any(r => r.Recipe.Id == Constants.ASPNET_CORE_ASPNET_CORE_FARGATE_RECIPE_ID)
                .ShouldBeTrue("Failed to receive Recommendation: " + Constants.ASPNET_CORE_ASPNET_CORE_FARGATE_RECIPE_ID);

            recommendations
                .Any(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_LINUX_RECIPE_ID)
                .ShouldBeTrue("Failed to receive Recommendation: " + Constants.ASPNET_CORE_BEANSTALK_LINUX_RECIPE_ID);
        }

        [Fact]
        public async Task MessageProcessingAppTest()
        {
            var projectPath = SystemIOUtilities.ResolvePath("MessageProcessingApp");

            var engine = await BuildRecommendationEngine("MessageProcessingApp");

            var recommendations = await engine.ComputeRecommendations();

            recommendations
                .Any(r => r.Recipe.Id == Constants.CONSOLE_APP_FARGATE_SERVICE_RECIPE_ID)
                .ShouldBeTrue("Failed to receive Recommendation: " + Constants.CONSOLE_APP_FARGATE_SERVICE_RECIPE_ID);

            recommendations
                .Any(r => r.Recipe.Id == Constants.CONSOLE_APP_FARGATE_SCHEDULE_TASK_RECIPE_ID)
                .ShouldBeTrue("Failed to receive Recommendation: " + Constants.CONSOLE_APP_FARGATE_SCHEDULE_TASK_RECIPE_ID);
        }

        [Theory]
        [InlineData("BlazorWasm31")]
        [InlineData("BlazorWasm50")]
        public async Task BlazorWasmTest(string projectName)
        {
            var engine = await BuildRecommendationEngine(projectName);

            var recommendations = await engine.ComputeRecommendations();

            var blazorRecommendation = recommendations.FirstOrDefault(r => r.Recipe.Id == Constants.BLAZOR_WASM);

            Assert.NotNull(blazorRecommendation);
        }

        [Fact]
        public async Task WorkerServiceTest()
        {
            var engine = await BuildRecommendationEngine("WorkerServiceExample");

            var recommendations = await engine.ComputeRecommendations();

            Assert.Single(recommendations);
            recommendations
                .Any(r => r.Recipe.Id == Constants.CONSOLE_APP_FARGATE_SERVICE_RECIPE_ID)
                .ShouldBeTrue("Failed to receive Recommendation: " + Constants.ASPNET_CORE_BEANSTALK_LINUX_RECIPE_ID);
        }


        [Fact]
        public async Task ValueMappingWithDefaultValue()
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_LINUX_RECIPE_ID);
            var environmentTypeOptionSetting = beanstalkRecommendation.Recipe.OptionSettings.First(optionSetting => optionSetting.Id.Equals("EnvironmentType"));

            Assert.Equal("SingleInstance", _optionSettingHandler.GetOptionSettingValue(beanstalkRecommendation, environmentTypeOptionSetting));
        }

        [Fact]
        public async Task ResetOptionSettingValue_Int()
        {
            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string>
            {
                "<reset>"
            });

            var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager, _optionSettingHandler);

            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var fargateRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_ASPNET_CORE_FARGATE_RECIPE_ID);
            var desiredCountOptionSetting = fargateRecommendation.Recipe.OptionSettings.First(optionSetting => optionSetting.Id.Equals("DesiredCount"));

            var originalDefaultValue = _optionSettingHandler.GetOptionSettingDefaultValue<int>(fargateRecommendation, desiredCountOptionSetting);

            await _optionSettingHandler.SetOptionSettingValue(fargateRecommendation, desiredCountOptionSetting, 2);

            Assert.Equal(2, _optionSettingHandler.GetOptionSettingValue<int>(fargateRecommendation, desiredCountOptionSetting));

            await _optionSettingHandler.SetOptionSettingValue(fargateRecommendation, desiredCountOptionSetting, consoleUtilities.AskUserForValue("Title", "2", true, originalDefaultValue.ToString()));

            Assert.Equal(originalDefaultValue, _optionSettingHandler.GetOptionSettingValue<int>(fargateRecommendation, desiredCountOptionSetting));
        }

        [Fact]
        public async Task ResetOptionSettingValue_String()
        {
            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string>
            {
                "<reset>"
            });
            var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager, _optionSettingHandler);

            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var fargateRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_ASPNET_CORE_FARGATE_RECIPE_ID);
            fargateRecommendation.AddReplacementToken("{StackName}", "MyAppStack");

            var ecsServiceNameOptionSetting = fargateRecommendation.Recipe.OptionSettings.First(optionSetting => optionSetting.Id.Equals("ECSServiceName"));

            var originalDefaultValue = _optionSettingHandler.GetOptionSettingDefaultValue<string>(fargateRecommendation, ecsServiceNameOptionSetting);

            await _optionSettingHandler.SetOptionSettingValue(fargateRecommendation, ecsServiceNameOptionSetting, "TestService");

            Assert.Equal("TestService", _optionSettingHandler.GetOptionSettingValue<string>(fargateRecommendation, ecsServiceNameOptionSetting));

            await _optionSettingHandler.SetOptionSettingValue(fargateRecommendation, ecsServiceNameOptionSetting, consoleUtilities.AskUserForValue("Title", "TestService", true, originalDefaultValue));

            Assert.Equal(originalDefaultValue, _optionSettingHandler.GetOptionSettingValue<string>(fargateRecommendation, ecsServiceNameOptionSetting));
        }

        [Fact]
        public async Task ObjectMappingWithDefaultValue()
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_LINUX_RECIPE_ID);
            var applicationIAMRoleOptionSetting = beanstalkRecommendation.Recipe.OptionSettings.First(optionSetting => optionSetting.Id.Equals("ApplicationIAMRole"));

            var iamRoleTypeHintResponse = _optionSettingHandler.GetOptionSettingValue<IAMRoleTypeHintResponse>(beanstalkRecommendation, applicationIAMRoleOptionSetting);

            Assert.Null(iamRoleTypeHintResponse.RoleArn);
            Assert.True(iamRoleTypeHintResponse.CreateNew);
        }

        [Fact]
        public async Task ObjectMappingWithoutDefaultValue()
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_LINUX_RECIPE_ID);
            var applicationIAMRoleOptionSetting = beanstalkRecommendation.Recipe.OptionSettings.First(optionSetting => optionSetting.Id.Equals("ApplicationIAMRole"));

            Assert.Null(_optionSettingHandler.GetOptionSettingDefaultValue(beanstalkRecommendation, applicationIAMRoleOptionSetting));
        }

        [Fact]
        public async Task ValueMappingSetWithValue()
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_LINUX_RECIPE_ID);
            var environmentTypeOptionSetting = beanstalkRecommendation.Recipe.OptionSettings.First(optionSetting => optionSetting.Id.Equals("EnvironmentType"));

            await _optionSettingHandler.SetOptionSettingValue(beanstalkRecommendation, environmentTypeOptionSetting, "LoadBalanced");
            Assert.Equal("LoadBalanced", _optionSettingHandler.GetOptionSettingValue(beanstalkRecommendation, environmentTypeOptionSetting));
        }

        [Fact]
        public async Task ObjectMappingSetWithValue()
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_LINUX_RECIPE_ID);
            var applicationIAMRoleOptionSetting = beanstalkRecommendation.Recipe.OptionSettings.First(optionSetting => optionSetting.Id.Equals("ApplicationIAMRole"));

            await _optionSettingHandler.SetOptionSettingValue(beanstalkRecommendation, applicationIAMRoleOptionSetting, new IAMRoleTypeHintResponse {CreateNew = false,
                RoleArn = "arn:aws:iam::123456789012:group/Developers" });

            var iamRoleTypeHintResponse = _optionSettingHandler.GetOptionSettingValue<IAMRoleTypeHintResponse>(beanstalkRecommendation, applicationIAMRoleOptionSetting);

            Assert.Equal("arn:aws:iam::123456789012:group/Developers", iamRoleTypeHintResponse.RoleArn);
            Assert.False(iamRoleTypeHintResponse.CreateNew);
        }

        [Fact]
        public async Task ApplyProjectNameToSettings()
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var beanstalkRecommendation = recommendations.FirstOrDefault(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_LINUX_RECIPE_ID);

            var beanstalEnvNameSetting = _optionSettingHandler.GetOptionSetting(beanstalkRecommendation, "BeanstalkEnvironment.EnvironmentName");

            beanstalkRecommendation.AddReplacementToken("{StackName}", "MyAppStack");
            Assert.Equal("MyAppStack-dev", _optionSettingHandler.GetOptionSettingValue<string>(beanstalkRecommendation, beanstalEnvNameSetting));

            beanstalkRecommendation.AddReplacementToken("{StackName}", "CustomAppStack");
            Assert.Equal("CustomAppStack-dev", _optionSettingHandler.GetOptionSettingValue<string>(beanstalkRecommendation, beanstalEnvNameSetting));
        }

        [Fact]
        public async Task GetKeyValueOptionSettingServerMode()
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var beanstalkRecommendation = recommendations.FirstOrDefault(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_LINUX_RECIPE_ID);

            var envVarsSetting = _optionSettingHandler.GetOptionSetting(beanstalkRecommendation, "ElasticBeanstalkEnvironmentVariables");

            Assert.Equal(OptionSettingValueType.KeyValue, envVarsSetting.Type);
        }

        [Fact]
        public async Task GetKeyValueOptionSettingConfigFile()
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var beanstalkRecommendation = recommendations.FirstOrDefault(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_LINUX_RECIPE_ID);

            var envVarsSetting = _optionSettingHandler.GetOptionSetting(beanstalkRecommendation, "ElasticBeanstalkEnvironmentVariables.Key");

            Assert.Equal(OptionSettingValueType.KeyValue, envVarsSetting.Type);
        }

        [Theory]
        [MemberData(nameof(ShouldIncludeTestCases))]
        public void ShouldIncludeTests(RuleEffect effect, bool testPass, bool expectedResult)
        {
            var awsCredentials = new Mock<AWSCredentials>();
            var session =  new OrchestratorSession(
                null,
                awsCredentials.Object,
                "us-west-2",
                "123456789012")
            {
                AWSProfileName = "default"
            };
            var engine = new RecommendationEngine(session, _recipeHandler);

            Assert.Equal(expectedResult, engine.ShouldInclude(effect, testPass));
        }

        public static IEnumerable<object[]> ShouldIncludeTestCases =>
            new List<object[]>
            {
                // No effect defined
                new object[]{ new RuleEffect { }, true, true },
                new object[]{ new RuleEffect { }, false, false },

                // Negative Rule
                new object[]{ new RuleEffect { Pass = new EffectOptions {Include = false }, Fail = new EffectOptions { Include = true } }, true, false },
                new object[]{ new RuleEffect { Pass = new EffectOptions {Include = false }, Fail = new EffectOptions { Include = true } }, false, true },

                // Explicitly define effects
                new object[]{ new RuleEffect { Pass = new EffectOptions {Include = true }, Fail = new EffectOptions { Include = false} }, true, true },
                new object[]{ new RuleEffect { Pass = new EffectOptions {Include = true }, Fail = new EffectOptions { Include = false} }, false, false },

                // Positive rule to adjust priority
                new object[]{ new RuleEffect { Pass = new EffectOptions {PriorityAdjustment = 55 } }, true, true },
                new object[]{ new RuleEffect { Pass = new EffectOptions { PriorityAdjustment = 55 }, Fail = new EffectOptions { Include = true } }, false, true },

                // Negative rule to adjust priority
                new object[]{ new RuleEffect { Fail = new EffectOptions {PriorityAdjustment = -55 } }, true, true },
                new object[]{ new RuleEffect { Fail = new EffectOptions { PriorityAdjustment = -55 } }, false, true },
            };

        [Fact]
        public async Task IsDisplayable_OneDependency()
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_LINUX_RECIPE_ID);
            var environmentTypeOptionSetting = beanstalkRecommendation.Recipe.OptionSettings.First(optionSetting => optionSetting.Id.Equals("EnvironmentType"));

            var loadBalancerTypeOptionSetting = beanstalkRecommendation.Recipe.OptionSettings.First(optionSetting => optionSetting.Id.Equals("LoadBalancerType"));

            Assert.Equal("SingleInstance", _optionSettingHandler.GetOptionSettingValue(beanstalkRecommendation, environmentTypeOptionSetting));

            // Before dependency isn't satisfied
            Assert.False(_optionSettingHandler.IsOptionSettingDisplayable(beanstalkRecommendation, loadBalancerTypeOptionSetting));

            // Satisfy dependency
            await _optionSettingHandler.SetOptionSettingValue(beanstalkRecommendation, environmentTypeOptionSetting, "LoadBalanced");
            Assert.Equal("LoadBalanced", _optionSettingHandler.GetOptionSettingValue(beanstalkRecommendation, environmentTypeOptionSetting));

            // Verify
            Assert.True(_optionSettingHandler.IsOptionSettingDisplayable(beanstalkRecommendation, loadBalancerTypeOptionSetting));
        }

        [Fact]
        public async Task IsDisplayable_ManyDependencies()
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var fargateRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_ASPNET_CORE_FARGATE_RECIPE_ID);
            var isDefaultOptionSetting = _optionSettingHandler.GetOptionSetting(fargateRecommendation, "Vpc.IsDefault");
            var createNewOptionSetting = _optionSettingHandler.GetOptionSetting(fargateRecommendation, "Vpc.CreateNew");
            var vpcIdOptionSetting = _optionSettingHandler.GetOptionSetting(fargateRecommendation, "Vpc.VpcId");

            // Before dependency aren't satisfied
            Assert.False(_optionSettingHandler.IsOptionSettingDisplayable(fargateRecommendation, vpcIdOptionSetting));

            // Satisfy dependencies
            await _optionSettingHandler.SetOptionSettingValue(fargateRecommendation, isDefaultOptionSetting, false);
            Assert.False(_optionSettingHandler.GetOptionSettingValue<bool>(fargateRecommendation, isDefaultOptionSetting));

            // Default value for Vpc.CreateNew already false, this is to show explicitly setting an override that satisfies Vpc Id option setting
            await _optionSettingHandler.SetOptionSettingValue(fargateRecommendation, createNewOptionSetting, false);
            Assert.False(_optionSettingHandler.GetOptionSettingValue<bool>(fargateRecommendation, createNewOptionSetting));

            // Verify
            Assert.True(_optionSettingHandler.IsOptionSettingDisplayable(fargateRecommendation, vpcIdOptionSetting));
        }

        [Fact]
        public async Task IsDisplayable_NotEmptyOperation()
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_LINUX_RECIPE_ID);
            var useVpcOptionSetting = _optionSettingHandler.GetOptionSetting(beanstalkRecommendation, "VPC.UseVPC");
            var createNewOptionSetting = _optionSettingHandler.GetOptionSetting(beanstalkRecommendation, "VPC.CreateNew");
            var vpcIdOptionSetting = _optionSettingHandler.GetOptionSetting(beanstalkRecommendation, "VPC.VpcId");
            var subnetsSetting = _optionSettingHandler.GetOptionSetting(beanstalkRecommendation, "VPC.Subnets");

            // Before dependency aren't satisfied
            Assert.True(_optionSettingHandler.IsOptionSettingDisplayable(beanstalkRecommendation, useVpcOptionSetting));
            Assert.False(_optionSettingHandler.IsOptionSettingDisplayable(beanstalkRecommendation, createNewOptionSetting));
            Assert.False(_optionSettingHandler.IsOptionSettingDisplayable(beanstalkRecommendation, vpcIdOptionSetting));
            Assert.False(_optionSettingHandler.IsOptionSettingDisplayable(beanstalkRecommendation, subnetsSetting));

            // Satisfy dependencies
            await _optionSettingHandler.SetOptionSettingValue(beanstalkRecommendation, useVpcOptionSetting, true);
            await _optionSettingHandler.SetOptionSettingValue(beanstalkRecommendation, createNewOptionSetting, false);
            await _optionSettingHandler.SetOptionSettingValue(beanstalkRecommendation, vpcIdOptionSetting, "vpc-1234abcd");
            Assert.True(_optionSettingHandler.IsOptionSettingDisplayable(beanstalkRecommendation, vpcIdOptionSetting));
            Assert.True(_optionSettingHandler.IsOptionSettingDisplayable(beanstalkRecommendation, subnetsSetting));
        }

        [Fact]
        public void LoadAvailableRecommendationTests()
        {
            var tests = RecommendationTestFactory.LoadAvailableTests();

            Assert.True(tests.Count > 0);

            // Look to see if the known system test FileExists has been found by LoadAvailableTests.
            Assert.Contains(new FileExistsTest().Name, tests);
        }

        [Fact]
        public async Task PackageReferenceTest()
        {
            var projectPath = SystemIOUtilities.ResolvePath("MessageProcessingApp");

            var projectDefinition = await new ProjectDefinitionParser(new FileManager(), new DirectoryManager()).Parse(projectPath);

            var test = new NuGetPackageReferenceTest();

            Assert.True(await test.Execute(new RecommendationTestInput(
                new RuleTest(
                    test.Name,
                    new RuleCondition
                    {
                        NuGetPackageName = "AWSSDK.SQS"
                    }),
                projectDefinition,
                _session)));

            Assert.False(await test.Execute(new RecommendationTestInput(
                new RuleTest(
                    test.Name,
                    new RuleCondition
                    {
                        NuGetPackageName = "AWSSDK.S3"
                    }),
                projectDefinition,
                _session)));
        }
    }
}
