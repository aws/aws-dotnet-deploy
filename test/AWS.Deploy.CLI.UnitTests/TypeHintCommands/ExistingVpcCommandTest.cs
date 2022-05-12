// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.EC2.Model;
using AWS.Deploy.CLI.Commands.TypeHints;
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.CLI.UnitTests.Utilities;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Recipes.Validation;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.Data;
using Moq;
using Xunit;

namespace AWS.Deploy.CLI.UnitTests.TypeHintCommands
{
    public class ExistingVpcCommandTest
    {
        private readonly Mock<IAWSResourceQueryer> _mockAWSResourceQueryer;
        private readonly IDirectoryManager _directoryManager;
        private readonly IOptionSettingHandler _optionSettingHandler;
        private readonly IServiceProvider _serviceProvider;

        public ExistingVpcCommandTest()
        {
            _mockAWSResourceQueryer = new Mock<IAWSResourceQueryer>();
            _directoryManager = new TestDirectoryManager();
            _serviceProvider = new Mock<IServiceProvider>().Object;
            _optionSettingHandler = new OptionSettingHandler(new ValidatorFactory(_serviceProvider));
        }

        [Fact]
        public async Task GetResources()
        {
            var engine = await HelperFunctions.BuildRecommendationEngine(
                "WebAppNoDockerFile",
                new FileManager(),
                new DirectoryManager(),
                "us-west-2",
                "123456789012",
                "default"
                );

            var recommendations = await engine.ComputeRecommendations();

            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID);

            var vpcOptionSetting = _optionSettingHandler.GetOptionSetting(beanstalkRecommendation, "VpcId");

            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string>());
            var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager, _optionSettingHandler);
            var command = new ExistingVpcCommand(_mockAWSResourceQueryer.Object, consoleUtilities, _optionSettingHandler);

            _mockAWSResourceQueryer
                .Setup(x => x.GetListOfVpcs())
                .ReturnsAsync(new List<Vpc>()
                {
                    new Vpc()
                    {
                        VpcId = "vpc1"
                    }
                });

            var resources = await command.GetResources(beanstalkRecommendation, vpcOptionSetting);

            Assert.Single(resources);
            Assert.Equal("vpc1", resources[0].DisplayName);
            Assert.Equal("vpc1", resources[0].SystemName);
        }

        [Fact]
        public async Task Execute()
        {
            var engine = await HelperFunctions.BuildRecommendationEngine(
                "WebAppNoDockerFile",
                new FileManager(),
                new DirectoryManager(),
                "us-west-2",
                "123456789012",
                "default"
            );

            var recommendations = await engine.ComputeRecommendations();

            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID);

            var vpcOptionSetting = _optionSettingHandler.GetOptionSetting(beanstalkRecommendation, "VpcId");

            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string>
            {
                "2"
            });
            var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager, _optionSettingHandler);
            var command = new ExistingVpcCommand(_mockAWSResourceQueryer.Object, consoleUtilities, _optionSettingHandler);

            _mockAWSResourceQueryer
                .Setup(x => x.GetListOfVpcs())
                .ReturnsAsync(new List<Vpc>()
                {
                    new Vpc()
                    {
                        VpcId = "vpc1"
                    }
                });

            var typeHintResponse = await command.Execute(beanstalkRecommendation, vpcOptionSetting);

            var stringResponse = Assert.IsType<string>(typeHintResponse);
            Assert.Equal("vpc1", stringResponse);
        }
    }
}
