// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
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
using Moq;
using Xunit;

namespace AWS.Deploy.CLI.UnitTests.TypeHintCommands
{
    /// <summary>
    /// Unit tests for <see cref="VpcCommand"/>
    /// </summary>
    public class VpcCommandTest
    {
        private readonly Mock<IAWSResourceQueryer> _mockAWSResourceQueryer;
        private readonly IDirectoryManager _directoryManager;
        private readonly IToolInteractiveService _toolInteractiveService;
        private readonly IOptionSettingHandler _optionSettingHandler;
        private readonly Mock<IServiceProvider> _serviceProvider;

        public VpcCommandTest()
        {
            _mockAWSResourceQueryer = new Mock<IAWSResourceQueryer>();
            _directoryManager = new TestDirectoryManager();
            _toolInteractiveService = new TestToolInteractiveServiceImpl();
            _serviceProvider = new Mock<IServiceProvider>();
            _serviceProvider
                .Setup(x => x.GetService(typeof(IAWSResourceQueryer)))
                .Returns(_mockAWSResourceQueryer.Object);

            _optionSettingHandler = new OptionSettingHandler(new ValidatorFactory(_serviceProvider.Object));

            _mockAWSResourceQueryer.Setup(x => x.GetListOfVpcs())
                 .ReturnsAsync(new List<Vpc>()
                {
                    new Vpc()
                    {
                        IsDefault = true,
                        VpcId = "vpc1"
                    },
                    new Vpc()
                    {
                        VpcId = "vpc-no-subnets"
                    },
                });

            _mockAWSResourceQueryer.Setup(x => x.DescribeSubnets("vpc1"))
                .ReturnsAsync(new List<Subnet>()
                {
                    new Subnet()
                    {
                        SubnetId = "subnet1",
                        VpcId = "vpc1",
                        AvailabilityZone = "us-west-2a"
                    },
                    new Subnet()
                    {
                        SubnetId = "subnet2",
                        VpcId = "vpc1",
                        AvailabilityZone = "us-west-2b"
                    },
                    new Subnet()
                    {
                        SubnetId = "subnet3",
                        VpcId = "vpc1",
                        AvailabilityZone = "us-west-2c"
                    }
                });

            _mockAWSResourceQueryer.Setup(x => x.DescribeSubnets("vpc-no-subnets"))
                .ReturnsAsync(new List<Subnet>());

        }
        /// <summary>
        /// Tests that a user is not prompted to enter subnets when
        /// creating a new VPC while configuring an ECS Fargate recipe.
        /// </summary>
        [Fact]
        public async Task VpcCommand_NewVPC_DoesNotPromptForSubnets()
        {
            var engine = await HelperFunctions.BuildRecommendationEngine(
                "WebAppWithDockerFile",
                new FileManager(),
                new DirectoryManager(),
                "us-west-2",
                "123456789012",
                "default"
                );

            var recommendation = (await engine.ComputeRecommendations()).First(r => r.Recipe.Id == Constants.ASPNET_CORE_ASPNET_CORE_FARGATE_RECIPE_ID);

            var vpcOptionSetting = _optionSettingHandler.GetOptionSetting(recommendation, "Vpc");

            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string>
            {
                "3", // mocked two subnets above, so this is choosing "Create new"
            });
            var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager, _optionSettingHandler);
            var command = new VpcCommand(_mockAWSResourceQueryer.Object, consoleUtilities, _toolInteractiveService);

            var typeHintResponse = await command.Execute(recommendation, vpcOptionSetting);
            var vpcTypeHintResponse = Assert.IsType<VpcTypeHintResponse>(typeHintResponse);

            Assert.True(vpcTypeHintResponse.CreateNew);
            Assert.Empty(vpcTypeHintResponse.Subnets);
        }

        /// <summary>
        /// Tests that when a user can select a subset of an existing
        /// VPC's subnets while configuring an ECS Fargate recipe
        /// </summary>
        [Fact]
        public async Task VpcCommand_ExistingVPC_SelectSomeSubnets()
        {
            var engine = await HelperFunctions.BuildRecommendationEngine(
                "WebAppWithDockerFile",
                new FileManager(),
                new DirectoryManager(),
                "us-west-2",
                "123456789012",
                "default"
                );

            var recommendation = (await engine.ComputeRecommendations()).First(r => r.Recipe.Id == Constants.ASPNET_CORE_ASPNET_CORE_FARGATE_RECIPE_ID);

            var vpcOptionSetting = _optionSettingHandler.GetOptionSetting(recommendation, "Vpc");

            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string>
            {
                "1", // Selecting the default VPC
                "1", // "Add new"
                "1", // Selecting the first subnet
                "1", // "Add new"
                "3", // Selecting the third subnet
                "3"  // "No action" to exit

            });
            var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager, _optionSettingHandler);
            var command = new VpcCommand(_mockAWSResourceQueryer.Object, consoleUtilities, _toolInteractiveService);

            var typeHintResponse = await command.Execute(recommendation, vpcOptionSetting);
            var vpcTypeHintResponse = Assert.IsType<VpcTypeHintResponse>(typeHintResponse);

            Assert.False(vpcTypeHintResponse.CreateNew);
            Assert.True(vpcTypeHintResponse.IsDefault);
            Assert.Equal("vpc1", vpcTypeHintResponse.VpcId);

            Assert.Collection(vpcTypeHintResponse.Subnets,
                subnetId => Assert.Equal("subnet1", subnetId),
                subnetId => Assert.Equal("subnet3", subnetId));
        }

        /// <summary>
        /// Tests that if a user selected a VPC without any subnets
        /// while configuring an ECS Fargate recipe, it resets back
        /// to creating a new VPC.
        /// </summary>
        [Fact]
        public async Task VpcCommand_VPCNoSubnets_ResetsToCreate()
        {
            var engine = await HelperFunctions.BuildRecommendationEngine(
                "WebAppWithDockerFile",
                new FileManager(),
                new DirectoryManager(),
                "us-west-2",
                "123456789012",
                "default"
                );

            var recommendation = (await engine.ComputeRecommendations()).First(r => r.Recipe.Id == Constants.ASPNET_CORE_ASPNET_CORE_FARGATE_RECIPE_ID);

            var vpcOptionSetting = _optionSettingHandler.GetOptionSetting(recommendation, "Vpc");

            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string>
            {
                "2", // Selecting the VPC without subnets
            });

            var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager, _optionSettingHandler);
            var command = new VpcCommand(_mockAWSResourceQueryer.Object, consoleUtilities, _toolInteractiveService);

            var typeHintResponse = await command.Execute(recommendation, vpcOptionSetting);
            var vpcTypeHintResponse = Assert.IsType<VpcTypeHintResponse>(typeHintResponse);

            Assert.True(vpcTypeHintResponse.CreateNew);
            Assert.Empty(vpcTypeHintResponse.Subnets);
        }
    }
}

