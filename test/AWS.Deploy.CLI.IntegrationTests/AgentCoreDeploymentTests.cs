// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.BedrockAgentCore;
using Amazon.BedrockAgentCore.Model;
using Amazon.BedrockAgentCoreControl;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.CLI.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Helpers;
using AWS.Deploy.CLI.IntegrationTests.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace AWS.Deploy.CLI.IntegrationTests
{
    /// <summary>
    /// Integration test that deploys an AgentCore agent using the deploy tool,
    /// invokes it via the AgentCore SDK, and tears down the stack.
    /// Requires AWS credentials with Bedrock and AgentCore permissions.
    /// </summary>
    public class AgentCoreDeploymentTests : IDisposable
    {
        private readonly IServiceCollection _serviceCollection;
        private readonly CloudFormationHelper _cloudFormationHelper;
        private bool _isDisposed;
        private string? _stackName;
        private readonly TestAppManager _testAppManager;

        public AgentCoreDeploymentTests()
        {
            _serviceCollection = new ServiceCollection();
            _serviceCollection.AddCustomServices();
            _serviceCollection.AddTestServices();

            var cloudFormationClient = new AmazonCloudFormationClient();
            _cloudFormationHelper = new CloudFormationHelper(cloudFormationClient);

            _testAppManager = new TestAppManager();
        }

        [Fact]
        public async Task DeployAndInvokeAgentCoreRuntime()
        {
            _stackName = $"AgentCore{Guid.NewGuid().ToString().Split('-').Last()}";
            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "AgentCoreWebApp", "AgentCoreWebApp.csproj"));

            InMemoryInteractiveService interactiveService = null!;
            try
            {
                // Deploy using the AgentCore recipe with default settings (silent mode)
                var deployArgs = new[] { "deploy", "--project-path", projectPath, "--application-name", _stackName, "--diagnostics", "--silent" };
                Assert.Equal(CommandReturnCodes.SUCCESS, await _serviceCollection.RunDeployToolAsync(deployArgs,
                    provider =>
                    {
                        interactiveService = provider.GetRequiredService<InMemoryInteractiveService>();

                        interactiveService.StdInWriter.Write(Environment.NewLine); // Select default recommendation (AgentCore)
                        interactiveService.StdInWriter.Write(Environment.NewLine); // Accept default settings
                        interactiveService.StdInWriter.Flush();
                    }));

                // Verify stack deployed successfully
                Assert.Equal(StackStatus.CREATE_COMPLETE, await _cloudFormationHelper.GetStackStatus(_stackName));

                var deployStdOut = interactiveService.StdOutReader.ReadAllLines();

                // Extract the runtime ARN from the displayed resources output
                var arnLine = deployStdOut.FirstOrDefault(line => line.Trim().StartsWith("ARN:"));
                Assert.NotNull(arnLine);
                var runtimeArn = arnLine!.Split(":", 2)[1].Trim();
                Assert.StartsWith("arn:", runtimeArn);

                // Wait for the runtime to become active before invoking
                await WaitForRuntimeActive(runtimeArn);

                // Invoke the agent via the AgentCore SDK
                var agentCoreClient = new AmazonBedrockAgentCoreClient();
                var payload = JsonSerializer.Serialize(new { prompt = "What is 2+2? Reply with just the number." });
                using var payloadStream = new MemoryStream(Encoding.UTF8.GetBytes(payload));

                var invokeResponse = await agentCoreClient.InvokeAgentRuntimeAsync(new InvokeAgentRuntimeRequest
                {
                    AgentRuntimeArn = runtimeArn,
                    Payload = payloadStream,
                    ContentType = "application/json"
                });

                // Read the response
                using var reader = new StreamReader(invokeResponse.Response);
                var responseBody = await reader.ReadToEndAsync();

                // The agent should return something containing "4"
                Assert.NotEmpty(responseBody);
                Assert.Contains("4", responseBody);
            }
            finally
            {
                interactiveService?.ReadStdOutStartToEnd();
            }

            try
            {
                // Delete the stack
                var deleteArgs = new[] { "delete-deployment", _stackName, "--diagnostics" };
                Assert.Equal(CommandReturnCodes.SUCCESS, await _serviceCollection.RunDeployToolAsync(deleteArgs,
                    provider =>
                    {
                        interactiveService = provider.GetRequiredService<InMemoryInteractiveService>();

                        interactiveService.StdInWriter.Write("y");
                        interactiveService.StdInWriter.Flush();
                    }));

                Assert.True(await _cloudFormationHelper.IsStackDeleted(_stackName), $"{_stackName} still exists.");
            }
            finally
            {
                interactiveService?.ReadStdOutStartToEnd();
            }
        }

        private static async Task WaitForRuntimeActive(string runtimeArn)
        {
            var client = new AmazonBedrockAgentCoreControlClient();
            var runtimeId = runtimeArn.Split('/').Last();

            for (var i = 0; i < 60; i++) // Up to 10 minutes (60 x 10s)
            {
                try
                {
                    var response = await client.GetAgentRuntimeAsync(new Amazon.BedrockAgentCoreControl.Model.GetAgentRuntimeRequest
                    {
                        AgentRuntimeId = runtimeId
                    });

                    if (string.Equals(response.Status?.Value, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                        return;
                }
                catch
                {
                    // Runtime may not exist yet during stack creation
                }

                await Task.Delay(TimeSpan.FromSeconds(10));
            }

            throw new TimeoutException($"AgentCore runtime {runtimeId} did not become ACTIVE within 10 minutes.");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing && !string.IsNullOrEmpty(_stackName))
            {
                var isStackDeleted = _cloudFormationHelper.IsStackDeleted(_stackName).GetAwaiter().GetResult();
                if (!isStackDeleted)
                {
                    _cloudFormationHelper.DeleteStack(_stackName).GetAwaiter().GetResult();
                }
            }

            _isDisposed = true;
        }

        ~AgentCoreDeploymentTests()
        {
            Dispose(false);
        }
    }
}
