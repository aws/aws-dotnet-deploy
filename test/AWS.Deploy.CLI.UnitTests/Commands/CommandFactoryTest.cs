// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using Amazon.Runtime;
using System.Threading.Tasks;
using AWS.Deploy.CLI.Commands;
using AWS.Deploy.CLI.Commands.CommandHandlerInput;
using AWS.Deploy.CLI.Commands.TypeHints;
using AWS.Deploy.CLI.Utilities;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Common.DeploymentManifest;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Recipes.Validation;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.CDK;
using AWS.Deploy.Orchestration.DisplayedResources;
using AWS.Deploy.Orchestration.LocalUserSettings;
using AWS.Deploy.Orchestration.ServiceHandlers;
using AWS.Deploy.Orchestration.Utilities;
using Moq;
using Xunit;
using Amazon.SecurityToken.Model;
using System.Collections.Generic;
using Amazon.Extensions.NETCore.Setup;

namespace AWS.Deploy.CLI.UnitTests
{
    public class CommandFactoryTests
    {
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IToolInteractiveService> _mockToolInteractiveService;
        private readonly Mock<IOrchestratorInteractiveService> _mockOrchestratorInteractiveService;
        private readonly Mock<ICDKManager> _mockCdkManager;
        private readonly Mock<ISystemCapabilityEvaluator> _mockSystemCapabilityEvaluator;
        private readonly Mock<ICloudApplicationNameGenerator> _mockCloudApplicationNameGenerator;
        private readonly Mock<IAWSUtilities> _mockAwsUtilities;
        private readonly Mock<IAWSClientFactory> _mockAwsClientFactory;
        private readonly Mock<IAWSResourceQueryer> _mockAwsResourceQueryer;
        private readonly Mock<IProjectParserUtility> _mockProjectParserUtility;
        private readonly Mock<ICommandLineWrapper> _mockCommandLineWrapper;
        private readonly Mock<ICdkProjectHandler> _mockCdkProjectHandler;
        private readonly Mock<IDeploymentBundleHandler> _mockDeploymentBundleHandler;
        private readonly Mock<ICloudFormationTemplateReader> _mockCloudFormationTemplateReader;
        private readonly Mock<IDeployedApplicationQueryer> _mockDeployedApplicationQueryer;
        private readonly Mock<ITypeHintCommandFactory> _mockTypeHintCommandFactory;
        private readonly Mock<IDisplayedResourcesHandler> _mockDisplayedResourceHandler;
        private readonly Mock<IConsoleUtilities> _mockConsoleUtilities;
        private readonly Mock<IDirectoryManager> _mockDirectoryManager;
        private readonly Mock<IFileManager> _mockFileManager;
        private readonly Mock<IDeploymentManifestEngine> _mockDeploymentManifestEngine;
        private readonly Mock<ILocalUserSettingsEngine> _mockLocalUserSettingsEngine;
        private readonly Mock<ICDKVersionDetector> _mockCdkVersionDetector;
        private readonly Mock<IAWSServiceHandler> _mockAwsServiceHandler;
        private readonly Mock<IOptionSettingHandler> _mockOptionSettingHandler;
        private readonly Mock<IValidatorFactory> _mockValidatorFactory;
        private readonly Mock<IRecipeHandler> _mockRecipeHandler;
        private readonly Mock<IDeployToolWorkspaceMetadata> _mockDeployToolWorkspaceMetadata;
        private readonly Mock<IDeploymentSettingsHandler> _mockDeploymentSettingsHandler;

        public CommandFactoryTests()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockToolInteractiveService = new Mock<IToolInteractiveService>();
            _mockOrchestratorInteractiveService = new Mock<IOrchestratorInteractiveService>();
            _mockCdkManager = new Mock<ICDKManager>();
            _mockSystemCapabilityEvaluator = new Mock<ISystemCapabilityEvaluator>();
            _mockCloudApplicationNameGenerator = new Mock<ICloudApplicationNameGenerator>();
            _mockAwsUtilities = new Mock<IAWSUtilities>();
            _mockAwsClientFactory = new Mock<IAWSClientFactory>();
            _mockAwsResourceQueryer = new Mock<IAWSResourceQueryer>();
            _mockProjectParserUtility = new Mock<IProjectParserUtility>();
            _mockCommandLineWrapper = new Mock<ICommandLineWrapper>();
            _mockCdkProjectHandler = new Mock<ICdkProjectHandler>();
            _mockDeploymentBundleHandler = new Mock<IDeploymentBundleHandler>();
            _mockCloudFormationTemplateReader = new Mock<ICloudFormationTemplateReader>();
            _mockDeployedApplicationQueryer = new Mock<IDeployedApplicationQueryer>();
            _mockTypeHintCommandFactory = new Mock<ITypeHintCommandFactory>();
            _mockDisplayedResourceHandler = new Mock<IDisplayedResourcesHandler>();
            _mockConsoleUtilities = new Mock<IConsoleUtilities>();
            _mockDirectoryManager = new Mock<IDirectoryManager>();
            _mockFileManager = new Mock<IFileManager>();
            _mockDeploymentManifestEngine = new Mock<IDeploymentManifestEngine>();
            _mockLocalUserSettingsEngine = new Mock<ILocalUserSettingsEngine>();
            _mockCdkVersionDetector = new Mock<ICDKVersionDetector>();
            _mockAwsServiceHandler = new Mock<IAWSServiceHandler>();
            _mockOptionSettingHandler = new Mock<IOptionSettingHandler>();
            _mockValidatorFactory = new Mock<IValidatorFactory>();
            _mockRecipeHandler = new Mock<IRecipeHandler>();
            _mockDeployToolWorkspaceMetadata = new Mock<IDeployToolWorkspaceMetadata>();
            _mockDeploymentSettingsHandler = new Mock<IDeploymentSettingsHandler>();
        }

        private CommandFactory CreateCommandFactory()
        {
            return new CommandFactory(
                _mockServiceProvider.Object,
                _mockToolInteractiveService.Object,
                _mockOrchestratorInteractiveService.Object,
                _mockCdkManager.Object,
                _mockSystemCapabilityEvaluator.Object,
                _mockCloudApplicationNameGenerator.Object,
                _mockAwsUtilities.Object,
                _mockAwsClientFactory.Object,
                _mockAwsResourceQueryer.Object,
                _mockProjectParserUtility.Object,
                _mockCommandLineWrapper.Object,
                _mockCdkProjectHandler.Object,
                _mockDeploymentBundleHandler.Object,
                _mockCloudFormationTemplateReader.Object,
                _mockDeployedApplicationQueryer.Object,
                _mockTypeHintCommandFactory.Object,
                _mockDisplayedResourceHandler.Object,
                _mockConsoleUtilities.Object,
                _mockDirectoryManager.Object,
                _mockFileManager.Object,
                _mockDeploymentManifestEngine.Object,
                _mockLocalUserSettingsEngine.Object,
                _mockCdkVersionDetector.Object,
                _mockAwsServiceHandler.Object,
                _mockOptionSettingHandler.Object,
                _mockValidatorFactory.Object,
                _mockRecipeHandler.Object,
                _mockDeployToolWorkspaceMetadata.Object,
                _mockDeploymentSettingsHandler.Object
            );
        }

        [Fact]
        public void BuildRootCommand_ReturnsRootCommandWithExpectedSubcommands()
        {
            // Arrange
            var commandFactory = CreateCommandFactory();

            // Act
            var rootCommand = commandFactory.BuildRootCommand();

            // Assert
            Assert.NotNull(rootCommand);
            Assert.Equal("dotnet-aws", rootCommand.Name);
            Assert.Contains(rootCommand.Options, o => o.Name == "version");
            Assert.Contains(rootCommand.Children, c => c.Name == "deploy");
            Assert.Contains(rootCommand.Children, c => c.Name == "list-deployments");
            Assert.Contains(rootCommand.Children, c => c.Name == "delete-deployment");
            Assert.Contains(rootCommand.Children, c => c.Name == "deployment-project");
            Assert.Contains(rootCommand.Children, c => c.Name == "server-mode");
        }

        [Fact]
        public void BuildRootCommand_DeployCommandHasExpectedOptions()
        {
            // Arrange
            var commandFactory = CreateCommandFactory();

            // Act
            var rootCommand = commandFactory.BuildRootCommand();
            var deployCommand = rootCommand.Children.First(c => c.Name == "deploy") as Command;

            // Assert
            Assert.NotNull(deployCommand);
            Assert.Contains(deployCommand.Options, o => o.Name == "profile");
            Assert.Contains(deployCommand.Options, o => o.Name == "region");
            Assert.Contains(deployCommand.Options, o => o.Name == "project-path");
            Assert.Contains(deployCommand.Options, o => o.Name == "application-name");
            Assert.Contains(deployCommand.Options, o => o.Name == "apply");
            Assert.Contains(deployCommand.Options, o => o.Name == "diagnostics");
            Assert.Contains(deployCommand.Options, o => o.Name == "silent");
            Assert.Contains(deployCommand.Options, o => o.Name == "deployment-project");
            Assert.Contains(deployCommand.Options, o => o.Name == "save-settings");
            Assert.Contains(deployCommand.Options, o => o.Name == "save-all-settings");
        }

        // Add more tests for other commands and their options...

        [Fact]
        public void BuildRootCommand_ServerModeCommandHasExpectedOptions()
        {
            // Arrange
            var commandFactory = CreateCommandFactory();

            // Act
            var rootCommand = commandFactory.BuildRootCommand();
            var serverModeCommand = rootCommand.Children.First(c => c.Name == "server-mode") as Command;

            // Assert
            Assert.NotNull(serverModeCommand);
            Assert.Contains(serverModeCommand.Options, o => o.Name == "port");
            Assert.Contains(serverModeCommand.Options, o => o.Name == "parent-pid");
            Assert.Contains(serverModeCommand.Options, o => o.Name == "unsecure-mode");
            Assert.Contains(serverModeCommand.Options, o => o.Name == "diagnostics");
        }

        [Fact]
        public async Task DeployCommand_UsesRegionFromCLIWhenProvided()
        {
            // Arrange
            var commandFactory = CreateCommandFactory();
            var mockCredentials = new Mock<AWSCredentials>();
            var testProfile = "test-profile";
            var regionFromCLI = "us-west-2";
            var regionFromProfile = "us-east-1";
            var testProjectPath = "/path/to/project";

            // Mock ProjectDefinition
            var mockProjectDefinition = new ProjectDefinition(
                new System.Xml.XmlDocument(),
                testProjectPath,
                testProjectPath,
                "123");

            _mockProjectParserUtility
                .Setup(x => x.Parse(It.IsAny<string>()))
                .ReturnsAsync(mockProjectDefinition);

            _mockAwsUtilities
                .Setup(x => x.ResolveAWSCredentials(It.IsAny<string>()))
                .Returns(Task.FromResult((Tuple.Create(mockCredentials.Object, regionFromProfile))));

            _mockAwsUtilities
                .Setup(x => x.ResolveAWSRegion(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(regionFromCLI);

            // Create a mock DeploymentSettings
            var mockDeploymentSettings = new DeploymentSettings
            {
                AWSProfile = "deployment-profile",
                AWSRegion = "deployment-region"
            };

            _mockDeploymentSettingsHandler
                .Setup(x => x.ReadSettings(It.IsAny<string>()))
                .ReturnsAsync(mockDeploymentSettings);

            _mockAwsResourceQueryer
                .Setup(x => x.GetCallerIdentity(It.IsAny<string>()))
                .ReturnsAsync(new GetCallerIdentityResponse { Account = "123456789012" });


            // Act
            var result = await InvokeDeployCommandHandler(new DeployCommandHandlerInput
            {
                Profile = testProfile,
                Region = regionFromCLI,
                Apply = "some-settings-file.json",
                ProjectPath = testProjectPath
            });

            // Assert
            _mockProjectParserUtility.Verify(x => x.Parse(testProjectPath), Times.Once);
            _mockAwsUtilities.Verify(x => x.ResolveAWSCredentials(testProfile), Times.Once);
            _mockAwsUtilities.Verify(x => x.ResolveAWSRegion(regionFromCLI, null), Times.Once);
        }

        [Fact]
        public async Task DeployCommand_UsesRegionFromProfileWhenCLIRegionNotProvided()
        {
            // Arrange
            var commandFactory = CreateCommandFactory();
            var mockCredentials = new Mock<AWSCredentials>();
            var testProfile = "test-profile";
            var regionFromProfile = "us-east-1";
            var testProjectPath = "/path/to/project";

            // Mock ProjectDefinition
            var mockProjectDefinition = new ProjectDefinition(
                new System.Xml.XmlDocument(),
                testProjectPath,
                testProjectPath,
                "123");

            _mockProjectParserUtility
                .Setup(x => x.Parse(It.IsAny<string>()))
                .ReturnsAsync(mockProjectDefinition);

            _mockAwsUtilities
                .Setup(x => x.ResolveAWSCredentials(It.IsAny<string>()))
                .Returns(Task.FromResult((Tuple.Create(mockCredentials.Object, regionFromProfile))));

            _mockAwsUtilities
                .Setup(x => x.ResolveAWSRegion(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string r, string f) => r);

            // Create a mock DeploymentSettings
            var mockDeploymentSettings = new DeploymentSettings
            {
                AWSProfile = "deployment-profile",
                AWSRegion = null  // Ensure this is null to test our scenario
            };

            _mockDeploymentSettingsHandler
                .Setup(x => x.ReadSettings(It.IsAny<string>()))
                .ReturnsAsync(mockDeploymentSettings);

            _mockAwsResourceQueryer
                .Setup(x => x.GetCallerIdentity(It.IsAny<string>()))
                .ReturnsAsync(new GetCallerIdentityResponse { Account = "123456789012" });

            // Act
            var result = await InvokeDeployCommandHandler(new DeployCommandHandlerInput
            {
                Profile = testProfile,
                Region = null,  // Not providing a region via CLI
                Apply = "some-settings-file.json",
                ProjectPath = testProjectPath
            });

            // Assert
            _mockProjectParserUtility.Verify(x => x.Parse(testProjectPath), Times.Once);
            _mockAwsUtilities.Verify(x => x.ResolveAWSCredentials(testProfile), Times.Once);
            _mockAwsUtilities.Verify(x => x.ResolveAWSRegion(regionFromProfile, null), Times.Once);

            // Verify that the region from the profile is used
            _mockAwsResourceQueryer.Verify(x => x.GetCallerIdentity(regionFromProfile), Times.Once);
        }

        [Fact]
        public void BuildRootCommand_DeleteCommandHasExpectedOptions()
        {
            // Arrange
            var commandFactory = CreateCommandFactory();

            // Act
            var rootCommand = commandFactory.BuildRootCommand();
            var deleteCommand = rootCommand.Children.First(c => c.Name == "delete-deployment") as Command;

            // Assert
            Assert.NotNull(deleteCommand);
            Assert.Contains(deleteCommand.Options, o => o.Name == "profile");
            Assert.Contains(deleteCommand.Options, o => o.Name == "region");
            Assert.Contains(deleteCommand.Options, o => o.Name == "project-path");
            Assert.Contains(deleteCommand.Options, o => o.Name == "diagnostics");
            Assert.Contains(deleteCommand.Options, o => o.Name == "silent");

            // Verify that the delete command has a deployment-name argument
            Assert.Contains(deleteCommand.Arguments, a => a.Name == "deployment-name");
        }

        [Fact]
        public void BuildRootCommand_ListCommandHasExpectedOptions()
        {
            // Arrange
            var commandFactory = CreateCommandFactory();

            // Act
            var rootCommand = commandFactory.BuildRootCommand();
            var listCommand = rootCommand.Children.First(c => c.Name == "list-deployments") as Command;

            // Assert
            Assert.NotNull(listCommand);
            Assert.Contains(listCommand.Options, o => o.Name == "profile");
            Assert.Contains(listCommand.Options, o => o.Name == "region");
            Assert.Contains(listCommand.Options, o => o.Name == "diagnostics");
        }

        [Fact]
        public async Task DeleteCommand_UsesRegionFromCLIWhenProvided()
        {
            // Arrange
            var commandFactory = CreateCommandFactory();
            var mockCredentials = new Mock<AWSCredentials>();
            var testProfile = "test-profile";
            var regionFromCLI = "us-west-2";
            var regionFromProfile = "us-east-1";
            var deploymentName = "test-deployment";

            _mockAwsUtilities
                .Setup(x => x.ResolveAWSCredentials(It.IsAny<string>()))
                .Returns(Task.FromResult((Tuple.Create(mockCredentials.Object, regionFromProfile))));

            _mockAwsUtilities
                .Setup(x => x.ResolveAWSRegion(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(regionFromCLI);

            _mockAwsClientFactory
                .Setup(x => x.ConfigureAWSOptions(It.IsAny<Action<AWSOptions>>()))
                .Callback<Action<AWSOptions>>(action =>
                {
                    var options = new AWSOptions();
                    action(options);
                    Assert.Equal(regionFromCLI, options.Region.SystemName);
                });

            // Act
            var result = await InvokeDeleteCommandHandler(new DeleteCommandHandlerInput
            {
                Profile = testProfile,
                Region = regionFromCLI,
                DeploymentName = deploymentName
            });

            // Assert
            _mockAwsUtilities.Verify(x => x.ResolveAWSCredentials(testProfile), Times.Once);
            _mockAwsUtilities.Verify(x => x.ResolveAWSRegion(regionFromCLI, null), Times.Once);
            _mockAwsClientFactory.Verify(x => x.ConfigureAWSOptions(It.IsAny<Action<AWSOptions>>()), Times.Once);
        }

        [Fact]
        public async Task DeleteCommand_UsesRegionFromProfileWhenCLIRegionNotProvided()
        {
            // Arrange
            var commandFactory = CreateCommandFactory();
            var mockCredentials = new Mock<AWSCredentials>();
            var testProfile = "test-profile";
            var regionFromProfile = "us-east-1";
            var deploymentName = "test-deployment";

            _mockAwsUtilities
                .Setup(x => x.ResolveAWSCredentials(It.IsAny<string>()))
                .Returns(Task.FromResult((Tuple.Create(mockCredentials.Object, regionFromProfile))));

            _mockAwsUtilities
                .Setup(x => x.ResolveAWSRegion(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string r, string f) => f);

            _mockAwsClientFactory
                .Setup(x => x.ConfigureAWSOptions(It.IsAny<Action<AWSOptions>>()))
                .Callback<Action<AWSOptions>>(action =>
                {
                    var options = new AWSOptions();
                    action(options);
                    Assert.Equal(regionFromProfile, options.Region.SystemName);
                });

            // Act
            var result = await InvokeDeleteCommandHandler(new DeleteCommandHandlerInput
            {
                Profile = testProfile,
                Region = null,  // Not providing a region via CLI
                DeploymentName = deploymentName
            });

            // Assert
            _mockAwsUtilities.Verify(x => x.ResolveAWSCredentials(testProfile), Times.Once);
            _mockAwsUtilities.Verify(x => x.ResolveAWSRegion(regionFromProfile, null), Times.Once);
            _mockAwsClientFactory.Verify(x => x.ConfigureAWSOptions(It.IsAny<Action<AWSOptions>>()), Times.Once);
        }

        [Fact]
        public async Task ListCommand_UsesRegionFromCLIWhenProvided()
        {
            // Arrange
            var commandFactory = CreateCommandFactory();
            var mockCredentials = new Mock<AWSCredentials>();
            var testProfile = "test-profile";
            var regionFromCLI = "us-west-2";
            var regionFromProfile = "us-east-1";

            _mockAwsUtilities
                .Setup(x => x.ResolveAWSCredentials(It.IsAny<string>()))
                .Returns(Task.FromResult((Tuple.Create(mockCredentials.Object, regionFromProfile))));

            _mockAwsUtilities
                .Setup(x => x.ResolveAWSRegion(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(regionFromCLI);

            _mockAwsClientFactory
                .Setup(x => x.ConfigureAWSOptions(It.IsAny<Action<AWSOptions>>()))
                .Callback<Action<AWSOptions>>(action =>
                {
                    var options = new AWSOptions();
                    action(options);
                    Assert.Equal(regionFromCLI, options.Region.SystemName);
                });

            // Act
            var result = await InvokeListCommandHandler(new ListCommandHandlerInput
            {
                Profile = testProfile,
                Region = regionFromCLI
            });

            // Assert
            _mockAwsUtilities.Verify(x => x.ResolveAWSCredentials(testProfile), Times.Once);
            _mockAwsUtilities.Verify(x => x.ResolveAWSRegion(regionFromCLI, null), Times.Once);
            _mockAwsClientFactory.Verify(x => x.ConfigureAWSOptions(It.IsAny<Action<AWSOptions>>()), Times.Once);
        }

        [Fact]
        public async Task ListCommand_UsesRegionFromProfileWhenCLIRegionNotProvided()
        {
            // Arrange
            var commandFactory = CreateCommandFactory();
            var mockCredentials = new Mock<AWSCredentials>();
            var testProfile = "test-profile";
            var regionFromProfile = "us-east-1";

            _mockAwsUtilities
                .Setup(x => x.ResolveAWSCredentials(It.IsAny<string>()))
                .Returns(Task.FromResult((Tuple.Create(mockCredentials.Object, regionFromProfile))));

            _mockAwsUtilities
                .Setup(x => x.ResolveAWSRegion(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string r, string f) => f);

            _mockAwsClientFactory
                .Setup(x => x.ConfigureAWSOptions(It.IsAny<Action<AWSOptions>>()))
                .Callback<Action<AWSOptions>>(action =>
                {
                    var options = new AWSOptions();
                    action(options);
                    Assert.Equal(regionFromProfile, options.Region.SystemName);
                });

            // Act
            var result = await InvokeListCommandHandler(new ListCommandHandlerInput
            {
                Profile = testProfile,
                Region = null  // Not providing a region via CLI
            });

            // Assert
            _mockAwsUtilities.Verify(x => x.ResolveAWSCredentials(testProfile), Times.Once);
            _mockAwsUtilities.Verify(x => x.ResolveAWSRegion(regionFromProfile, null), Times.Once);
            _mockAwsClientFactory.Verify(x => x.ConfigureAWSOptions(It.IsAny<Action<AWSOptions>>()), Times.Once);
        }

        private async Task<int> InvokeDeployCommandHandler(DeployCommandHandlerInput input)
        {
            var args = new List<string> { "deploy" };

            if (!string.IsNullOrEmpty(input.Profile))
                args.AddRange(new[] { "--profile", input.Profile });

            if (!string.IsNullOrEmpty(input.Region))
                args.AddRange(new[] { "--region", input.Region });

            if (!string.IsNullOrEmpty(input.Apply))
                args.AddRange(new[] { "--apply", input.Apply });

            if (!string.IsNullOrEmpty(input.ProjectPath))
                args.AddRange(new[] { "--project-path", input.ProjectPath });

            var rootCommand = CreateCommandFactory().BuildRootCommand();
            return await rootCommand.InvokeAsync(args.ToArray());
        }


        private async Task<int> InvokeDeleteCommandHandler(DeleteCommandHandlerInput input)
        {
            var args = new List<string> { "delete-deployment" };

            if (!string.IsNullOrEmpty(input.Profile))
                args.AddRange(new[] { "--profile", input.Profile });

            if (!string.IsNullOrEmpty(input.Region))
                args.AddRange(new[] { "--region", input.Region });

            if (!string.IsNullOrEmpty(input.DeploymentName))
                args.Add(input.DeploymentName);

            var rootCommand = CreateCommandFactory().BuildRootCommand();
            return await rootCommand.InvokeAsync(args.ToArray());
        }

        private async Task<int> InvokeListCommandHandler(ListCommandHandlerInput input)
        {
            var args = new List<string> { "list-deployments" };

            if (!string.IsNullOrEmpty(input.Profile))
                args.AddRange(new[] { "--profile", input.Profile });

            if (!string.IsNullOrEmpty(input.Region))
                args.AddRange(new[] { "--region", input.Region });

            var rootCommand = CreateCommandFactory().BuildRootCommand();
            return await rootCommand.InvokeAsync(args.ToArray());
        }


    }
}
