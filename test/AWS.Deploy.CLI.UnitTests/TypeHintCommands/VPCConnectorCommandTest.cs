// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.AppRunner.Model;
using Amazon.EC2.Model;
using AWS.Deploy.CLI.Commands.TypeHints;
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.CLI.TypeHintResponses;
using AWS.Deploy.CLI.UnitTests.Utilities;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Orchestration.Data;
using Moq;
using Xunit;

namespace AWS.Deploy.CLI.UnitTests.TypeHintCommands
{
    public class VPCConnectorCommandTest
    {
        private readonly Mock<IAWSResourceQueryer> _mockAWSResourceQueryer;
        private readonly IDirectoryManager _directoryManager;
        private readonly IToolInteractiveService _toolInteractiveService;

        public VPCConnectorCommandTest()
        {
            _mockAWSResourceQueryer = new Mock<IAWSResourceQueryer>();
            _directoryManager = new TestDirectoryManager();
            _toolInteractiveService = new TestToolInteractiveServiceImpl();
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

            var vpcConnectorOptionSetting = appRunnerRecommendation.GetOptionSetting("VPCConnector");

            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string>());
            var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager);
            var command = new VPCConnectorCommand(_mockAWSResourceQueryer.Object, consoleUtilities, _toolInteractiveService);

            _mockAWSResourceQueryer
                .Setup(x => x.DescribeAppRunnerVpcConnectors())
                .ReturnsAsync(new List<VpcConnector>()
                {
                    new VpcConnector()
                    {
                        VpcConnectorArn = "arn:aws:apprunner:us-west-2:123456789010:vpcconnector/fakeVpcConnector",
                        VpcConnectorName = "vpcConnectorName"
                    }
                });

            var resources = await command.GetResources(appRunnerRecommendation, vpcConnectorOptionSetting);

            Assert.Single(resources);
            Assert.Equal("vpcConnectorName", resources[0].DisplayName);
            Assert.Equal("arn:aws:apprunner:us-west-2:123456789010:vpcconnector/fakeVpcConnector", resources[0].SystemName);
        }

        [Fact]
        public async Task Execute_ExistingVPCConnector()
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

            var vpcConnectorOptionSetting = appRunnerRecommendation.GetOptionSetting("VPCConnector");

            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string>
            {
                "n",
                "1"
            });
            var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager);
            var command = new VPCConnectorCommand(_mockAWSResourceQueryer.Object, consoleUtilities, _toolInteractiveService);

            _mockAWSResourceQueryer
                .Setup(x => x.DescribeAppRunnerVpcConnectors())
                .ReturnsAsync(new List<VpcConnector>()
                {
                    new VpcConnector()
                    {
                        VpcConnectorArn = "arn:aws:apprunner:us-west-2:123456789010:vpcconnector/fakeVpcConnector",
                        VpcConnectorName = "vpcConnectorName"
                    }
                });

            var typeHintResponse = await command.Execute(appRunnerRecommendation, vpcConnectorOptionSetting);

            var vpcConnectorTypeHintResponse = Assert.IsType<VPCConnectorTypeHintResponse>(typeHintResponse);
            Assert.False(vpcConnectorTypeHintResponse.CreateNew);
            Assert.Equal("arn:aws:apprunner:us-west-2:123456789010:vpcconnector/fakeVpcConnector", vpcConnectorTypeHintResponse.VpcConnectorId);
            Assert.Empty(vpcConnectorTypeHintResponse.Subnets);
            Assert.Empty(vpcConnectorTypeHintResponse.SecurityGroups);
        }

        [Fact]
        public async Task Execute_NewVPCConnector()
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

            var vpcConnectorOptionSetting = appRunnerRecommendation.GetOptionSetting("VPCConnector");

            var interactiveServices = new TestToolInteractiveServiceImpl(new List<string>
            {
                "y",
                "1",
                "1",
                "1",
                "3",
                "1",
                "1",
                "3"
            });
            var consoleUtilities = new ConsoleUtilities(interactiveServices, _directoryManager);
            var command = new VPCConnectorCommand(_mockAWSResourceQueryer.Object, consoleUtilities, _toolInteractiveService);

            _mockAWSResourceQueryer
                .Setup(x => x.GetListOfVpcs())
                .ReturnsAsync(new List<Vpc>()
                {
                    new Vpc()
                    {
                        VpcId = "vpc1"
                    }
                });

            _mockAWSResourceQueryer
                .Setup(x => x.DescribeSubnets("vpc1"))
                .ReturnsAsync(new List<Subnet>()
                {
                    new Subnet()
                    {
                        SubnetId = "subnet1",
                        VpcId = "vpc1",
                        AvailabilityZone = "us-west-2"
                    }
                });

            _mockAWSResourceQueryer
                .Setup(x => x.DescribeSecurityGroups("vpc1"))
                .ReturnsAsync(new List<SecurityGroup>()
                {
                    new SecurityGroup()
                    {
                        GroupId = "group1",
                        GroupName = "groupName1",
                        VpcId = "vpc1"
                    }
                });

            var typeHintResponse = await command.Execute(appRunnerRecommendation, vpcConnectorOptionSetting);

            var vpcConnectorTypeHintResponse = Assert.IsType<VPCConnectorTypeHintResponse>(typeHintResponse);
            Assert.True(vpcConnectorTypeHintResponse.CreateNew);
            Assert.Null(vpcConnectorTypeHintResponse.VpcConnectorId);
            Assert.Single(vpcConnectorTypeHintResponse.Subnets);
            Assert.Contains("subnet1", vpcConnectorTypeHintResponse.Subnets);
            Assert.Single(vpcConnectorTypeHintResponse.SecurityGroups);
            Assert.Contains("group1", vpcConnectorTypeHintResponse.SecurityGroups);
        }
    }
}
