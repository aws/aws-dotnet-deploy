// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Orchestration.CDK;
using AWS.Deploy.Orchestration.Data;
using AWS.Deploy.Orchestration.Utilities;
using Moq;
using Xunit;
using Amazon.CloudFormation.Model;
using System.Collections.Generic;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Recipes.Validation;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Common.Extensions;
using AWS.Deploy.Common;

namespace AWS.Deploy.Orchestration.UnitTests.CDK
{
    public class CDKProjectHandlerTests
    {
        private readonly IOptionSettingHandler _optionSettingHandler;
        private readonly Mock<IAWSResourceQueryer> _awsResourceQueryer;
        private readonly Mock<ICdkAppSettingsSerializer> _cdkAppSettingsSerializer;
        private readonly Mock<IServiceProvider> _serviceProvider;
        private readonly Mock<IDeployToolWorkspaceMetadata> _workspaceMetadata;
        private readonly Mock<ICloudFormationTemplateReader> _cloudFormationTemplateReader;
        private readonly Mock<IFileManager> _fileManager;
        private readonly Mock<IDirectoryManager> _directoryManager;
        private readonly string _cdkBootstrapTemplate;

        public CDKProjectHandlerTests()
        {
            _awsResourceQueryer = new Mock<IAWSResourceQueryer>();
            _cdkAppSettingsSerializer = new Mock<ICdkAppSettingsSerializer>();
            _serviceProvider = new Mock<IServiceProvider>();
            _serviceProvider
                .Setup(x => x.GetService(typeof(IAWSResourceQueryer)))
                .Returns(_awsResourceQueryer.Object);
            _optionSettingHandler = new OptionSettingHandler(new ValidatorFactory(_serviceProvider.Object));
            _workspaceMetadata = new Mock<IDeployToolWorkspaceMetadata>();
            _cloudFormationTemplateReader = new Mock<ICloudFormationTemplateReader>();
            _fileManager = new Mock<IFileManager>();
            _directoryManager = new Mock<IDirectoryManager>();

            var templateIdentifier = "AWS.Deploy.Orchestration.CDK.CDKBootstrapTemplate.yaml";
            _cdkBootstrapTemplate = typeof(CdkProjectHandler).Assembly.ReadEmbeddedFile(templateIdentifier);
        }

        [Fact]
        public async Task CheckCDKBootstrap_DoesNotExist()
        {
            var interactiveService = new Mock<IOrchestratorInteractiveService>();
            var commandLineWrapper = new Mock<ICommandLineWrapper>();

            var awsResourceQuery = new Mock<IAWSResourceQueryer>();
            awsResourceQuery.Setup(x => x.GetCloudFormationStack(It.IsAny<string>())).Returns(Task.FromResult<Stack>(null));

            var cdkProjectHandler = new CdkProjectHandler(interactiveService.Object, commandLineWrapper.Object, awsResourceQuery.Object, _cdkAppSettingsSerializer.Object, _fileManager.Object, _directoryManager.Object, _optionSettingHandler, _workspaceMetadata.Object, _cloudFormationTemplateReader.Object);

            Assert.True(await cdkProjectHandler.DetermineIfCDKBootstrapShouldRun());
        }

        [Fact]
        public async Task CheckCDKBootstrap_NoCFParameter()
        {
            var interactiveService = new Mock<IOrchestratorInteractiveService>();
            var commandLineWrapper = new Mock<ICommandLineWrapper>();

            var awsResourceQuery = new Mock<IAWSResourceQueryer>();
            awsResourceQuery.Setup(x => x.GetCloudFormationStack(It.IsAny<string>())).Returns(Task.FromResult<Stack>(new Stack { Parameters = new List<Parameter>() }));

            var cdkProjectHandler = new CdkProjectHandler(interactiveService.Object, commandLineWrapper.Object, awsResourceQuery.Object, _cdkAppSettingsSerializer.Object, _fileManager.Object, _directoryManager.Object, _optionSettingHandler, _workspaceMetadata.Object, _cloudFormationTemplateReader.Object);

            Assert.True(await cdkProjectHandler.DetermineIfCDKBootstrapShouldRun());
        }

        [Fact]
        public async Task CheckCDKBootstrap_NoSSMParameter()
        {
            var interactiveService = new Mock<IOrchestratorInteractiveService>();
            var commandLineWrapper = new Mock<ICommandLineWrapper>();

            var awsResourceQuery = new Mock<IAWSResourceQueryer>();
            awsResourceQuery.Setup(x => x.GetCloudFormationStack(It.IsAny<string>())).Returns(Task.FromResult<Stack>(
                new Stack { Parameters = new List<Parameter>() { new Parameter { ParameterKey = "Qualifier", ParameterValue = "q1" } } }));

            var cdkProjectHandler = new CdkProjectHandler(interactiveService.Object, commandLineWrapper.Object, awsResourceQuery.Object, _cdkAppSettingsSerializer.Object, _fileManager.Object, _directoryManager.Object, _optionSettingHandler, _workspaceMetadata.Object, _cloudFormationTemplateReader.Object);

            Assert.True(await cdkProjectHandler.DetermineIfCDKBootstrapShouldRun());
        }

        [Fact]
        public async Task CheckCDKBootstrap_SSMParameterOld()
        {
            var interactiveService = new Mock<IOrchestratorInteractiveService>();
            var commandLineWrapper = new Mock<ICommandLineWrapper>();
            var deployToolWorkspaceMetadata = new Mock<IDeployToolWorkspaceMetadata>();
            var awsClientFactory = new Mock<IAWSClientFactory>();

            var awsResourceQuery = new Mock<IAWSResourceQueryer>();
            awsResourceQuery.Setup(x => x.GetCloudFormationStack(It.IsAny<string>())).Returns(Task.FromResult<Stack>(
                new Stack { Parameters = new List<Parameter>() { new Parameter { ParameterKey = "Qualifier", ParameterValue = "q1" } } }));

            _fileManager.Setup(x => x.ReadAllTextAsync(It.IsAny<string>())).ReturnsAsync(_cdkBootstrapTemplate);
            var cloudFormationTemplateReader = new CloudFormationTemplateReader(awsClientFactory.Object, deployToolWorkspaceMetadata.Object, _fileManager.Object);
            awsResourceQuery.Setup(x => x.GetParameterStoreTextValue(It.IsAny<string>())).Returns(Task.FromResult<string>("1"));

            var cdkProjectHandler = new CdkProjectHandler(interactiveService.Object, commandLineWrapper.Object, awsResourceQuery.Object, _cdkAppSettingsSerializer.Object, _fileManager.Object, _directoryManager.Object, _optionSettingHandler, _workspaceMetadata.Object, cloudFormationTemplateReader);

            Assert.True(await cdkProjectHandler.DetermineIfCDKBootstrapShouldRun());
        }

        [Fact]
        public async Task CheckCDKBootstrap_SSMParameterNewer()
        {
            var interactiveService = new Mock<IOrchestratorInteractiveService>();
            var commandLineWrapper = new Mock<ICommandLineWrapper>();
            var deployToolWorkspaceMetadata = new Mock<IDeployToolWorkspaceMetadata>();
            var awsClientFactory = new Mock<IAWSClientFactory>();

            var awsResourceQuery = new Mock<IAWSResourceQueryer>();
            awsResourceQuery.Setup(x => x.GetCloudFormationStack(It.IsAny<string>())).Returns(Task.FromResult<Stack>(
                new Stack { Parameters = new List<Parameter>() { new Parameter { ParameterKey = "Qualifier", ParameterValue = "q1" } } }));

            _fileManager.Setup(x => x.ReadAllTextAsync(It.IsAny<string>())).ReturnsAsync(_cdkBootstrapTemplate);
            var cloudFormationTemplateReader = new CloudFormationTemplateReader(awsClientFactory.Object, deployToolWorkspaceMetadata.Object, _fileManager.Object);
            awsResourceQuery.Setup(x => x.GetParameterStoreTextValue(It.IsAny<string>())).Returns(Task.FromResult<string>("100"));


            var cdkProjectHandler = new CdkProjectHandler(interactiveService.Object, commandLineWrapper.Object, awsResourceQuery.Object, _cdkAppSettingsSerializer.Object, _fileManager.Object, _directoryManager.Object, _optionSettingHandler, _workspaceMetadata.Object, cloudFormationTemplateReader);

            Assert.False(await cdkProjectHandler.DetermineIfCDKBootstrapShouldRun());
        }

        [Fact]
        public async Task CheckCDKBootstrap_SSMParameterSame()
        {
            var interactiveService = new Mock<IOrchestratorInteractiveService>();
            var commandLineWrapper = new Mock<ICommandLineWrapper>();
            var deployToolWorkspaceMetadata = new Mock<IDeployToolWorkspaceMetadata>();
            var awsClientFactory = new Mock<IAWSClientFactory>();

            var awsResourceQuery = new Mock<IAWSResourceQueryer>();
            awsResourceQuery.Setup(x => x.GetCloudFormationStack(It.IsAny<string>())).Returns(Task.FromResult<Stack>(
                new Stack { Parameters = new List<Parameter>() { new Parameter { ParameterKey = "Qualifier", ParameterValue = "q1" } } }));

            _fileManager.Setup(x => x.ReadAllTextAsync(It.IsAny<string>())).ReturnsAsync(_cdkBootstrapTemplate);
            var cloudFormationTemplateReader = new CloudFormationTemplateReader(awsClientFactory.Object, deployToolWorkspaceMetadata.Object, _fileManager.Object);
            var templateVersion = await cloudFormationTemplateReader.ReadCDKTemplateVersion();
            awsResourceQuery.Setup(x => x.GetParameterStoreTextValue(It.IsAny<string>())).Returns(Task.FromResult<string>(templateVersion.ToString()));

            var cdkProjectHandler = new CdkProjectHandler(interactiveService.Object, commandLineWrapper.Object, awsResourceQuery.Object, _cdkAppSettingsSerializer.Object, _fileManager.Object, _directoryManager.Object, _optionSettingHandler, _workspaceMetadata.Object, cloudFormationTemplateReader);

            Assert.False(await cdkProjectHandler.DetermineIfCDKBootstrapShouldRun());
        }
    }
}
