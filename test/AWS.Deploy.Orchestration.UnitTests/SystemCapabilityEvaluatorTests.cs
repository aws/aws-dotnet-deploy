// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AWS.Deploy.CLI.Common.UnitTests.Utilities;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Orchestration.Utilities;
using Moq;
using Xunit;

namespace AWS.Deploy.Orchestration.UnitTests
{
    public class SystemCapabilityEvaluatorTests
    {
        private const string _expectedNodeCommand = "node --version";
        private readonly string _expectedDockerCommand = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "docker info -f \"{{.OSType}}\"" : "docker info";

        private readonly Recommendation _cdkAndContainerRecommendation = new(new RecipeDefinition("", "", "", DeploymentTypes.CdkProject, DeploymentBundleTypes.Container, "", "", "", "", ""), null, 100, null);
        private readonly Recommendation _cdkOnlyRecommendation = new(new RecipeDefinition("", "", "", DeploymentTypes.CdkProject, DeploymentBundleTypes.DotnetPublishZipFile, "", "", "", "", ""), null, 100, null);
        private readonly Recommendation _containerOnlyRecommendation = new(new RecipeDefinition("", "", "", DeploymentTypes.ElasticContainerRegistryImage, DeploymentBundleTypes.Container, "", "", "", "", ""), null, 100, null);

        [Fact]
        public async Task CdkAndContainerRecipe_NoMissing_Cache()
        {
            var commandLineWrapper = new TestCommandLineWrapper();
            commandLineWrapper.MockedResults.Add(_expectedNodeCommand, new TryRunResult { ExitCode = 0, StandardOut = "v18.16.1" });
            commandLineWrapper.MockedResults.Add(_expectedDockerCommand, new TryRunResult { ExitCode = 0, StandardOut = "linux" });

            var evaluator = new SystemCapabilityEvaluator(commandLineWrapper);
            var missingCapabilities = await evaluator.EvaluateSystemCapabilities(_cdkAndContainerRecommendation);

            Assert.Empty(missingCapabilities);
            Assert.Equal(2, commandLineWrapper.CommandsToExecute.Count);
            Assert.Contains(commandLineWrapper.CommandsToExecute, command => command.Command == _expectedNodeCommand);
            Assert.Contains(commandLineWrapper.CommandsToExecute, command => command.Command == _expectedDockerCommand);

            // Evaluate again, to verify that the results were cached
            missingCapabilities = await evaluator.EvaluateSystemCapabilities(_cdkAndContainerRecommendation);

            Assert.Empty(missingCapabilities);
            Assert.Equal(2, commandLineWrapper.CommandsToExecute.Count); // we still expect the first two commands, since the results should be cached
        }

        [Fact]
        public async Task CdkAndContainerRecipe_NoMissing_CacheClearing()
        {
            var commandLineWrapper = new TestCommandLineWrapper();
            commandLineWrapper.MockedResults.Add(_expectedNodeCommand, new TryRunResult { ExitCode = 0, StandardOut = "v18.16.1" });
            commandLineWrapper.MockedResults.Add(_expectedDockerCommand, new TryRunResult { ExitCode = 0, StandardOut = "linux" });

            var evaluator = new SystemCapabilityEvaluator(commandLineWrapper);
            var missingCapabilities = await evaluator.EvaluateSystemCapabilities(_cdkAndContainerRecommendation);

            Assert.Empty(missingCapabilities);
            Assert.Equal(2, commandLineWrapper.CommandsToExecute.Count);
            Assert.Contains(commandLineWrapper.CommandsToExecute, command => command.Command == _expectedNodeCommand);
            Assert.Contains(commandLineWrapper.CommandsToExecute, command => command.Command == _expectedDockerCommand);

            // Evaluate again after clearing the cache to verify that the checks are run again
            evaluator.ClearCachedCapabilityChecks();
            missingCapabilities = await evaluator.EvaluateSystemCapabilities(_cdkAndContainerRecommendation);

            Assert.Empty(missingCapabilities);
            Assert.Equal(4, commandLineWrapper.CommandsToExecute.Count);
        }

        [Fact]
        public async Task CdkAndContainerRecipe_MissingDocker_NoCache()
        {
            var commandLineWrapper = new TestCommandLineWrapper();
            commandLineWrapper.MockedResults.Add(_expectedNodeCommand, new TryRunResult { ExitCode = 0, StandardOut = "v18.16.1" });
            commandLineWrapper.MockedResults.Add(_expectedDockerCommand, new TryRunResult { ExitCode = -1, StandardOut = "" });

            var evaluator = new SystemCapabilityEvaluator(commandLineWrapper);
            var missingCapabilities = await evaluator.EvaluateSystemCapabilities(_cdkAndContainerRecommendation);

            Assert.Single(missingCapabilities);
            Assert.Equal(2, commandLineWrapper.CommandsToExecute.Count);
            Assert.Contains(commandLineWrapper.CommandsToExecute, command => command.Command == _expectedNodeCommand);
            Assert.Contains(commandLineWrapper.CommandsToExecute, command => command.Command == _expectedDockerCommand);

            // Evaluate again, to verify that it will check Docker again
            missingCapabilities = await evaluator.EvaluateSystemCapabilities(_cdkAndContainerRecommendation);

            Assert.Single(missingCapabilities);
            Assert.Equal(3, commandLineWrapper.CommandsToExecute.Count);  // verify that this was incremented for the second check
        }

        [Fact]
        public async Task ContainerOnlyRecipe_NoMissing_Cache()
        {
            var commandLineWrapper = new TestCommandLineWrapper();
            commandLineWrapper.MockedResults.Add(_expectedNodeCommand, new TryRunResult { ExitCode = 0, StandardOut = "v18.16.1" });
            commandLineWrapper.MockedResults.Add(_expectedDockerCommand, new TryRunResult { ExitCode = 0, StandardOut = "linux" });

            var evaluator = new SystemCapabilityEvaluator(commandLineWrapper);
            var missingCapabilities = await evaluator.EvaluateSystemCapabilities(_containerOnlyRecommendation);

            Assert.Empty(missingCapabilities);
            Assert.Single(commandLineWrapper.CommandsToExecute);    // only expect Docker, since don't need CDK for the ECR recipe
            Assert.Contains(commandLineWrapper.CommandsToExecute, command => command.Command == _expectedDockerCommand);

            // Evaluate again, to verify that the results were cached
            missingCapabilities = await evaluator.EvaluateSystemCapabilities(_containerOnlyRecommendation);

            Assert.Empty(missingCapabilities);
            Assert.Single(commandLineWrapper.CommandsToExecute); // we only expect the first command, since the results should be cached
        }

        [Fact]
        public async Task ContainerOnlyRecipe_DockerMissing_NoCache()
        {
            var commandLineWrapper = new TestCommandLineWrapper();
            commandLineWrapper.MockedResults.Add(_expectedNodeCommand, new TryRunResult { ExitCode = 0, StandardOut = "v18.16.1" });
            commandLineWrapper.MockedResults.Add(_expectedDockerCommand, new TryRunResult { ExitCode = -1, StandardOut = "windows" });

            var evaluator = new SystemCapabilityEvaluator(commandLineWrapper);
            var missingCapabilities = await evaluator.EvaluateSystemCapabilities(_containerOnlyRecommendation);

            Assert.Single(missingCapabilities);
            Assert.Single(commandLineWrapper.CommandsToExecute);    // only expect Docker, since don't need CDK for the ECR recipe
            Assert.Contains(commandLineWrapper.CommandsToExecute, command => command.Command == _expectedDockerCommand);

            // Evaluate again, to verify that it checks Docker again
            missingCapabilities = await evaluator.EvaluateSystemCapabilities(_containerOnlyRecommendation);

            Assert.Single(missingCapabilities);
            Assert.Equal(2, commandLineWrapper.CommandsToExecute.Count); // verify that this was incremented for the second check
        }

        [Fact]
        public async Task ContainerOnlyRecipe_DockerInWindowsMode_NoCache()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Docker only supports Windows vs. Linux mode when running on Windows, so
                // this test isn't valid on other OSes since we always assume "linux"
                return;
            }

            var commandLineWrapper = new TestCommandLineWrapper();
            commandLineWrapper.MockedResults.Add(_expectedNodeCommand, new TryRunResult { ExitCode = 0, StandardOut = "v18.16.1" });
            commandLineWrapper.MockedResults.Add(_expectedDockerCommand, new TryRunResult { ExitCode = 0, StandardOut = "windows" });

            var evaluator = new SystemCapabilityEvaluator(commandLineWrapper);
            var missingCapabilities = await evaluator.EvaluateSystemCapabilities(_containerOnlyRecommendation);

            Assert.Single(missingCapabilities);
            Assert.Single(commandLineWrapper.CommandsToExecute);    // only expect Docker, since don't need CDK for the ECR recipe
            Assert.Contains(commandLineWrapper.CommandsToExecute, command => command.Command == _expectedDockerCommand);

            // Evaluate again, to verify that it checks Docker again
            missingCapabilities = await evaluator.EvaluateSystemCapabilities(_containerOnlyRecommendation);

            Assert.Single(missingCapabilities);
            Assert.Equal(2, commandLineWrapper.CommandsToExecute.Count); // verify that this was incremented for the second check
        }

        [Fact]
        public async Task CdkOnlyRecipe_NoMissing_Cache()
        {
            var commandLineWrapper = new TestCommandLineWrapper();
            commandLineWrapper.MockedResults.Add(_expectedNodeCommand, new TryRunResult { ExitCode = 0, StandardOut = "v18.16.1" });
            commandLineWrapper.MockedResults.Add(_expectedDockerCommand, new TryRunResult { ExitCode = 0, StandardOut = "linux" });

            var evaluator = new SystemCapabilityEvaluator(commandLineWrapper);
            var missingCapabilities = await evaluator.EvaluateSystemCapabilities(_cdkOnlyRecommendation);

            Assert.Empty(missingCapabilities);
            Assert.Single(commandLineWrapper.CommandsToExecute);    // only expect Node since don't need Docker for an Elastic Banstalk recipe
            Assert.Contains(commandLineWrapper.CommandsToExecute, command => command.Command == _expectedNodeCommand);

            // Evaluate again, to verify that the results were cached
            missingCapabilities = await evaluator.EvaluateSystemCapabilities(_cdkOnlyRecommendation);

            Assert.Empty(missingCapabilities);
            Assert.Single(commandLineWrapper.CommandsToExecute); // we still expect one, since the evaluator should have cached
        }

        [Fact]
        public async Task CdkOnlyRecipe_MissingNode_NoCache()
        {
            var commandLineWrapper = new TestCommandLineWrapper();
            commandLineWrapper.MockedResults.Add(_expectedNodeCommand, new TryRunResult { ExitCode = 1, StandardOut = "" });
            commandLineWrapper.MockedResults.Add(_expectedDockerCommand, new TryRunResult { ExitCode = 0, StandardOut = "linux" });

            var evaluator = new SystemCapabilityEvaluator(commandLineWrapper);
            var missingCapabilities = await evaluator.EvaluateSystemCapabilities(_cdkOnlyRecommendation);

            Assert.Single(missingCapabilities);
            Assert.Single(commandLineWrapper.CommandsToExecute);    // only expect Node since don't need Docker for an Elastic Banstalk recipe
            Assert.Contains(commandLineWrapper.CommandsToExecute, command => command.Command == _expectedNodeCommand);

            // Evaluate again, to verify that it checks Node again
            missingCapabilities = await evaluator.EvaluateSystemCapabilities(_cdkOnlyRecommendation);

            Assert.Single(missingCapabilities);
            Assert.Equal(2, commandLineWrapper.CommandsToExecute.Count); // verify that this was incremented for the second check
        }

        [Fact]
        public async Task CdkOnlyRecipe_NodeTooOld_NoCache()
        {
            var commandLineWrapper = new TestCommandLineWrapper();
            commandLineWrapper.MockedResults.Add(_expectedNodeCommand, new TryRunResult { ExitCode = 0, StandardOut = "v10.24.1" });
            commandLineWrapper.MockedResults.Add(_expectedDockerCommand, new TryRunResult { ExitCode = 0, StandardOut = "linux" });

            var evaluator = new SystemCapabilityEvaluator(commandLineWrapper);
            var missingCapabilities = await evaluator.EvaluateSystemCapabilities(_cdkOnlyRecommendation);

            Assert.Single(missingCapabilities);
            Assert.Single(commandLineWrapper.CommandsToExecute);    // even though Node is installed, it's older than the minimum required version
            Assert.Contains(commandLineWrapper.CommandsToExecute, command => command.Command == _expectedNodeCommand);

            // Evaluate again, to verify that it checks Node again
            missingCapabilities = await evaluator.EvaluateSystemCapabilities(_cdkOnlyRecommendation);

            Assert.Single(missingCapabilities);
            Assert.Equal(2, commandLineWrapper.CommandsToExecute.Count); // verify that this was incremented for the second check
        }


        [Fact]
        public async Task CdkAndContainerRecipe_ChecksTimeout()
        {
            // Mock the CommandLineWrapper to throw TaskCanceledException, which is similar to if the node or docker commands timed out
            var mock = new Mock<ICommandLineWrapper>();
            mock.Setup(x => x.Run(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Action<TryRunResult>>(), It.IsAny<bool>(),
                It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ThrowsAsync(new TaskCanceledException());

            var evaluator = new SystemCapabilityEvaluator(mock.Object);
            var missingCapabilities = await evaluator.EvaluateSystemCapabilities(_cdkAndContainerRecommendation);

            // Assert that both Node and Docker are reported missing for a CDK+Container recipe
            Assert.Equal(2, missingCapabilities.Count);
        }


    }
}
