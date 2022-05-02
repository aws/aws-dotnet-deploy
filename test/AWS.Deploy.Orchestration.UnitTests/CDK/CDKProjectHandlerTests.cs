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

namespace AWS.Deploy.Orchestration.UnitTests.CDK
{
    public class CDKProjectHandlerTests
    {
        [Fact]
        public async Task CheckCDKBootstrap_DoesNotExist()
        {
            var interactiveService = new Mock<IOrchestratorInteractiveService>();
            var commandLineWrapper = new Mock<ICommandLineWrapper>();
            var fileManager = new Mock<IFileManager>();

            var awsResourceQuery = new Mock<IAWSResourceQueryer>();
            awsResourceQuery.Setup(x => x.GetCloudFormationStack(It.IsAny<string>())).Returns(Task.FromResult<Stack>(null));


            var cdkProjectHandler = new CdkProjectHandler(interactiveService.Object, commandLineWrapper.Object, awsResourceQuery.Object, fileManager.Object);

            Assert.True(await cdkProjectHandler.DetermineIfCDKBootstrapShouldRun());
        }

        [Fact]
        public async Task CheckCDKBootstrap_NoCFParameter()
        {
            var interactiveService = new Mock<IOrchestratorInteractiveService>();
            var commandLineWrapper = new Mock<ICommandLineWrapper>();
            var fileManager = new Mock<IFileManager>();

            var awsResourceQuery = new Mock<IAWSResourceQueryer>();
            awsResourceQuery.Setup(x => x.GetCloudFormationStack(It.IsAny<string>())).Returns(Task.FromResult<Stack>(new Stack { Parameters = new List<Parameter>() }));


            var cdkProjectHandler = new CdkProjectHandler(interactiveService.Object, commandLineWrapper.Object, awsResourceQuery.Object, fileManager.Object);

            Assert.True(await cdkProjectHandler.DetermineIfCDKBootstrapShouldRun());
        }

        [Fact]
        public async Task CheckCDKBootstrap_NoSSMParameter()
        {
            var interactiveService = new Mock<IOrchestratorInteractiveService>();
            var commandLineWrapper = new Mock<ICommandLineWrapper>();
            var fileManager = new Mock<IFileManager>();

            var awsResourceQuery = new Mock<IAWSResourceQueryer>();
            awsResourceQuery.Setup(x => x.GetCloudFormationStack(It.IsAny<string>())).Returns(Task.FromResult<Stack>(
                new Stack { Parameters = new List<Parameter>() { new Parameter { ParameterKey = "Qualifier", ParameterValue = "q1" } } }));


            var cdkProjectHandler = new CdkProjectHandler(interactiveService.Object, commandLineWrapper.Object, awsResourceQuery.Object, fileManager.Object);

            Assert.True(await cdkProjectHandler.DetermineIfCDKBootstrapShouldRun());
        }

        [Fact]
        public async Task CheckCDKBootstrap_SSMParameterOld()
        {
            var interactiveService = new Mock<IOrchestratorInteractiveService>();
            var commandLineWrapper = new Mock<ICommandLineWrapper>();
            var fileManager = new Mock<IFileManager>();

            var awsResourceQuery = new Mock<IAWSResourceQueryer>();
            awsResourceQuery.Setup(x => x.GetCloudFormationStack(It.IsAny<string>())).Returns(Task.FromResult<Stack>(
                new Stack { Parameters = new List<Parameter>() { new Parameter { ParameterKey = "Qualifier", ParameterValue = "q1" } } }));

            awsResourceQuery.Setup(x => x.GetParameterStoreTextValue(It.IsAny<string>())).Returns(Task.FromResult<string>("1"));


            var cdkProjectHandler = new CdkProjectHandler(interactiveService.Object, commandLineWrapper.Object, awsResourceQuery.Object, fileManager.Object);

            Assert.True(await cdkProjectHandler.DetermineIfCDKBootstrapShouldRun());
        }

        [Fact]
        public async Task CheckCDKBootstrap_SSMParameterNewer()
        {
            var interactiveService = new Mock<IOrchestratorInteractiveService>();
            var commandLineWrapper = new Mock<ICommandLineWrapper>();
            var fileManager = new Mock<IFileManager>();

            var awsResourceQuery = new Mock<IAWSResourceQueryer>();
            awsResourceQuery.Setup(x => x.GetCloudFormationStack(It.IsAny<string>())).Returns(Task.FromResult<Stack>(
                new Stack { Parameters = new List<Parameter>() { new Parameter { ParameterKey = "Qualifier", ParameterValue = "q1" } } }));

            awsResourceQuery.Setup(x => x.GetParameterStoreTextValue(It.IsAny<string>())).Returns(Task.FromResult<string>("100"));


            var cdkProjectHandler = new CdkProjectHandler(interactiveService.Object, commandLineWrapper.Object, awsResourceQuery.Object, fileManager.Object);

            Assert.False(await cdkProjectHandler.DetermineIfCDKBootstrapShouldRun());
        }

        [Fact]
        public async Task CheckCDKBootstrap_SSMParameterSame()
        {
            var interactiveService = new Mock<IOrchestratorInteractiveService>();
            var commandLineWrapper = new Mock<ICommandLineWrapper>();
            var fileManager = new Mock<IFileManager>();

            var awsResourceQuery = new Mock<IAWSResourceQueryer>();
            awsResourceQuery.Setup(x => x.GetCloudFormationStack(It.IsAny<string>())).Returns(Task.FromResult<Stack>(
                new Stack { Parameters = new List<Parameter>() { new Parameter { ParameterKey = "Qualifier", ParameterValue = "q1" } } }));

            awsResourceQuery.Setup(x => x.GetParameterStoreTextValue(It.IsAny<string>())).Returns(Task.FromResult<string>(AWS.Deploy.Constants.CDK.CDKTemplateVersion.ToString()));


            var cdkProjectHandler = new CdkProjectHandler(interactiveService.Object, commandLineWrapper.Object, awsResourceQuery.Object, fileManager.Object);

            Assert.False(await cdkProjectHandler.DetermineIfCDKBootstrapShouldRun());
        }
    }
}
