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
    public class ExistingSecurityGroubsCommandTest
    {
        private readonly Mock<IAWSResourceQueryer> _mockAWSResourceQueryer;
        private readonly IDirectoryManager _directoryManager;
        private readonly IOptionSettingHandler _optionSettingHandler;
        private readonly Mock<IAWSResourceQueryer> _awsResourceQueryer;
        private readonly Mock<IServiceProvider> _serviceProvider;

        public ExistingSecurityGroubsCommandTest()
        {
            _mockAWSResourceQueryer = new Mock<IAWSResourceQueryer>();
            _directoryManager = new TestDirectoryManager();
            _awsResourceQueryer = new Mock<IAWSResourceQueryer>();
            _serviceProvider = new Mock<IServiceProvider>();
            _serviceProvider
                .Setup(x => x.GetService(typeof(IAWSResourceQueryer)))
                .Returns(_awsResourceQueryer.Object);
            _optionSettingHandler = new OptionSettingHandler(new ValidatorFactory(_serviceProvider.Object));
        }

        [Fact]
        public async Task GetResources()
        {
            var engine = await HelperFunctions.BuildRecommendationEngine(
                "WebAppWithDockerFile",
                new FileManager(),
                new DirectoryManager(),
                "us-west-2",
                "123456789012",
                "default"
                );

            var recommendations = await engine.ComputeRecommendations();

            var appRunnerRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_APPRUNNER_ID);

            var securityGroupsOptionSetting = _optionSettingHandler.GetOptionSetting(appRunnerRecommendation, "VPCConnector.SecurityGroups");

            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string>());
            var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager, _optionSettingHandler);
            var command = new ExistingSecurityGroupsCommand(_mockAWSResourceQueryer.Object, consoleUtilities, _optionSettingHandler);

            _mockAWSResourceQueryer
                .Setup(x => x.DescribeSecurityGroups(It.IsAny<string>()))
                .ReturnsAsync(new List<SecurityGroup>()
                {
                    new SecurityGroup()
                    {
                        GroupId = "group1"
                    }
                });

            var resources = await command.GetResources(appRunnerRecommendation, securityGroupsOptionSetting);

            Assert.Single(resources);
            Assert.Equal("group1", resources[0].DisplayName);
            Assert.Equal("group1", resources[0].SystemName);
        }

        [Fact]
        public async Task Execute()
        {
            var engine = await HelperFunctions.BuildRecommendationEngine(
                "WebAppWithDockerFile",
                new FileManager(),
                new DirectoryManager(),
                "us-west-2",
                "123456789012",
                "default"
            );

            var recommendations = await engine.ComputeRecommendations();

            var appRunnerRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_APPRUNNER_ID);

            var securityGroupsOptionSetting = _optionSettingHandler.GetOptionSetting(appRunnerRecommendation, "VPCConnector.SecurityGroups");

            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string>
            {
                "1",
                "1",
                "3"
            });
            var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager, _optionSettingHandler);
            var command = new ExistingSecurityGroupsCommand(_mockAWSResourceQueryer.Object, consoleUtilities, _optionSettingHandler);

            _mockAWSResourceQueryer
                .Setup(x => x.DescribeSecurityGroups(It.IsAny<string>()))
                .ReturnsAsync(new List<SecurityGroup>()
                {
                    new SecurityGroup()
                    {
                        GroupId = "group1",
                        GroupName = "groupName1",
                        VpcId = "vpc1"
                    }
                });

            var typeHintResponse = await command.Execute(appRunnerRecommendation, securityGroupsOptionSetting);

            var sortedSetResponse = Assert.IsType<SortedSet<string>>(typeHintResponse);
            Assert.Single(sortedSetResponse);
            Assert.Contains("group1", sortedSetResponse);
        }
    }
}
