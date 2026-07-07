// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.EC2.Model;
using AWS.Deploy.CLI.Commands.TypeHints;
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.CLI.TypeHintResponses;
using AWS.Deploy.CLI.UnitTests.Utilities;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Recipes.Validation;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.Data;
using Moq;
using Xunit;

namespace AWS.Deploy.CLI.UnitTests.TypeHintCommands
{
    public class AgentCoreVpcCommandTest
    {
        private readonly Mock<IAWSResourceQueryer> _mockAWSResourceQueryer;
        private readonly IDirectoryManager _directoryManager;
        private readonly IToolInteractiveService _toolInteractiveService;
        private readonly IOptionSettingHandler _optionSettingHandler;

        public AgentCoreVpcCommandTest()
        {
            _mockAWSResourceQueryer = new Mock<IAWSResourceQueryer>();
            _directoryManager = new TestDirectoryManager();
            _toolInteractiveService = new TestToolInteractiveServiceImpl();
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(x => x.GetService(typeof(IAWSResourceQueryer)))
                .Returns(_mockAWSResourceQueryer.Object);
            _optionSettingHandler = new OptionSettingHandler(new ValidatorFactory(serviceProvider.Object));
        }

        [Fact]
        public async Task GetResources_ReturnsAvailableVpcs()
        {
            var engine = await BuildRecommendationEngine();
            var recommendations = await engine.ComputeRecommendations();
            var recommendation = recommendations.First(r => r.Recipe.Id == "AspNetAppBedrockAgentCore");
            var vpcOptionSetting = _optionSettingHandler.GetOptionSetting(recommendation, "VPC");

            _mockAWSResourceQueryer
                .Setup(x => x.GetListOfVpcs())
                .ReturnsAsync(new List<Vpc>
                {
                    new Vpc { VpcId = "vpc-111", IsDefault = true },
                    new Vpc { VpcId = "vpc-222", IsDefault = false }
                });

            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string>());
            var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager, _optionSettingHandler);
            var command = new AgentCoreVpcCommand(_mockAWSResourceQueryer.Object, consoleUtilities, _toolInteractiveService);

            var resources = await command.GetResources(recommendation, vpcOptionSetting);

            Assert.Equal(2, resources.Rows.Count);
        }

        [Fact]
        public async Task Execute_UserSelectsNo_ReturnsNotUsingVpc()
        {
            var engine = await BuildRecommendationEngine();
            var recommendations = await engine.ComputeRecommendations();
            var recommendation = recommendations.First(r => r.Recipe.Id == "AspNetAppBedrockAgentCore");
            var vpcOptionSetting = _optionSettingHandler.GetOptionSetting(recommendation, "VPC");

            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string>
            {
                "n"
            });
            var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager, _optionSettingHandler);
            var command = new AgentCoreVpcCommand(_mockAWSResourceQueryer.Object, consoleUtilities, interactiveServices);

            var result = await command.Execute(recommendation, vpcOptionSetting);

            var response = Assert.IsType<AgentCoreVpcTypeHintResponse>(result);
            Assert.False(response.UseVPC);
            Assert.False(response.CreateNew);
            Assert.Null(response.VpcId);
            Assert.Equal("*** Not using VPC ***", response.ToDisplayString());
        }

        [Fact]
        public async Task Execute_UserSelectsYes_NoVpcsExist_ReturnsCreateNew()
        {
            var engine = await BuildRecommendationEngine();
            var recommendations = await engine.ComputeRecommendations();
            var recommendation = recommendations.First(r => r.Recipe.Id == "AspNetAppBedrockAgentCore");
            var vpcOptionSetting = _optionSettingHandler.GetOptionSetting(recommendation, "VPC");

            _mockAWSResourceQueryer
                .Setup(x => x.GetListOfVpcs())
                .ReturnsAsync(new List<Vpc>());

            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string>
            {
                "y"
            });
            var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager, _optionSettingHandler);
            var command = new AgentCoreVpcCommand(_mockAWSResourceQueryer.Object, consoleUtilities, interactiveServices);

            var result = await command.Execute(recommendation, vpcOptionSetting);

            var response = Assert.IsType<AgentCoreVpcTypeHintResponse>(result);
            Assert.True(response.UseVPC);
            Assert.True(response.CreateNew);
            Assert.Equal("*** Create new VPC ***", response.ToDisplayString());
        }

        [Fact]
        public async Task Execute_UserSelectsYes_ChoosesExistingVpc()
        {
            var engine = await BuildRecommendationEngine();
            var recommendations = await engine.ComputeRecommendations();
            var recommendation = recommendations.First(r => r.Recipe.Id == "AspNetAppBedrockAgentCore");
            var vpcOptionSetting = _optionSettingHandler.GetOptionSetting(recommendation, "VPC");

            _mockAWSResourceQueryer
                .Setup(x => x.GetListOfVpcs())
                .ReturnsAsync(new List<Vpc>
                {
                    new Vpc { VpcId = "vpc-abc123", IsDefault = true }
                });

            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string>
            {
                "y",
                "1"
            });
            var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager, _optionSettingHandler);
            var command = new AgentCoreVpcCommand(_mockAWSResourceQueryer.Object, consoleUtilities, interactiveServices);

            var result = await command.Execute(recommendation, vpcOptionSetting);

            var response = Assert.IsType<AgentCoreVpcTypeHintResponse>(result);
            Assert.True(response.UseVPC);
            Assert.False(response.CreateNew);
            Assert.Equal("vpc-abc123", response.VpcId);
            Assert.Equal("vpc-abc123", response.ToDisplayString());
        }

        [Fact]
        public async Task Execute_UserSelectsYes_ChoosesCreateNew()
        {
            var engine = await BuildRecommendationEngine();
            var recommendations = await engine.ComputeRecommendations();
            var recommendation = recommendations.First(r => r.Recipe.Id == "AspNetAppBedrockAgentCore");
            var vpcOptionSetting = _optionSettingHandler.GetOptionSetting(recommendation, "VPC");

            _mockAWSResourceQueryer
                .Setup(x => x.GetListOfVpcs())
                .ReturnsAsync(new List<Vpc>
                {
                    new Vpc { VpcId = "vpc-abc123", IsDefault = true }
                });

            // "y" to use VPC, "2" to select "Create new" (option after the one VPC)
            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string>
            {
                "y",
                "2"
            });
            var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager, _optionSettingHandler);
            var command = new AgentCoreVpcCommand(_mockAWSResourceQueryer.Object, consoleUtilities, interactiveServices);

            var result = await command.Execute(recommendation, vpcOptionSetting);

            var response = Assert.IsType<AgentCoreVpcTypeHintResponse>(result);
            Assert.True(response.UseVPC);
            Assert.True(response.CreateNew);
            Assert.Equal("*** Create new VPC ***", response.ToDisplayString());
        }

        private static async Task<Orchestration.RecommendationEngine.RecommendationEngine> BuildRecommendationEngine()
        {
            return await HelperFunctions.BuildRecommendationEngine(
                "AgentCoreWebApp",
                new FileManager(),
                new DirectoryManager(),
                "us-west-2",
                "123456789012",
                "default");
        }
    }
}
