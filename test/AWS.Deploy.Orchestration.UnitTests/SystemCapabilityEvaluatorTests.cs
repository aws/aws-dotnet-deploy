// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AWS.Deploy.CLI.Common.UnitTests.Utilities;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Orchestration.Utilities;
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
        public async Task CdkAndContainerRecipe_MissingDocker_NoCache()
        {
            var commandLineWrapper = new TestCommandLineWrapper();
            commandLineWrapper.MockedResults.Add(_expectedNodeCommand, new TryRunResult { ExitCode = 0, StandardOut = "v18.16.1" });
            commandLineWrapper.MockedResults.Add(_expectedDockerCommand, new TryRunResult { ExitCode = -1, StandardOut = "windows" });

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
    }
}
